using BankApp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankApp.Data.Configurations;

public class AccountTransactionConfiguration : IEntityTypeConfiguration<AccountTransaction>
{
    public void Configure(EntityTypeBuilder<AccountTransaction> builder)
    {
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.BalanceAfter).HasPrecision(18, 2);
        builder.Property(t => t.Description).HasMaxLength(200).IsRequired();
        builder.Property(t => t.CounterpartyIban).HasMaxLength(26);
        builder.Property(t => t.CounterpartyName).HasMaxLength(100);

        // Ekstre sorguları hesap + tarih üzerinden yapıldığı için index eklenir.
        builder.HasIndex(t => new { t.AccountId, t.CreatedAt });

        builder.HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Transfer)
            .WithMany()
            .HasForeignKey(t => t.TransferId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
