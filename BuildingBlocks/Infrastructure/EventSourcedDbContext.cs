using Microsoft.EntityFrameworkCore;

namespace Rudoger.BuildingBlocks.Infrastructure;

public abstract class EventSourcedDbContext(DbContextOptions options, string schema)
    : DbContext(options)
{
    private readonly string _schema = schema;

    public DbSet<EventEntity> Events => Set<EventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(_schema);
        modelBuilder.Entity<EventEntity>(builder =>
        {
            builder.ToTable("Events");
            builder.HasKey(item => item.EventId);
            builder.Property(item => item.AggregateType).HasMaxLength(100).IsRequired();
            builder.Property(item => item.EventType).HasMaxLength(200).IsRequired();
            builder.Property(item => item.Payload).IsRequired();
            builder.Property(item => item.Metadata).IsRequired();
            builder.Property(item => item.OccurredAtUtc).HasPrecision(7);
            builder.HasIndex(item => new { item.StreamId, item.Version }).IsUnique();
            builder.HasIndex(item => new { item.AggregateType, item.OccurredAtUtc });
        });
    }
}
