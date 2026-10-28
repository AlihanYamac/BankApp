using BankApp.Core.Enums;

namespace BankApp.Core.Entities;

/// <summary>
/// Hesap hareketi (hesap ekstresindeki bir satır).
/// Tutar işaretlidir: hesaba giren para pozitif (+), çıkan para negatif (-).
/// Komisyon da ayrı bir hareket satırı olarak kaydedilir.
/// </summary>
public class AccountTransaction
{
    public int Id { get; set; }

    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;

    /// <summary>Hareket bir transferden doğduysa ilgili transfer.</summary>
    public int? TransferId { get; set; }
    public Transfer? Transfer { get; set; }

    public TransactionType Type { get; set; }

    /// <summary>İşaretli tutar: + giriş, - çıkış.</summary>
    public decimal Amount { get; set; }

    /// <summary>Bu hareketten sonraki hesap bakiyesi (ekstrede gösterilir).</summary>
    public decimal BalanceAfter { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Karşı tarafın IBAN'ı (varsa).</summary>
    public string? CounterpartyIban { get; set; }

    /// <summary>Karşı tarafın adı (varsa).</summary>
    public string? CounterpartyName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
