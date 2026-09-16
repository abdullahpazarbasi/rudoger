using Microsoft.EntityFrameworkCore;
using Rudoger.Modules.Logging.Domain;

namespace Rudoger.Modules.Logging.Infrastructure;

public sealed class LoggingDbContext(DbContextOptions<LoggingDbContext> options) : DbContext(options)
{
    public DbSet<RequestLogEntity> RequestLogs => Set<RequestLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("logging");
        modelBuilder.Entity<RequestLogEntity>(builder =>
        {
            builder.ToTable("RequestLogs");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(item => item.CorrelationId).HasMaxLength(RequestLogRules.CorrelationIdMaximumLength).IsRequired();
            builder.Property(item => item.Method).HasMaxLength(RequestLogRules.MethodMaximumLength);
            builder.Property(item => item.Path).HasMaxLength(RequestLogRules.PathMaximumLength);
            builder.Property(item => item.Operation).HasMaxLength(RequestLogRules.OperationMaximumLength);
            builder.Property(item => item.Outcome).HasMaxLength(30).IsRequired();
            builder.Property(item => item.ProblemType).HasMaxLength(RequestLogRules.ProblemTypeMaximumLength);
            builder.Property(item => item.StartedAtUtc).HasPrecision(7);
            builder.Property(item => item.CompletedAtUtc).HasPrecision(7);
            builder.HasIndex(item => item.CorrelationId);
            builder.HasIndex(item => item.StartedAtUtc);
        });
    }
}
