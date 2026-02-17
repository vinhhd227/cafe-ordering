using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.CustomerAggregate;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Api.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<
  ApplicationUser,
  ApplicationRole,
  int,
  IdentityUserClaim<int>,
  ApplicationUserRole,
  IdentityUserLogin<int>,
  IdentityRoleClaim<int>,
  IdentityUserToken<int>>
{
  private readonly ICurrentUserService? _currentUserService;

  public AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUserService? currentUserService = null) : base(options)
  {
    _currentUserService = currentUserService;
  }

  // Domain Entities
  public DbSet<Product> Products => Set<Product>();
  public DbSet<Category> Categories => Set<Category>();
  public DbSet<Customer> Customers => Set<Customer>();  // Added - Customer aggregate

  // Identity Entities (exposed for querying)
  public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
  public DbSet<ApplicationRole> ApplicationRoles => Set<ApplicationRole>();

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

    // Handle AuditableEntity<int> (existing entities like Product, Category, Order)
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

    // Handle AuditableEntity<string> (Customer with string ID)
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
