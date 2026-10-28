namespace BankApp.Core.Entities;

/// <summary>
/// Müşteriye ait banka hesabı. Her hesabın benzersiz bir IBAN'ı vardır.
/// </summary>
public class Account
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>TR ile başlayan 26 karakterlik benzersiz IBAN.</summary>
    public string Iban { get; set; } = string.Empty;

    /// <summary>Kullanıcının hesaba verdiği isim (ör. "Maaş Hesabım").</summary>
    public string Name { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public string Currency { get; set; } = "TRY";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AccountTransaction> Transactions { get; set; } = new List<AccountTransaction>();
}
