using System.Numerics;
using System.Text;

namespace BankApp.Core.Helpers;

/// <summary>
/// Türkiye IBAN formatı (26 karakter):
///   TR + 2 kontrol hanesi + 5 banka kodu + 1 rezerv (0) + 16 hesap numarası
/// Kontrol haneleri ISO 13616 / MOD-97 algoritması ile hesaplanır,
/// böylece üretilen IBAN'lar gerçek IBAN doğrulamasından geçer.
/// </summary>
public static class IbanHelper
{
    public const string CountryCode = "TR";
    public const int IbanLength = 26;

    /// <summary>Uygulamanın kullandığı hayali banka kodu (5 hane).</summary>
    public const string BankCode = "00099";

    /// <summary>16 haneli hesap numarasından geçerli bir IBAN üretir.</summary>
    public static string Generate(string accountNumber)
    {
        if (accountNumber.Length != 16 || !accountNumber.All(char.IsDigit))
            throw new ArgumentException("Hesap numarası 16 haneli olmalıdır.", nameof(accountNumber));

        var bban = BankCode + "0" + accountNumber;           // 22 hane
        var checkDigits = 98 - Mod97(bban + CountryCode + "00");
        return $"{CountryCode}{checkDigits:D2}{bban}";
    }

    /// <summary>Rastgele 16 haneli hesap numarası ile IBAN üretir.</summary>
    public static string GenerateRandom()
    {
        var sb = new StringBuilder(16);
        for (var i = 0; i < 16; i++)
            sb.Append(Random.Shared.Next(0, 10));
        return Generate(sb.ToString());
    }

    /// <summary>Boşlukları siler ve büyük harfe çevirir ("tr12 0009..." → "TR120009...").</summary>
    public static string Normalize(string? iban) =>
        (iban ?? string.Empty).Replace(" ", string.Empty).Trim().ToUpperInvariant();

    /// <summary>IBAN'ın format ve MOD-97 kontrolünü yapar.</summary>
    public static bool IsValid(string? iban)
    {
        var value = Normalize(iban);
        if (value.Length != IbanLength || !value.StartsWith(CountryCode))
            return false;
        if (!value[2..].All(char.IsDigit))
            return false;

        // Algoritma: ilk 4 karakter sona taşınır, sonuç mod 97 == 1 olmalı.
        var rearranged = value[4..] + value[..4];
        return Mod97(rearranged) == 1;
    }

    /// <summary>Ekranda okunaklı gösterim: "TR12 0009 9000 ...".</summary>
    public static string Format(string iban)
    {
        var value = Normalize(iban);
        var sb = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            if (i > 0 && i % 4 == 0) sb.Append(' ');
            sb.Append(value[i]);
        }
        return sb.ToString();
    }

    // Harfler sayıya çevrilir (A=10, B=11 ... Z=35) ve büyük sayı mod 97 alınır.
    private static int Mod97(string input)
    {
        var numeric = new StringBuilder();
        foreach (var c in input)
            numeric.Append(char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString());
        return (int)(BigInteger.Parse(numeric.ToString()) % 97);
    }
}
