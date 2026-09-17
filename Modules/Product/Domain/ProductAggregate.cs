using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Product.Domain;

public sealed class ProductAggregate : AggregateRoot
{
    private readonly Dictionary<Guid, ProductPackaging> _packagings = [];
    private readonly Dictionary<Guid, string> _usageClaims = [];

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string BaseUomCode { get; private set; } = string.Empty;

    public decimal BasePriceAmount { get; private set; }

    public string BasePriceCurrencyCode { get; private set; } = string.Empty;

    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<ProductPackaging> Packagings => _packagings.Values;

    public IReadOnlyDictionary<Guid, string> UsageClaims => _usageClaims;

    public static ProductAggregate Create(
        Guid id,
        string sku,
        string name,
        string baseUomCode,
        decimal basePriceAmount,
        string basePriceCurrencyCode,
        IEnumerable<PackagingDefinition> packagings)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("product-id-required", "Product id is required.");
        }

        string normalizedSku = NormalizeRequired(sku, ProductRules.SkuMaximumLength, "product-sku-invalid", "SKU");
        string normalizedName = Guard.Required(name, "product-name-invalid", "Name", ProductRules.NameMaximumLength);
        string normalizedUom = NormalizeRequired(baseUomCode, ProductRules.UomCodeMaximumLength, "product-uom-invalid", "Base UoM code");
        string normalizedCurrency = NormalizeCurrency(basePriceCurrencyCode);
        Guard.NonNegative(basePriceAmount, "product-price-invalid", "Base price amount");

        PackagingDefinition[] definitions = packagings?.ToArray()
            ?? throw new DomainException("product-packaging-required", "At least the level zero packaging is required.");
        ValidatePackagingSet(definitions, normalizedUom);

        var aggregate = new ProductAggregate();
        aggregate.Raise(new ProductCreated(id, normalizedSku, normalizedName, normalizedUom, basePriceAmount, normalizedCurrency));
        foreach (PackagingDefinition definition in definitions.OrderBy(item => item.Level))
        {
            aggregate.Raise(new ProductPackagingAdded(NormalizePackaging(definition, normalizedUom)));
        }

        return aggregate;
    }

    public void Change(string sku, string name, decimal basePriceAmount, string basePriceCurrencyCode)
    {
        EnsureActive();
        string normalizedSku = NormalizeRequired(sku, ProductRules.SkuMaximumLength, "product-sku-invalid", "SKU");
        string normalizedName = Guard.Required(name, "product-name-invalid", "Name", ProductRules.NameMaximumLength);
        string normalizedCurrency = NormalizeCurrency(basePriceCurrencyCode);
        Guard.NonNegative(basePriceAmount, "product-price-invalid", "Base price amount");

        if (Sku == normalizedSku
            && Name == normalizedName
            && BasePriceAmount == basePriceAmount
            && BasePriceCurrencyCode == normalizedCurrency)
        {
            return;
        }

        Raise(new ProductChanged(normalizedSku, normalizedName, basePriceAmount, normalizedCurrency));
    }

    public void AddPackaging(PackagingDefinition definition)
    {
        EnsureActive();
        PackagingDefinition normalized = NormalizePackaging(definition, BaseUomCode);
        if (normalized.Level == 0)
        {
            throw new DomainException("base-packaging-already-exists", "Level zero packaging is created with the product.");
        }

        EnsurePackagingUnique(normalized, null);
        Raise(new ProductPackagingAdded(normalized));
    }

    public void ChangePackaging(PackagingDefinition definition)
    {
        EnsureActive();
        if (!_packagings.TryGetValue(definition.Id, out ProductPackaging? current))
        {
            throw new NotFoundException(
                "product-packaging-not-found",
                $"Packaging '{definition.Id}' was not found.");
        }

        PackagingDefinition normalized = NormalizePackaging(definition, BaseUomCode);
        if (current.Level == 0 && (normalized.Level != 0 || normalized.UomCode != BaseUomCode || normalized.ConversionFactor != 1m))
        {
            throw new DomainException("base-packaging-immutable", "The level, UoM code, and conversion factor of level zero packaging are immutable.");
        }

        if (current.Level != 0 && normalized.Level == 0)
        {
            throw new DomainException("base-packaging-already-exists", "A product can have only one level zero packaging.");
        }

        EnsurePackagingUnique(normalized, definition.Id);
        Raise(new ProductPackagingChanged(normalized));
    }

    public void RemovePackaging(Guid packagingId)
    {
        EnsureActive();
        if (!_packagings.TryGetValue(packagingId, out ProductPackaging? packaging))
        {
            throw new NotFoundException(
                "product-packaging-not-found",
                $"Packaging '{packagingId}' was not found.");
        }

        if (packaging.Level == 0)
        {
            throw new DomainException("base-packaging-required", "Level zero packaging cannot be removed.");
        }

        Raise(new ProductPackagingRemoved(packagingId));
    }

    public void ClaimUsage(Guid operationId, string usageType)
    {
        EnsureActive();
        if (operationId == Guid.Empty)
        {
            throw new DomainException("usage-operation-id-required", "Usage operation id is required.");
        }

        string normalizedType = Guard.Required(
            usageType,
            "usage-type-invalid",
            "Usage type",
            ProductRules.UsageTypeMaximumLength);

        if (_usageClaims.TryGetValue(operationId, out string? existing))
        {
            if (!string.Equals(existing, normalizedType, StringComparison.Ordinal))
            {
                throw new ConflictException(
                    ProductFailureCode.UsageClaimConflict,
                    "The operation id is already used by a different claim.");
            }

            return;
        }

        Raise(new ProductUsageClaimed(operationId, normalizedType));
    }

    public void ReleaseUsage(Guid operationId)
    {
        if (_usageClaims.ContainsKey(operationId))
        {
            Raise(new ProductUsageReleased(operationId));
        }
    }

    public void Delete()
    {
        EnsureActive();
        if (_usageClaims.Count > 0)
        {
            throw new ConflictException("product-in-use", "The product has an active operation and cannot be deleted.");
        }

        Raise(new ProductDeleted());
    }

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case ProductCreated created:
                Id = created.ProductId;
                Sku = created.Sku;
                Name = created.Name;
                BaseUomCode = created.BaseUomCode;
                BasePriceAmount = created.BasePriceAmount;
                BasePriceCurrencyCode = created.BasePriceCurrencyCode;
                break;
            case ProductChanged changed:
                Sku = changed.Sku;
                Name = changed.Name;
                BasePriceAmount = changed.BasePriceAmount;
                BasePriceCurrencyCode = changed.BasePriceCurrencyCode;
                break;
            case ProductPackagingAdded added:
                _packagings.Add(added.Packaging.Id, ToPackaging(added.Packaging));
                break;
            case ProductPackagingChanged changed:
                _packagings[changed.Packaging.Id] = ToPackaging(changed.Packaging);
                break;
            case ProductPackagingRemoved removed:
                _packagings.Remove(removed.PackagingId);
                break;
            case ProductUsageClaimed claimed:
                _usageClaims[claimed.OperationId] = claimed.UsageType;
                break;
            case ProductUsageReleased released:
                _usageClaims.Remove(released.OperationId);
                break;
            case ProductDeleted:
                IsDeleted = true;
                break;
            default:
                throw new InvalidOperationException($"Unsupported event '{domainEvent.GetType().Name}'.");
        }
    }

    private static string NormalizeRequired(string value, int maximumLength, string code, string fieldName)
    {
        return Guard.Required(value, code, fieldName, maximumLength).ToUpperInvariant();
    }

    private static string NormalizeCurrency(string value)
    {
        string normalized = NormalizeRequired(value, ProductRules.CurrencyCodeLength, "product-currency-invalid", "Currency code");
        if (normalized.Length != ProductRules.CurrencyCodeLength || !normalized.All(character => character is >= 'A' and <= 'Z'))
        {
            throw new DomainException("product-currency-invalid", "Currency code must contain exactly three ASCII letters.");
        }

        return normalized;
    }

    private static PackagingDefinition NormalizePackaging(PackagingDefinition definition, string baseUomCode)
    {
        if (definition.Id == Guid.Empty)
        {
            throw new DomainException("packaging-id-required", "Packaging id is required.");
        }

        if (definition.Level < 0)
        {
            throw new DomainException("packaging-level-invalid", "Packaging level cannot be negative.");
        }

        string uomCode = NormalizeRequired(definition.UomCode, ProductRules.UomCodeMaximumLength, "packaging-uom-invalid", "Packaging UoM code");
        Guard.Positive(definition.ConversionFactor, "packaging-conversion-invalid", "Conversion factor");

        if (definition.Level == 0 && (uomCode != baseUomCode || definition.ConversionFactor != 1m))
        {
            throw new DomainException("base-packaging-invalid", "Level zero packaging must use the base UoM code and a conversion factor of one.");
        }

        string? barcode = string.IsNullOrWhiteSpace(definition.Barcode)
            ? null
            : Guard.Required(definition.Barcode, "packaging-barcode-invalid", "Barcode", ProductRules.BarcodeMaximumLength);

        ValidateOptionalPositive(definition.WeightInKg, "Weight in kg");
        ValidateOptionalPositive(definition.LengthInMm, "Length in mm");
        ValidateOptionalPositive(definition.WidthInMm, "Width in mm");
        ValidateOptionalPositive(definition.HeightInMm, "Height in mm");

        return definition with { UomCode = uomCode, Barcode = barcode };
    }

    private static void ValidatePackagingSet(PackagingDefinition[] definitions, string baseUomCode)
    {
        if (definitions.Length == 0 || definitions.Count(item => item.Level == 0) != 1)
        {
            throw new DomainException("base-packaging-required", "Exactly one level zero packaging is required.");
        }

        var normalized = definitions.Select(item => NormalizePackaging(item, baseUomCode)).ToArray();
        if (normalized.Select(item => item.Id).Distinct().Count() != normalized.Length
            || normalized.Select(item => item.Level).Distinct().Count() != normalized.Length
            || normalized.Select(item => item.UomCode).Distinct(StringComparer.Ordinal).Count() != normalized.Length
            || normalized.Where(item => item.Barcode is not null).Select(item => item.Barcode).Distinct(StringComparer.Ordinal).Count()
                != normalized.Count(item => item.Barcode is not null))
        {
            throw new DomainException("product-packaging-duplicate", "Packaging ids, levels, UoM codes, and non-empty barcodes must be unique.");
        }
    }

    private void EnsurePackagingUnique(PackagingDefinition definition, Guid? excludedId)
    {
        IEnumerable<ProductPackaging> others = _packagings.Values.Where(item => item.Id != excludedId);
        if (others.Any(item => item.Level == definition.Level
                || item.UomCode == definition.UomCode
                || (definition.Barcode is not null && item.Barcode == definition.Barcode)))
        {
            throw new DomainException("product-packaging-duplicate", "Packaging level, UoM code, and non-empty barcode must be unique.");
        }
    }

    private void EnsureActive()
    {
        if (IsDeleted)
        {
            throw new ConflictException(ProductFailureCode.ProductDeleted, "The product has been deleted.");
        }
    }

    private static void ValidateOptionalPositive(decimal? value, string fieldName)
    {
        if (value is <= 0)
        {
            throw new DomainException("packaging-measurement-invalid", $"{fieldName} must be positive when supplied.");
        }
    }

    private static ProductPackaging ToPackaging(PackagingDefinition definition)
    {
        return new ProductPackaging
        {
            Id = definition.Id,
            Level = definition.Level,
            UomCode = definition.UomCode,
            ConversionFactor = definition.ConversionFactor,
            Barcode = definition.Barcode,
            WeightInKg = definition.WeightInKg,
            LengthInMm = definition.LengthInMm,
            WidthInMm = definition.WidthInMm,
            HeightInMm = definition.HeightInMm,
        };
    }
}
