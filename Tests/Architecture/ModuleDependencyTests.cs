using System.Reflection;
using Rudoger.Modules.Authn.Application;
using Rudoger.Modules.Authn.Domain;
using Rudoger.Modules.Authn.Infrastructure;
using Rudoger.Modules.Authn.Presentation;
using Rudoger.Modules.Inventory.Application;
using Rudoger.Modules.Inventory.Domain;
using Rudoger.Modules.Inventory.Infrastructure;
using Rudoger.Modules.Inventory.Presentation;
using Rudoger.Modules.Logging.Application;
using Rudoger.Modules.Logging.Domain;
using Rudoger.Modules.Logging.Infrastructure;
using Rudoger.Modules.Logging.Presentation;
using Rudoger.Modules.Order.Application;
using Rudoger.Modules.Order.Domain;
using Rudoger.Modules.Order.Infrastructure;
using Rudoger.Modules.Order.Presentation;
using Rudoger.Modules.Product.Application;
using Rudoger.Modules.Product.Domain;
using Rudoger.Modules.Product.Infrastructure;
using Rudoger.Modules.Product.Presentation;

namespace Rudoger.ArchitectureTests;

public sealed class ModuleDependencyTests
{
    public static TheoryData<Assembly> DomainAssemblies => new()
    {
        typeof(UserAggregate).Assembly,
        typeof(ProductAggregate).Assembly,
        typeof(StockItemAggregate).Assembly,
        typeof(OrderAggregate).Assembly,
        typeof(RequestLogChannel).Assembly,
    };

    public static TheoryData<Assembly> PresentationAssemblies => new()
    {
        typeof(TokensController).Assembly,
        typeof(ProductsController).Assembly,
        typeof(StockItemsController).Assembly,
        typeof(OrdersController).Assembly,
        typeof(ApiRequestLoggingMiddleware).Assembly,
    };

    public static TheoryData<Assembly> ApplicationAssemblies => new()
    {
        typeof(AuthnApplicationService).Assembly,
        typeof(InventoryApplicationService).Assembly,
        typeof(RequestLogStart).Assembly,
        typeof(OrderApplicationService).Assembly,
        typeof(ProductApplicationService).Assembly,
    };

    public static TheoryData<Assembly> BoundaryAssemblies => new()
    {
        typeof(UserAggregate).Assembly,
        typeof(ProductAggregate).Assembly,
        typeof(StockItemAggregate).Assembly,
        typeof(OrderAggregate).Assembly,
        typeof(RequestLogChannel).Assembly,
        typeof(AuthnApplicationService).Assembly,
        typeof(ProductApplicationService).Assembly,
        typeof(InventoryApplicationService).Assembly,
        typeof(OrderApplicationService).Assembly,
        typeof(RequestLogStart).Assembly,
        typeof(TokensController).Assembly,
        typeof(ProductsController).Assembly,
        typeof(StockItemsController).Assembly,
        typeof(OrdersController).Assembly,
        typeof(ApiRequestLoggingMiddleware).Assembly,
    };

    public static TheoryData<Assembly> InfrastructureAssemblies => new()
    {
        typeof(AuthnDbContext).Assembly,
        typeof(ProductDbContext).Assembly,
        typeof(InventoryDbContext).Assembly,
        typeof(OrderDbContext).Assembly,
        typeof(LoggingDbContext).Assembly,
    };

    [Theory]
    [MemberData(nameof(DomainAssemblies))]
    public void DomainAssembliesDoNotReferenceOuterLayers(Assembly assembly)
    {
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference =>
            IsRudogerLayer(reference, "Application")
            || IsRudogerLayer(reference, "Infrastructure")
            || IsRudogerLayer(reference, "Presentation"));
    }

    [Theory]
    [MemberData(nameof(PresentationAssemblies))]
    public void PresentationAssembliesDoNotReferenceInfrastructure(Assembly assembly)
    {
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference =>
            IsRudogerLayer(reference, "Infrastructure"));
    }

    [Theory]
    [MemberData(nameof(ApplicationAssemblies))]
    public void ApplicationAssembliesDoNotReferenceOuterLayers(Assembly assembly)
    {
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference =>
            IsRudogerLayer(reference, "Infrastructure")
            || IsRudogerLayer(reference, "Presentation"));
    }

    [Theory]
    [MemberData(nameof(BoundaryAssemblies))]
    public void DomainApplicationAndPresentationDoNotReferenceOtherModules(Assembly assembly)
    {
        string module = GetModule(assembly.GetName().Name!);
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference =>
            reference.Name?.StartsWith("Rudoger.Modules.", StringComparison.Ordinal) == true
            && GetModule(reference.Name!) != module);
    }

    [Theory]
    [MemberData(nameof(InfrastructureAssemblies))]
    public void InfrastructureCrossModuleReferencesTargetOnlyPresentationContracts(Assembly assembly)
    {
        string module = GetModule(assembly.GetName().Name!);
        AssemblyName[] forbidden = assembly.GetReferencedAssemblies()
            .Where(reference => reference.Name?.StartsWith("Rudoger.Modules.", StringComparison.Ordinal) == true)
            .Where(reference => GetModule(reference.Name!) != module)
            .Where(reference => !reference.Name!.EndsWith(".Presentation", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(forbidden);
    }

    private static bool IsRudogerLayer(AssemblyName reference, string layer)
    {
        return reference.Name?.StartsWith("Rudoger.", StringComparison.Ordinal) == true
            && reference.Name.EndsWith($".{layer}", StringComparison.Ordinal);
    }

    private static string GetModule(string assemblyName)
    {
        string[] parts = assemblyName.Split('.');
        return parts.Length > 2 && parts[1] == "Modules" ? parts[2] : string.Empty;
    }
}
