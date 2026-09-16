using Microsoft.EntityFrameworkCore;
using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Infrastructure;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options)
    : EventSourcedDbContext(options, "order")
{
    public DbSet<OrderReadEntity> Orders => Set<OrderReadEntity>();

    public DbSet<OrderLineReadEntity> OrderLines => Set<OrderLineReadEntity>();

    public DbSet<OrderPlacementReadEntity> OrderPlacements => Set<OrderPlacementReadEntity>();

    public DbSet<OrderPlacementLineReadEntity> OrderPlacementLines => Set<OrderPlacementLineReadEntity>();

    public DbSet<OrderTransitionReadEntity> OrderTransitions => Set<OrderTransitionReadEntity>();

    public DbSet<OrderOutboxMessageEntity> OutboxMessages => Set<OrderOutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrderReadEntity>(builder =>
        {
            builder.ToTable("Orders");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.OrderNumber).HasMaxLength(OrderRules.OrderNumberMaximumLength).IsRequired();
            builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(item => item.PendingTransitionTarget).HasConversion<string>().HasMaxLength(20);
            builder.HasIndex(item => item.OrderNumber).IsUnique();
            builder.HasIndex(item => item.UserId);
        });

        modelBuilder.Entity<OrderLineReadEntity>(builder =>
        {
            builder.ToTable("OrderLines");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.UomCode).HasMaxLength(OrderRules.UomCodeMaximumLength).IsRequired();
            builder.Property(item => item.Quantity).HasPrecision(19, 6);
            builder.Property(item => item.UnitPriceAmount).HasPrecision(19, 4);
            builder.Property(item => item.UnitPriceCurrencyCode).HasMaxLength(OrderRules.CurrencyCodeLength).IsRequired();
            builder.Property(item => item.BaseQuantity).HasPrecision(19, 6);
            builder.HasIndex(item => new { item.OrderId, item.Num }).IsUnique();
            builder.HasIndex(item => item.ProductId);
            builder.HasOne(item => item.Order)
                .WithMany(item => item.Lines)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderPlacementReadEntity>(builder =>
        {
            builder.ToTable("OrderPlacements");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.IdempotencyKey).HasMaxLength(OrderRules.IdempotencyKeyMaximumLength).IsRequired();
            builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(item => item.FailureCode).HasMaxLength(OrderRules.FailureCodeMaximumLength);
            builder.Property(item => item.FailureDetail).HasMaxLength(OrderRules.FailureDetailMaximumLength);
            builder.HasIndex(item => item.IdempotencyKey).IsUnique();
            builder.HasIndex(item => item.OrderId).IsUnique();
        });

        modelBuilder.Entity<OrderPlacementLineReadEntity>(builder =>
        {
            builder.ToTable("OrderPlacementLines");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.UomCode).HasMaxLength(OrderRules.UomCodeMaximumLength).IsRequired();
            builder.Property(item => item.Quantity).HasPrecision(19, 6);
            builder.HasIndex(item => new { item.PlacementId, item.Num }).IsUnique();
            builder.HasOne(item => item.Placement)
                .WithMany(item => item.Lines)
                .HasForeignKey(item => item.PlacementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderTransitionReadEntity>(builder =>
        {
            builder.ToTable("OrderTransitions");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Target).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.HasIndex(item => item.OrderId);
        });

        modelBuilder.Entity<OrderOutboxMessageEntity>(builder =>
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(item => item.OccurredAtUtc).HasPrecision(7);
            builder.Property(item => item.ProcessedAtUtc).HasPrecision(7);
            builder.Property(item => item.LockedUntilUtc).HasPrecision(7);
            builder.Property(item => item.NextAttemptAtUtc).HasPrecision(7);
            builder.Property(item => item.LastError).HasMaxLength(2000);
            builder.HasIndex(item => new { item.ProcessedAtUtc, item.NextAttemptAtUtc });
        });
    }
}
