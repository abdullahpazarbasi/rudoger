using Microsoft.EntityFrameworkCore;
using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.Modules.Inventory.Domain;

namespace Rudoger.Modules.Inventory.Infrastructure;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : EventSourcedDbContext(options, "inventory")
{
    public DbSet<StockItemReadEntity> StockItems => Set<StockItemReadEntity>();

    public DbSet<StockMovementReadEntity> StockMovements => Set<StockMovementReadEntity>();

    public DbSet<InventoryOutboxMessageEntity> OutboxMessages => Set<InventoryOutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StockItemReadEntity>(builder =>
        {
            builder.ToTable("StockItems");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.BaseUomCode).HasMaxLength(InventoryRules.UomCodeMaximumLength).IsRequired();
            builder.Property(item => item.CreationIdempotencyKey)
                .HasMaxLength(InventoryRules.IdempotencyKeyMaximumLength)
                .IsRequired();
            builder.Property(item => item.OpeningUomCode)
                .HasMaxLength(InventoryRules.UomCodeMaximumLength)
                .IsRequired();
            builder.Property(item => item.OpeningQuantity).HasPrecision(19, 6);
            builder.Property(item => item.RequestedOpeningQuantity).HasPrecision(19, 6);
            builder.Property(item => item.OnHandQuantity).HasPrecision(19, 6);
            builder.Property(item => item.ReservedQuantity).HasPrecision(19, 6);
            builder.HasIndex(item => item.ProductId).IsUnique();
            builder.HasIndex(item => item.CreationIdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<StockMovementReadEntity>(builder =>
        {
            builder.ToTable("StockMovements");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(item => item.UomCode).HasMaxLength(InventoryRules.UomCodeMaximumLength).IsRequired();
            builder.Property(item => item.Quantity).HasPrecision(19, 6);
            builder.Property(item => item.OnHandQuantityDelta).HasPrecision(19, 6);
            builder.Property(item => item.ReservedQuantityDelta).HasPrecision(19, 6);
            builder.Property(item => item.ReferenceType).HasMaxLength(InventoryRules.ReferenceTypeMaximumLength).IsRequired();
            builder.Property(item => item.IdempotencyKey).HasMaxLength(InventoryRules.IdempotencyKeyMaximumLength).IsRequired();
            builder.Property(item => item.CorrelationId).HasMaxLength(InventoryRules.CorrelationIdMaximumLength).IsRequired();
            builder.Property(item => item.OccurredAtUtc).HasPrecision(7);
            builder.HasIndex(item => item.IdempotencyKey).IsUnique();
            // AGENTS.md pins the persisted StockMovement field name, so only the CLR name changes.
            builder.Property(item => item.OperationId).HasColumnName("SourceEventId");
            builder.HasIndex(item => item.OperationId).IsUnique();
            builder.HasIndex(item => new { item.ReferenceType, item.ReferenceId });
            builder.HasOne<StockItemReadEntity>()
                .WithMany()
                .HasForeignKey(item => item.StockItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryOutboxMessageEntity>(builder =>
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.OccurredAtUtc).HasPrecision(7);
            builder.Property(item => item.NextAttemptAtUtc).HasPrecision(7);
            builder.Property(item => item.LockedUntilUtc).HasPrecision(7);
            builder.Property(item => item.ProcessedAtUtc).HasPrecision(7);
            builder.Property(item => item.LastError).HasMaxLength(2000);
            builder.HasIndex(item => new { item.ProductId, item.OperationId }).IsUnique();
            builder.HasIndex(item => new { item.ProcessedAtUtc, item.NextAttemptAtUtc });
        });
    }
}
