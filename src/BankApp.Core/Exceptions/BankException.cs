namespace BankApp.Core.Exceptions;

/// <summary>
/// İş kuralı ihlallerinde fırlatılan özel hata.
/// Mesajı doğrudan kullanıcıya gösterilebilecek şekilde yazılır.
/// </summary>
public class BankException : Exception
{
    public BankException(string message) : base(message) { }
}

public class InsufficientBalanceException : BankException
{
    public InsufficientBalanceException(decimal required, decimal available)
        : base($"Yetersiz bakiye. Gerekli tutar: {required:N2} TL, kullanılabilir bakiye: {available:N2} TL.") { }
}

public class AccountNotFoundException : BankException
{
    public AccountNotFoundException(string message = "Hesap bulunamadı.") : base(message) { }
}
