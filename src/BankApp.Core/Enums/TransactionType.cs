namespace BankApp.Core.Enums;

public enum TransactionType
{
    Deposit = 1,       // Para yatırma
    TransferOut = 2,   // Giden transfer
    TransferIn = 3,    // Gelen transfer
    Commission = 4     // Transfer komisyonu
}
