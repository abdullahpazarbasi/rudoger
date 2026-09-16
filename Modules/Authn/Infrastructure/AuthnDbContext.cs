using Microsoft.EntityFrameworkCore;
using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.Modules.Authn.Domain;

namespace Rudoger.Modules.Authn.Infrastructure;

public sealed class AuthnDbContext(DbContextOptions<AuthnDbContext> options)
    : EventSourcedDbContext(options, "authn")
{
    public DbSet<UserReadEntity> Users => Set<UserReadEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<UserReadEntity>(builder =>
        {
            builder.ToTable("Users");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Username).HasMaxLength(UserRules.UsernameMaximumLength).IsRequired();
            builder.Property(item => item.PasswordHash).HasMaxLength(UserRules.PasswordHashMaximumLength).IsRequired();
            builder.HasIndex(item => item.Username).IsUnique();
        });
    }
}
