using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.CustomerAggregate;
using Api.Core.Aggregates.ProductAggregate;

namespace Api.Infrastructure.Data;

/// <summary>
/// Business data DbContext. Contains only domain/business entities.
/// Identity data (Users, Roles) lives in AppIdentityDbContext.
/// </summary>
public class AppDbContext : DbContext
{
  private readonly ICurrentUserService? _currentUserService;

  public AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUserService? currentUserService = null) : base(options)
  {
    _currentUserService = currentUserService;
  }

  public DbSet<Product> Products => Set<Product>();
  public DbSet<Category> Categories => Set<Category>();
  public DbSet<Customer> Customers => Set<Customer>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Tất cả business tables nằm trong schema "business"
    modelBuilder.HasDefaultSchema("business");

    modelBuilder.ApplyConfigurationsFromAssembly(
      Assembly.GetExecutingAssembly(),
      t => t.Namespace?.StartsWith("Api.Infrastructure.Data.Config") == true);
  }

  public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
  {
    ApplyAuditInfo();

    return await base.SaveChangesAsync(ct);
  }

  private void ApplyAuditInfo()
  {
    var now = DateTime.UtcNow;
    var userName = _currentUserService?.UserName;

    foreach (var entry in ChangeTracker.Entries<AuditableEntity<int>>())
    {
      switch (entry.State)
      {
        case EntityState.Added:
          entry.Property(nameof(AuditableEntity<int>.CreatedAt)).CurrentValue = now;
          entry.Property(nameof(AuditableEntity<int>.CreatedBy)).CurrentValue = userName;
          entry.Property(nameof(AuditableEntity<int>.UpdatedAt)).CurrentValue = now;
          entry.Property(nameof(AuditableEntity<int>.UpdatedBy)).CurrentValue = userName;
          break;

        case EntityState.Modified:
          entry.Property(nameof(AuditableEntity<int>.UpdatedAt)).CurrentValue = now;
          entry.Property(nameof(AuditableEntity<int>.UpdatedBy)).CurrentValue = userName;
          break;
      }
    }

    foreach (var entry in ChangeTracker.Entries<AuditableEntity<string>>())
    {
      switch (entry.State)
      {
        case EntityState.Added:
          entry.Property(nameof(AuditableEntity<string>.CreatedAt)).CurrentValue = now;
          entry.Property(nameof(AuditableEntity<string>.CreatedBy)).CurrentValue = userName;
          entry.Property(nameof(AuditableEntity<string>.UpdatedAt)).CurrentValue = now;
          entry.Property(nameof(AuditableEntity<string>.UpdatedBy)).CurrentValue = userName;
          break;

        case EntityState.Modified:
          entry.Property(nameof(AuditableEntity<string>.UpdatedAt)).CurrentValue = now;
          entry.Property(nameof(AuditableEntity<string>.UpdatedBy)).CurrentValue = userName;
          break;
      }
    }
  }
}
