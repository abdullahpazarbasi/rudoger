using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Product.Domain;

namespace Rudoger.UnitTests;

public sealed class ProductAggregateTests
{
    [Fact]
    public void CreateNormalizesProductAndPackaging()
    {
        ProductAggregate product = CreateProduct();

        Assert.Equal("SKU-1", product.Sku);
        Assert.Equal("EA", product.BaseUomCode);
        Assert.Equal("USD", product.BasePriceCurrencyCode);
        Assert.Equal(2, product.Packagings.Count);
        Assert.Equal(3, product.UncommittedEvents.Count);
        Assert.Equal(3, product.Version);
        Assert.Equal(0, product.OriginalVersion);
    }

    [Fact]
    public void CreateRequiresExactlyOneBasePackaging()
    {
        Assert.Throws<DomainException>(() => ProductAggregate.Create(
            Guid.CreateVersion7(),
            "SKU",
            "Name",
            "EA",
            1,
            "USD",
            []));
        Assert.Throws<DomainException>(() => ProductAggregate.Create(
            Guid.CreateVersion7(),
            "SKU",
            "Name",
            "EA",
            1,
            "USD",
            [Packaging(0, "EA", 1), Packaging(0, "EA", 1)]));
    }

    [Fact]
    public void BasePackagingMustMatchBaseUomAndFactor()
    {
        Assert.Throws<DomainException>(() => ProductAggregate.Create(
            Guid.CreateVersion7(), "SKU", "Name", "EA", 1, "USD", [Packaging(0, "BOX", 1)]));
        Assert.Throws<DomainException>(() => ProductAggregate.Create(
            Guid.CreateVersion7(), "SKU", "Name", "EA", 1, "USD", [Packaging(0, "EA", 2)]));
    }

    [Fact]
    public void CreateRejectsDuplicatePackagingProperties()
    {
        Assert.Throws<DomainException>(() => ProductAggregate.Create(
            Guid.CreateVersion7(),
            "SKU",
            "Name",
            "EA",
            1,
            "USD",
            [Packaging(0, "EA", 1), Packaging(1, "EA", 2)]));
    }

    [Fact]
    public void ChangeEmitsOnlyWhenValuesDiffer()
    {
        ProductAggregate product = CreateProduct();
        product.MarkChangesAsCommitted();

        product.Change(product.Sku, product.Name, product.BasePriceAmount, product.BasePriceCurrencyCode);
        Assert.Empty(product.UncommittedEvents);

        product.Change("new-sku", "New name", 12.5m, "eur");
        ProductChanged changed = Assert.IsType<ProductChanged>(Assert.Single(product.UncommittedEvents));
        Assert.Equal("NEW-SKU", changed.Sku);
        Assert.Equal("EUR", product.BasePriceCurrencyCode);
    }

    [Fact]
    public void AdditionalPackagingCanBeChangedAndRemoved()
    {
        ProductAggregate product = CreateProduct();
        ProductPackaging packaging = product.Packagings.Single(item => item.Level == 1);
        product.MarkChangesAsCommitted();

        product.ChangePackaging(Packaging(2, "CASE", 24, packaging.Id));
        Assert.Equal(2, product.Packagings.Single(item => item.Id == packaging.Id).Level);
        product.RemovePackaging(packaging.Id);
        Assert.DoesNotContain(product.Packagings, item => item.Id == packaging.Id);
    }

    [Fact]
    public void BasePackagingCannotBeAddedChangedStructurallyOrRemoved()
    {
        ProductAggregate product = CreateProduct();
        ProductPackaging basePackaging = product.Packagings.Single(item => item.Level == 0);

        Assert.Throws<DomainException>(() => product.AddPackaging(Packaging(0, "EA", 1)));
        Assert.Throws<DomainException>(() => product.ChangePackaging(Packaging(1, "BOX", 2, basePackaging.Id)));
        Assert.Throws<DomainException>(() => product.RemovePackaging(basePackaging.Id));
    }

    [Fact]
    public void PackagingMeasurementsMustBePositive()
    {
        ProductAggregate product = CreateProduct();
        PackagingDefinition invalid = Packaging(2, "PAL", 100) with { WeightInKg = 0 };
        Assert.Throws<DomainException>(() => product.AddPackaging(invalid));
    }

