using BankApp.Core.Enums;

namespace BankApp.Services.Dtos;

// DTO (Data Transfer Object): Katmanlar arasında veri taşımak için kullanılan basit sınıflar.
// Entity'leri doğrudan arayüze vermek yerine sadece gereken alanları taşırız.

public record RegisterRequest(
    string FirstName,
    string LastName,
    string NationalId,
    string Email,
    string Password);

public record CustomerDto(int Id, string FullName, string Email);

public record AccountDto(
    int Id,
    string Name,
    string Iban,
    decimal Balance,
    string Currency,
    DateTime CreatedAt);

public record TransactionDto(
    int Id,
    DateTime CreatedAt,
    TransactionType Type,
    string Description,
    decimal Amount,
    decimal BalanceAfter,
    string? CounterpartyIban,
    string? CounterpartyName,
    string? ReferenceNo);

public record TransferRequest(
    int FromAccountId,
    string ToIban,
    decimal Amount,
    string? Description);

/// <summary>Transfer onaylanmadan önce kullanıcıya gösterilen özet.</summary>
public record TransferPreview(
    string FromAccountName,
    string FromIban,
    string ToIban,
    string RecipientName,
    TransferType Type,
    decimal Amount,
    decimal Fee,
    decimal TotalDebit,
    decimal CurrentBalance);

/// <summary>Başarılı transfer sonrası dekont bilgisi.</summary>
public record TransferResult(
    string ReferenceNo,
    DateTime CreatedAt,
    string FromIban,
    string ToIban,
    string RecipientName,
    TransferType Type,
    decimal Amount,
    decimal Fee,
    decimal TotalDebit,
    decimal NewBalance,
    string? Description);

public record TransferHistoryDto(
    string ReferenceNo,
    DateTime CreatedAt,
    string FromIban,
    string ToIban,
    string CounterpartyName,
    TransferType Type,
    bool IsIncoming,
    decimal Amount,
    decimal Fee,
    string? Description);
