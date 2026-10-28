using BankApp.Core.Entities;
using BankApp.Data;
using BankApp.Services;
using BankApp.Services.Dtos;
using BankApp.Services.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Options = Microsoft.Extensions.Options.Options;

namespace BankApp.Tests;

/// <summary>
/// Her test için bellekte (in-memory) çalışan gerçek bir SQLite veritabanı oluşturur.
/// Böylece testler birbirini etkilemez ve dosya oluşturmaz.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public BankDbContext Db { get; }
    public AccountService Accounts { get; }
    public CustomerService Customers { get; }
    public TransferService Transfers { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BankDbContext>().UseSqlite(_connection).Options;
        Db = new BankDbContext(options);
        Db.Database.EnsureCreated();

        var bankOptions = Options.Create(new BankOptions());
        Accounts = new AccountService(Db, bankOptions);
        Customers = new CustomerService(Db, new PasswordHasher<Customer>(), Accounts);
        Transfers = new TransferService(Db, bankOptions, NullLogger<TransferService>.Instance);
    }

    /// <summary>Bir müşteri oluşturur ve ilk hesabına verilen bakiyeyi yatırır.</summary>
    public async Task<(CustomerDto Customer, AccountDto Account)> CreateCustomerAsync(
        string nationalId, string email, decimal initialBalance)
    {
        var customer = await Customers.RegisterAsync(new RegisterRequest("Test", "Kullanıcı", nationalId, email, "Sifre123!"));
        var account = (await Accounts.GetAccountsAsync(customer.Id))[0];
        if (initialBalance > 0)
            account = await Accounts.DepositAsync(customer.Id, account.Id, initialBalance);
        return (customer, account);
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
