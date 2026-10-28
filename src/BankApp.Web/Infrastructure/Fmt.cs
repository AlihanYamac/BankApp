using BankApp.Core.Enums;
using BankApp.Core.Helpers;

namespace BankApp.Web.Infrastructure;

/// <summary>View'larda kullanılan gösterim yardımcıları.</summary>
public static class Fmt
{
    public static string Money(decimal amount) => $"{amount:N2} ₺";

    public static string SignedMoney(decimal amount) => (amount > 0 ? "+" : "") + Money(amount);

    // Veritabanında UTC saklanır, ekranda yerel saate çevrilir.
    public static string Date(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("dd.MM.yyyy HH:mm");

    public static string Iban(string iban) => IbanHelper.Format(iban);

    public static string TransactionTypeName(TransactionType type) => type switch
    {
        TransactionType.Deposit => "Para Yatırma",
        TransactionType.TransferOut => "Giden Transfer",
        TransactionType.TransferIn => "Gelen Transfer",
        TransactionType.Commission => "Komisyon",
        _ => type.ToString()
    };

    public static string TransferTypeName(TransferType type) => type switch
    {
        TransferType.OwnAccount => "Kendi Hesabıma",
        TransferType.OtherCustomer => "Başka Hesaba",
        _ => type.ToString()
    };
}
