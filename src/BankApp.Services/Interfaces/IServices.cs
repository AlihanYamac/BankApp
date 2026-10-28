using BankApp.Services.Dtos;

namespace BankApp.Services.Interfaces;

// Arayüzler (interface) sayesinde Controller'lar somut sınıflara değil sözleşmelere bağımlı olur.
// Bu "Dependency Inversion" prensibidir ve test yazmayı kolaylaştırır.

public interface ICustomerService
{
    Task<CustomerDto> RegisterAsync(RegisterRequest request);
    Task<CustomerDto?> ValidateCredentialsAsync(string email, string password);
}

public interface IAccountService
{
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(int customerId);
    Task<AccountDto> GetAccountAsync(int customerId, int accountId);
    Task<AccountDto> OpenAccountAsync(int customerId, string name);
    Task<AccountDto> DepositAsync(int customerId, int accountId, decimal amount);
    Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(int customerId, int accountId);
}

public interface ITransferService
{
    decimal CalculateFee(bool isOwnAccount);
    Task<TransferPreview> PreviewAsync(int customerId, TransferRequest request);
    Task<TransferResult> TransferAsync(int customerId, TransferRequest request);
    Task<IReadOnlyList<TransferHistoryDto>> GetHistoryAsync(int customerId);
}
