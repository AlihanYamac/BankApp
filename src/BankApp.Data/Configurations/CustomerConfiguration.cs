using BankApp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankApp.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(c => c.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(50).IsRequired();
        builder.Property(c => c.NationalId).HasMaxLength(11).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(100).IsRequired();
        builder.Property(c => c.PasswordHash).IsRequired();

        // Aynı TCKN veya e-posta ile ikinci müşteri açılamaz.
        builder.HasIndex(c => c.NationalId).IsUnique();
        builder.HasIndex(c => c.Email).IsUnique();

        builder.Ignore(c => c.FullName);
    }
}
