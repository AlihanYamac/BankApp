namespace BankApp.Services.Options;

/// <summary>
/// appsettings.json içindeki "Bank" bölümünden okunan ayarlar.
/// Komisyon tutarı kodda sabit değil, konfigürasyondan gelir.
/// </summary>
public class BankOptions
{
    public const string SectionName = "Bank";

    /// <summary>Başka müşterinin hesabına yapılan transferde alınan komisyon.</summary>
    public decimal OtherCustomerTransferFee { get; set; } = 3.99m;

    /// <summary>Tek seferde gönderilebilecek en yüksek tutar.</summary>
    public decimal MaxTransferAmount { get; set; } = 100_000m;

    /// <summary>Tek seferde yatırılabilecek en yüksek tutar (demo para yatırma).</summary>
    public decimal MaxDepositAmount { get; set; } = 50_000m;

    /// <summary>Bir müşterinin açabileceği en fazla hesap sayısı.</summary>
    public int MaxAccountsPerCustomer { get; set; } = 5;
}
