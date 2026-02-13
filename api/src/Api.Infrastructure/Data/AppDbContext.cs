using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.ProductAggregate;

namespace Api.Infrastructure.Data;

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

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Apply tất cả IEntityTypeConfiguration trong assembly này
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
  }

  public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
  {
    ApplyAuditInfo();

    return await base.SaveChangesAsync(ct);
  }

  /// <summary>
  ///   Tự động set CreatedAt/By, UpdatedAt/By cho AuditableEntity.
  ///   Domain events được dispatch bởi EventDispatchInterceptor (tách concern).
  /// </summary>
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
  }
}
