using Api.Core.Aggregates.CustomerAggregate;
using Api.Core.Entities.Identity;

namespace Api.Infrastructure.Data.Config.Identity;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
  public void Configure(EntityTypeBuilder<ApplicationUser> builder)
  {
    // Map to Identity table name
    builder.ToTable("AspNetUsers");

    // Primary Key (inherited from AuditableEntity<int>)
    builder.HasKey(u => u.Id);

    // Indexes for performance
    builder.HasIndex(u => u.NormalizedUserName)
      .IsUnique()
      .HasDatabaseName("UserNameIndex");

    builder.HasIndex(u => u.NormalizedEmail)
      .HasDatabaseName("EmailIndex");

    builder.HasIndex(u => u.Email);
    builder.HasIndex(u => u.IsActive);

    // Property Configurations
    builder.Property(u => u.UserName).HasMaxLength(256).IsRequired();
    builder.Property(u => u.NormalizedUserName).HasMaxLength(256).IsRequired();
    builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
    builder.Property(u => u.NormalizedEmail).HasMaxLength(256);
    builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
    builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
    builder.Property(u => u.PhoneNumber).HasMaxLength(20);
    builder.Property(u => u.PasswordHash).HasMaxLength(500);
    builder.Property(u => u.SecurityStamp).HasMaxLength(100);
    builder.Property(u => u.ConcurrencyStamp).HasMaxLength(100);

    // Concurrency token (RowVersion from AuditableEntity)
    builder.Property(u => u.RowVersion).IsRowVersion();

    // Relationships
    builder.HasMany(u => u.UserRoles)
      .WithOne(ur => ur.User)
      .HasForeignKey(ur => ur.UserId)
      .IsRequired()
      .OnDelete(DeleteBehavior.Cascade);

    // Optional: Link to Customer
    builder.HasOne<Customer>()
      .WithMany()
      .HasForeignKey(u => u.CustomerId)
      .OnDelete(DeleteBehavior.SetNull)
      .IsRequired(false);
  }
}
