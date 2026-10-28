using BankApp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankApp.Data.Configurations;

public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.Property(t => t.ReferenceNo).HasMaxLength(20).IsRequired();
        builder.HasIndex(t => t.ReferenceNo).IsUnique();

        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.Fee).HasPrecision(18, 2);
        builder.Property(t => t.Description).HasMaxLength(200);

        // Bir hesabın iki farklı ilişkisi var (gönderen / alıcı), bu yüzden açıkça tanımlanır.
        builder.HasOne(t => t.FromAccount)
            .WithMany()
            .HasForeignKey(t => t.FromAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ToAccount)
            .WithMany()
            .HasForeignKey(t => t.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
