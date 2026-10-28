namespace BankApp.Core.Entities;

/// <summary>
/// Banka müşterisi. Bir müşterinin birden fazla hesabı olabilir (1 - N ilişki).
/// </summary>
public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>11 haneli T.C. kimlik numarası (benzersiz).</summary>
    public string NationalId { get; set; } = string.Empty;

    /// <summary>Giriş için kullanılan e-posta (benzersiz).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Şifre asla düz metin saklanmaz; sadece hash'i tutulur.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Account> Accounts { get; set; } = new List<Account>();

    public string FullName => $"{FirstName} {LastName}";
}
