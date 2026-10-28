namespace BankApp.Core.Enums;

public enum TransferType
{
    /// <summary>Müşterinin kendi hesapları arası (komisyonsuz).</summary>
    OwnAccount = 1,

    /// <summary>Başka bir müşterinin hesabına (komisyonlu).</summary>
    OtherCustomer = 2
}