    [Fact]
    public void UsageClaimIsIdempotentAndBlocksDelete()
    {
        ProductAggregate product = CreateProduct();
        Guid operationId = Guid.CreateVersion7();
        product.MarkChangesAsCommitted();

        product.ClaimUsage(operationId, "ORDER");
        product.ClaimUsage(operationId, "ORDER");
        Assert.Single(product.UsageClaims);
        Assert.Throws<ConflictException>(() => product.ClaimUsage(operationId, "INVENTORY"));
        Assert.Throws<ConflictException>(product.Delete);

        product.ReleaseUsage(operationId);
        product.Delete();
        Assert.True(product.IsDeleted);
    }

    [Fact]
    public void DeletedProductRejectsChanges()
    {
        ProductAggregate product = CreateProduct();
        product.Delete();

        Assert.Throws<ConflictException>(() => product.Change("SKU", "Name", 1, "USD"));
        Assert.Throws<ConflictException>(() => product.AddPackaging(Packaging(2, "PAL", 100)));
    }

    [Fact]
    public void LoadFromHistoryRestoresStateWithoutUncommittedEvents()
    {
        ProductAggregate source = CreateProduct();
        IDomainEvent[] history = source.UncommittedEvents.ToArray();
        var restored = new ProductAggregate();

        restored.LoadFromHistory(history);

        Assert.Equal(source.Sku, restored.Sku);
        Assert.Equal(history.Length, restored.Version);
        Assert.Empty(restored.UncommittedEvents);
    }

    [Fact]
    public void CreateValidatesIdentityCurrencyAndPackagingShape()
    {
        Assert.Throws<DomainException>(() => ProductAggregate.Create(
            Guid.Empty, "SKU", "Name", "EA", 1, "USD", [Packaging(0, "EA", 1)]));
        Assert.Throws<DomainException>(() => ProductAggregate.Create(
            Guid.CreateVersion7(), "SKU", "Name", "EA", 1, "US", [Packaging(0, "EA", 1)]));
        Assert.Throws<DomainException>(() => ProductAggregate.Create(
            Guid.CreateVersion7(), "SKU", "Name", "EA", 1, "U1D", [Packaging(0, "EA", 1)]));
        Assert.Throws<DomainException>(() => ProductAggregate.Create(
            Guid.CreateVersion7(), "SKU", "Name", "EA", 1, "USD", null!));
        Assert.Throws<DomainException>(() => ProductAggregate.Create(
            Guid.CreateVersion7(), "SKU", "Name", "EA", 1, "USD", [Packaging(0, "EA", 1, Guid.Empty)]));
        Assert.Throws<DomainException>(() => ProductAggregate.Create(
            Guid.CreateVersion7(), "SKU", "Name", "EA", 1, "USD", [Packaging(-1, "EA", 1)]));
    }

    [Fact]
    public void PackagingCrudValidatesMissingAndDuplicateResources()
    {
        ProductAggregate product = CreateProduct();
        ProductPackaging added = null!;
        product.AddPackaging(Packaging(2, "PAL", 120));
        added = product.Packagings.Single(item => item.Level == 2);

        Assert.Equal("PAL", added.UomCode);
        Assert.Throws<KeyNotFoundException>(() => product.ChangePackaging(Packaging(2, "PAL", 120)));
        Assert.Throws<KeyNotFoundException>(() => product.RemovePackaging(Guid.CreateVersion7()));
        Assert.Throws<DomainException>(() => product.AddPackaging(Packaging(1, "OTHER", 2)));
        Assert.Throws<DomainException>(() => product.AddPackaging(Packaging(3, "PAL", 2)));

        ProductPackaging nonBase = product.Packagings.Single(item => item.Level == 1);
        Assert.Throws<DomainException>(() => product.ChangePackaging(Packaging(0, "EA", 1, nonBase.Id)));
    }

    [Fact]
    public void UsageAndHistoryValidateUnknownInputs()
    {
        ProductAggregate product = CreateProduct();
        int version = product.Version;
        product.ReleaseUsage(Guid.CreateVersion7());
        Assert.Equal(version, product.Version);
        Assert.Throws<DomainException>(() => product.ClaimUsage(Guid.Empty, "ORDER"));
        Assert.Throws<InvalidOperationException>(() => new ProductAggregate().LoadFromHistory([new OtherTestEvent()]));
    }

    private static ProductAggregate CreateProduct()
    {
        return ProductAggregate.Create(
            Guid.CreateVersion7(),
            "sku-1",
            "Product",
            "ea",
            5m,
            "usd",
            [Packaging(0, "ea", 1), Packaging(1, "box", 12)]);
    }

    private static PackagingDefinition Packaging(int level, string uom, decimal factor, Guid? id = null)
    {
        return new PackagingDefinition(id ?? Guid.CreateVersion7(), level, uom, factor, null, null, null, null, null);
    }
}
