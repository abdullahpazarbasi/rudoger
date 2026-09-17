using System.Reflection;
using System.Text.RegularExpressions;
using Rudoger.Modules.Inventory.Presentation;
using Rudoger.Modules.Order.Presentation;
using Rudoger.Modules.Product.Presentation;

namespace Rudoger.UnitTests;

/// <summary>
/// Guards the shape of what the HTTP API publishes. The wire contract is owned by the presentation
/// layer, so it must not carry a module's internal view models, its domain enumerations, or any
/// identifier that would let a caller infer how a module stores or correlates its work.
/// </summary>
public sealed class PublishedContractTests
{
    private static readonly Assembly[] PresentationAssemblies =
    [
        typeof(ProductResponse).Assembly,
        typeof(StockItemResponse).Assembly,
        typeof(OrderResponse).Assembly,
    ];

    public static TheoryData<Type> ResponseContracts => new()
    {
        typeof(ProductResponse),
        typeof(ProductPackagingResponse),
        typeof(StockItemResponse),
        typeof(StockMovementResponse),
        typeof(OrderResponse),
        typeof(OrderLineResponse),
        typeof(OrderPlacementResponse),
        typeof(OrderPlacementLineResponse),
        typeof(OrderTransitionResponse),
    };

    [Theory]
    [MemberData(nameof(ResponseContracts))]
    public void ResponseContractsHideStorageAndCorrelationInternals(Type contract)
    {
        string[] forbidden = ["Event", "Stream", "Version", "Operation", "Aggregate", "Projection"];

        string[] leaks = contract.GetProperties()
            .Select(property => property.Name)
            .Where(name => Words(name).Intersect(forbidden, StringComparer.Ordinal).Any())
            .ToArray();

        Assert.Empty(leaks);
    }

    // "ConversionFactor" must not trip the "Version" check, so the name is compared word by word.
    private static IEnumerable<string> Words(string name)
    {
        return Regex.Matches(name, "[A-Z]+(?![a-z])|[A-Z][a-z]*|[a-z]+").Select(match => match.Value);
    }

    [Theory]
    [MemberData(nameof(ResponseContracts))]
    public void ResponseContractsExposeNoDomainTypes(Type contract)
    {
        string[] domainMembers = contract.GetProperties()
            .Select(property => UnwrapType(property.PropertyType))
            .Where(type => type.Assembly.GetName().Name?.EndsWith(".Domain", StringComparison.Ordinal) == true)
            .Select(type => type.FullName!)
            .ToArray();

        Assert.Empty(domainMembers);
    }

    [Theory]
    [MemberData(nameof(ResponseContracts))]
    public void ResponseContractsExposeNoApplicationViewModels(Type contract)
    {
        string[] viewMembers = contract.GetProperties()
            .Select(property => UnwrapType(property.PropertyType))
            .Where(type => type.Assembly.GetName().Name?.EndsWith(".Application", StringComparison.Ordinal) == true)
            .Select(type => type.FullName!)
            .ToArray();

        Assert.Empty(viewMembers);
    }

    [Fact]
    public void ControllerSignaturesNeverPublishApplicationOrDomainTypes()
    {
        string[] leaks = PresentationAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Name.EndsWith("Controller", StringComparison.Ordinal))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .SelectMany(method => Returned(method).Select(type => $"{method.DeclaringType!.Name}.{method.Name}: {type.FullName}"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(leaks);
    }

    [Fact]
    public void TheLeakDetectionItselfCatchesALeakyContract()
    {
        string[] forbidden = ["Event", "Stream", "Version", "Operation", "Aggregate", "Projection"];

        Assert.Contains("SourceEventId", Leaky(typeof(LeakySample), forbidden));
        Assert.Contains("StreamId", Leaky(typeof(LeakySample), forbidden));
        Assert.Contains("Version", Leaky(typeof(LeakySample), forbidden));
        Assert.DoesNotContain("ConversionFactor", Leaky(typeof(LeakySample), forbidden));
        Assert.DoesNotContain("UomCode", Leaky(typeof(LeakySample), forbidden));
    }

    [Fact]
    public void ContractEnumerationsStayInStepWithTheModelTheyMirror()
    {
        AssertSameMembers(typeof(StockMovementTypeContract), typeof(Rudoger.Modules.Inventory.Domain.StockMovementType));
        AssertSameMembers(typeof(OrderStatusContract), typeof(Rudoger.Modules.Order.Domain.OrderStatus));
        AssertSameMembers(typeof(OrderTransitionTargetContract), typeof(Rudoger.Modules.Order.Domain.OrderTransitionTarget));
        AssertSameMembers(typeof(OrderPlacementStatusContract), typeof(Rudoger.Modules.Order.Domain.OrderPlacementStatus));
        AssertSameMembers(typeof(OrderTransitionStatusContract), typeof(Rudoger.Modules.Order.Domain.OrderTransitionStatus));
    }

    private static void AssertSameMembers(Type contract, Type model)
    {
        Assert.Equal(Enum.GetNames(model).Order().ToArray(), Enum.GetNames(contract).Order().ToArray());
    }

    private static string[] Leaky(Type contract, string[] forbidden)
    {
        return contract.GetProperties()
            .Select(property => property.Name)
            .Where(name => Words(name).Intersect(forbidden, StringComparer.Ordinal).Any())
            .ToArray();
    }

    private sealed record LeakySample(
        Guid SourceEventId,
        Guid StreamId,
        int Version,
        decimal ConversionFactor,
        string UomCode);

    private static IEnumerable<Type> Returned(MethodInfo method)
    {
        return Flatten(method.ReturnType)
            .Where(type => type.Assembly.GetName().Name?.StartsWith("Rudoger.", StringComparison.Ordinal) == true)
            .Where(type =>
            {
                string name = type.Assembly.GetName().Name!;
                return name.EndsWith(".Domain", StringComparison.Ordinal)
                    || (name.EndsWith(".Application", StringComparison.Ordinal)
                        && name.StartsWith("Rudoger.Modules.", StringComparison.Ordinal));
            });
    }

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;
        if (!type.IsGenericType)
        {
            yield break;
        }

        foreach (Type argument in type.GetGenericArguments())
        {
            foreach (Type nested in Flatten(argument))
            {
                yield return nested;
            }
        }
    }

    private static Type UnwrapType(Type type)
    {
        Type unwrapped = Nullable.GetUnderlyingType(type) ?? type;
        return unwrapped.IsGenericType ? unwrapped.GetGenericArguments()[^1] : unwrapped;
    }
}
