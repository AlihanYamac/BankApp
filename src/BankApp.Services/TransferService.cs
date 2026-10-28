using BankApp.Core.Entities;
using BankApp.Core.Enums;
using BankApp.Core.Exceptions;
using BankApp.Core.Helpers;
using BankApp.Data;
using BankApp.Services.Dtos;
using BankApp.Services.Interfaces;
using BankApp.Services.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankApp.Services;

/// <summary>
/// Para transferi iş kuralları:
///  - Kendi hesapları arası transfer: 0 TL komisyon
///  - Başka müşterinin hesabına transfer: 3,99 TL komisyon (appsettings'ten okunur)
///  - Gönderen hesaptan "tutar + komisyon" düşülür, alıcıya sadece "tutar" geçer
///  - Her adım hesap hareketi (AccountTransaction) olarak kaydedilir; komisyon ayrı satırdır
///  - Tüm işlem tek bir veritabanı transaction'ı içinde yapılır (ya hepsi ya hiçbiri)
/// </summary>
public class TransferService : ITransferService
{
    private readonly BankDbContext _db;
    private readonly BankOptions _options;
    private readonly ILogger<TransferService> _logger;

    public TransferService(BankDbContext db, IOptions<BankOptions> options, ILogger<TransferService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public decimal CalculateFee(bool isOwnAccount) =>
        isOwnAccount ? 0m : _options.OtherCustomerTransferFee;

    public async Task<TransferPreview> PreviewAsync(int customerId, TransferRequest request)
    {
        var (from, to) = await LoadAndValidateAsync(customerId, request);
        var isOwn = from.CustomerId == to.CustomerId;
        var fee = CalculateFee(isOwn);

        return new TransferPreview(
            from.Name,
            from.Iban,
            to.Iban,
            isOwn ? to.Customer.FullName : MaskHelper.MaskName(to.Customer.FullName),
            isOwn ? TransferType.OwnAccount : TransferType.OtherCustomer,
            request.Amount,
            fee,
            request.Amount + fee,
            from.Balance);
    }

    public async Task<TransferResult> TransferAsync(int customerId, TransferRequest request)
    {
        // Transaction: Aşağıdaki tüm veritabanı işlemleri tek bir bütün olarak çalışır.
        // Arada bir hata olursa Commit çağrılmaz ve hiçbir değişiklik kalıcı olmaz (Atomicity).
        // SQLite'ta bu transaction yazma kilidini hemen alır; bu sayede aynı anda gelen
        // iki transfer aynı bakiyeyi okuyup "çift harcama" yapamaz.
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();

        var (from, to) = await LoadAndValidateAsync(customerId, request);

        var isOwn = from.CustomerId == to.CustomerId;
        var fee = CalculateFee(isOwn);
        var totalDebit = request.Amount + fee;

        if (from.Balance < totalDebit)
            throw new InsufficientBalanceException(totalDebit, from.Balance);

        var now = DateTime.UtcNow;
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        var transfer = new Transfer
        {
            ReferenceNo = GenerateReferenceNo(),
            FromAccountId = from.Id,
            ToAccountId = to.Id,
            Amount = request.Amount,
            Fee = fee,
            Type = isOwn ? TransferType.OwnAccount : TransferType.OtherCustomer,
            Description = description,
            CreatedAt = now
        };
        _db.Transfers.Add(transfer);

        // 1) Gönderen hesaptan tutar düşülür
        from.Balance -= request.Amount;
        _db.AccountTransactions.Add(new AccountTransaction
        {
            Account = from,
            Transfer = transfer,
            Type = TransactionType.TransferOut,
            Amount = -request.Amount,
            BalanceAfter = from.Balance,
            Description = BuildDescription(isOwn ? "Kendi hesabıma transfer" : "Giden transfer", description),
            CounterpartyIban = to.Iban,
            CounterpartyName = to.Customer.FullName,
            CreatedAt = now
        });

        // 2) Komisyon varsa ayrı bir hesap hareketi olarak düşülür
        if (fee > 0)
        {
            from.Balance -= fee;
            _db.AccountTransactions.Add(new AccountTransaction
            {
                Account = from,
                Transfer = transfer,
                Type = TransactionType.Commission,
                Amount = -fee,
                BalanceAfter = from.Balance,
                Description = $"Transfer komisyonu ({transfer.ReferenceNo})",
                CreatedAt = now
            });
        }

        // 3) Alıcı hesaba tutar eklenir
        to.Balance += request.Amount;
        _db.AccountTransactions.Add(new AccountTransaction
        {
            Account = to,
            Transfer = transfer,
            Type = TransactionType.TransferIn,
            Amount = request.Amount,
            BalanceAfter = to.Balance,
            Description = BuildDescription(isOwn ? "Kendi hesabımdan gelen transfer" : "Gelen transfer", description),
            CounterpartyIban = from.Iban,
            CounterpartyName = from.Customer.FullName,
            CreatedAt = now
        });

        await _db.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        _logger.LogInformation(
            "Transfer tamamlandı. Ref: {ReferenceNo}, {From} -> {To}, Tutar: {Amount}, Komisyon: {Fee}",
            transfer.ReferenceNo, from.Iban, to.Iban, request.Amount, fee);

        return new TransferResult(
            transfer.ReferenceNo,
            transfer.CreatedAt,
            from.Iban,
            to.Iban,
            isOwn ? to.Customer.FullName : MaskHelper.MaskName(to.Customer.FullName),
            transfer.Type,
            transfer.Amount,
            fee,
            totalDebit,
            from.Balance,
            description);
    }

    public async Task<IReadOnlyList<TransferHistoryDto>> GetHistoryAsync(int customerId)
    {
        var transfers = await _db.Transfers
            .AsNoTracking()
            .Include(t => t.FromAccount).ThenInclude(a => a.Customer)
            .Include(t => t.ToAccount).ThenInclude(a => a.Customer)
            .Where(t => t.FromAccount.CustomerId == customerId || t.ToAccount.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

        return transfers.Select(t =>
        {
            var isIncoming = t.ToAccount.CustomerId == customerId && t.FromAccount.CustomerId != customerId;
            var counterparty = isIncoming ? t.FromAccount.Customer.FullName : t.ToAccount.Customer.FullName;
            return new TransferHistoryDto(
                t.ReferenceNo,
                t.CreatedAt,
                t.FromAccount.Iban,
                t.ToAccount.Iban,
                counterparty,
                t.Type,
                isIncoming,
                t.Amount,
                isIncoming ? 0m : t.Fee,   // Gelen transferde komisyonu alıcı ödemez
                t.Description);
        }).ToList();
    }

    /// <summary>Ortak doğrulamalar: tutar, IBAN, hesap sahipliği.</summary>
    private async Task<(Account From, Account To)> LoadAndValidateAsync(int customerId, TransferRequest request)
    {
        if (request.Amount <= 0)
            throw new BankException("Transfer tutarı sıfırdan büyük olmalıdır.");
        if (decimal.Round(request.Amount, 2) != request.Amount)
            throw new BankException("Tutar en fazla 2 ondalık basamak içerebilir.");
        if (request.Amount > _options.MaxTransferAmount)
            throw new BankException($"Tek seferde en fazla {_options.MaxTransferAmount:N2} TL gönderilebilir.");

        var toIban = IbanHelper.Normalize(request.ToIban);
        if (!IbanHelper.IsValid(toIban))
            throw new BankException("Geçersiz IBAN. Lütfen kontrol edip tekrar deneyin.");

        var from = await _db.Accounts
            .Include(a => a.Customer)
            .SingleOrDefaultAsync(a => a.Id == request.FromAccountId && a.CustomerId == customerId && a.IsActive)
            ?? throw new AccountNotFoundException("Gönderen hesap bulunamadı.");

        var to = await _db.Accounts
            .Include(a => a.Customer)
            .SingleOrDefaultAsync(a => a.Iban == toIban && a.IsActive)
            ?? throw new AccountNotFoundException("Bu IBAN'a ait aktif bir hesap bulunamadı.");

        if (from.Id == to.Id)
            throw new BankException("Gönderen ve alıcı hesap aynı olamaz.");

        return (from, to);
    }

    private static string BuildDescription(string title, string? userDescription) =>
        userDescription is null ? title : $"{title} - {userDescription}";

    private static string GenerateReferenceNo() =>
        "TRF" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
}
