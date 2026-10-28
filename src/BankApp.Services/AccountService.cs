using BankApp.Core.Entities;
using BankApp.Core.Enums;
using BankApp.Core.Exceptions;
using BankApp.Core.Helpers;
using BankApp.Data;
using BankApp.Services.Dtos;
using BankApp.Services.Interfaces;
using BankApp.Services.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BankApp.Services;

public class AccountService : IAccountService
{
    private readonly BankDbContext _db;
    private readonly BankOptions _options;

    public AccountService(BankDbContext db, IOptions<BankOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(int customerId)
    {
        var accounts = await _db.Accounts
            .AsNoTracking()   // Sadece okuma yapılacağı için değişiklik takibi kapatılır (performans).
            .Where(a => a.CustomerId == customerId && a.IsActive)
            .OrderBy(a => a.Id)
            .ToListAsync();

        return accounts.Select(ToDto).ToList();
    }

    public async Task<AccountDto> GetAccountAsync(int customerId, int accountId)
    {
        var account = await FindOwnedAccountAsync(customerId, accountId);
        return ToDto(account);
    }

    public async Task<AccountDto> OpenAccountAsync(int customerId, string name)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new BankException("Hesap adı boş olamaz.");

        var accountCount = await _db.Accounts.CountAsync(a => a.CustomerId == customerId && a.IsActive);
        if (accountCount >= _options.MaxAccountsPerCustomer)
            throw new BankException($"En fazla {_options.MaxAccountsPerCustomer} hesap açabilirsiniz.");

        var account = new Account
        {
            CustomerId = customerId,
            Name = name,
            Iban = await GenerateUniqueIbanAsync(),
            Balance = 0m
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        return ToDto(account);
    }

    /// <summary>
    /// Demo amaçlı para yatırma (ATM / şubeden yatırma simülasyonu).
    /// Hesap hareketlerine "Para Yatırma" satırı olarak düşer.
    /// </summary>
    public async Task<AccountDto> DepositAsync(int customerId, int accountId, decimal amount)
    {
        if (amount <= 0)
            throw new BankException("Tutar sıfırdan büyük olmalıdır.");
        if (decimal.Round(amount, 2) != amount)
            throw new BankException("Tutar en fazla 2 ondalık basamak içerebilir.");
        if (amount > _options.MaxDepositAmount)
            throw new BankException($"Tek seferde en fazla {_options.MaxDepositAmount:N2} TL yatırılabilir.");

        await using var dbTransaction = await _db.Database.BeginTransactionAsync();

        var account = await FindOwnedAccountAsync(customerId, accountId);
        account.Balance += amount;

        _db.AccountTransactions.Add(new AccountTransaction
        {
            AccountId = account.Id,
            Type = TransactionType.Deposit,
            Amount = amount,
            BalanceAfter = account.Balance,
            Description = "Para yatırma"
        });

        await _db.SaveChangesAsync();
        await dbTransaction.CommitAsync();
        return ToDto(account);
    }

    public async Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(int customerId, int accountId)
    {
        await FindOwnedAccountAsync(customerId, accountId); // Yetki kontrolü: hesap bu müşterinin mi?

        return await _db.AccountTransactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Select(t => new TransactionDto(
                t.Id,
                t.CreatedAt,
                t.Type,
                t.Description,
                t.Amount,
                t.BalanceAfter,
                t.CounterpartyIban,
                t.CounterpartyName,
                t.Transfer != null ? t.Transfer.ReferenceNo : null))
            .ToListAsync();
    }

    /// <summary>
    /// Hesabı getirir; hesap yoksa veya başka müşteriye aitse hata fırlatır.
    /// Böylece bir kullanıcı URL'deki id'yi değiştirerek başkasının hesabını göremez.
    /// </summary>
    private async Task<Account> FindOwnedAccountAsync(int customerId, int accountId)
    {
        return await _db.Accounts.SingleOrDefaultAsync(a =>
                   a.Id == accountId && a.CustomerId == customerId && a.IsActive)
               ?? throw new AccountNotFoundException();
    }

    /// <summary>Rastgele IBAN üretir; veritabanında varsa yenisini dener.</summary>
    private async Task<string> GenerateUniqueIbanAsync()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var iban = IbanHelper.GenerateRandom();
            if (!await _db.Accounts.AnyAsync(a => a.Iban == iban))
                return iban;
        }
        throw new BankException("IBAN üretilemedi, lütfen tekrar deneyin.");
    }

    private static AccountDto ToDto(Account a) =>
        new(a.Id, a.Name, a.Iban, a.Balance, a.Currency, a.CreatedAt);
}
