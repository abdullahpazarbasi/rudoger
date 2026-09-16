using Microsoft.EntityFrameworkCore;
using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.Modules.Product.Domain;

namespace Rudoger.Modules.Product.Infrastructure;

public sealed class ProductDbContext(DbContextOptions<ProductDbContext> options)
    : EventSourcedDbContext(options, "product")
{
    public DbSet<ProductReadEntity> Products => Set<ProductReadEntity>();

    public DbSet<ProductPackagingReadEntity> ProductPackagings => Set<ProductPackagingReadEntity>();

    public DbSet<ProductUsageClaimEntity> ProductUsageClaims => Set<ProductUsageClaimEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductReadEntity>(builder =>
        {
            builder.ToTable("Products");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Sku).HasMaxLength(ProductRules.SkuMaximumLength).IsRequired();
            builder.Property(item => item.Name).HasMaxLength(ProductRules.NameMaximumLength).IsRequired();
            builder.Property(item => item.BaseUomCode).HasMaxLength(ProductRules.UomCodeMaximumLength).IsRequired();
            builder.Property(item => item.BasePriceAmount).HasPrecision(19, 4);
            builder.Property(item => item.BasePriceCurrencyCode).HasMaxLength(ProductRules.CurrencyCodeLength).IsRequired();
            builder.HasIndex(item => item.Sku).IsUnique();
        });

        modelBuilder.Entity<ProductPackagingReadEntity>(builder =>
        {
            builder.ToTable("ProductPackagings");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.UomCode).HasMaxLength(ProductRules.UomCodeMaximumLength).IsRequired();
            builder.Property(item => item.ConversionFactor).HasPrecision(19, 6);
            builder.Property(item => item.Barcode).HasMaxLength(ProductRules.BarcodeMaximumLength);
            builder.Property(item => item.WeightInKg).HasPrecision(19, 6);
            builder.Property(item => item.LengthInMm).HasPrecision(19, 6);
            builder.Property(item => item.WidthInMm).HasPrecision(19, 6);
            builder.Property(item => item.HeightInMm).HasPrecision(19, 6);
            builder.HasIndex(item => new { item.ProductId, item.Level }).IsUnique();
            builder.HasIndex(item => new { item.ProductId, item.UomCode }).IsUnique();
            builder.HasIndex(item => item.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");
            builder.HasOne(item => item.Product)
                .WithMany(item => item.Packagings)
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductUsageClaimEntity>(builder =>
        {
            builder.ToTable("ProductUsageClaims");
            builder.HasKey(item => new { item.ProductId, item.OperationId });
            builder.Property(item => item.UsageType).HasMaxLength(ProductRules.UsageTypeMaximumLength).IsRequired();
            builder.HasIndex(item => item.OperationId);
        });
    }
}
