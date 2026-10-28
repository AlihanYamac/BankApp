using BankApp.Core.Enums;

namespace BankApp.Core.Entities;

/// <summary>
/// Bir para transferi işlemi. Gönderen ve alıcı hesap, tutar ve komisyon bilgisini tutar.
/// Hesap hareketleri (AccountTransaction) bu kayda TransferId ile bağlanır.
/// </summary>
public class Transfer
{
    public int Id { get; set; }

    /// <summary>Kullanıcıya gösterilen işlem referans numarası (dekont no).</summary>
    public string ReferenceNo { get; set; } = string.Empty;

    public int FromAccountId { get; set; }
    public Account FromAccount { get; set; } = null!;

    public int ToAccountId { get; set; }
    public Account ToAccount { get; set; } = null!;

    public decimal Amount { get; set; }

    /// <summary>Alınan komisyon. Kendi hesaplar arası 0, başka müşteriye 3,99 TL.</summary>
    public decimal Fee { get; set; }

    public TransferType Type { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
