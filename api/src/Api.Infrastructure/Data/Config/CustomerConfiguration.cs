using Api.Core.Aggregates.CustomerAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Data.Config;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
  public void Configure(EntityTypeBuilder<Customer> builder)
  {
    builder.ToTable("Customers");

    builder.HasKey(c => c.Id);
    builder.Property(c => c.Id).HasMaxLength(36);

    builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
    builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();
    builder.Property(c => c.Email).HasMaxLength(256).IsRequired();
    builder.Property(c => c.PhoneNumber).HasMaxLength(20);

    // IdentityGuid: string value only, no FK constraint (cross-DbContext link)
    builder.Property(c => c.IdentityGuid).HasMaxLength(36);
    builder.HasIndex(c => c.IdentityGuid).HasDatabaseName("IX_Customers_IdentityGuid");

    // Soft delete
    builder.Property(c => c.IsDeleted).HasDefaultValue(false);

    builder.HasIndex(c => c.Email).HasDatabaseName("IX_Customers_Email");
    builder.HasIndex(c => c.IsDeleted).HasDatabaseName("IX_Customers_IsDeleted");
  }
}
