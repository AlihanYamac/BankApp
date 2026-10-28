using BankApp.Core.Enums;
using BankApp.Core.Exceptions;
using BankApp.Core.Helpers;
using BankApp.Services.Dtos;

namespace BankApp.Tests;

public class AccountAndCustomerTests : IDisposable
{
    private readonly TestDb _t = new();

    public void Dispose() => _t.Dispose();

    [Fact]
    public async Task Register_OpensDefaultAccountWithValidIban()
    {
        var (customer, account) = await _t.CreateCustomerAsync("12345678950", "a@test.com", 0m);

        Assert.Equal("Vadesiz TL Hesabı", account.Name);
        Assert.True(IbanHelper.IsValid(account.Iban));
        Assert.Single(await _t.Accounts.GetAccountsAsync(customer.Id));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_IsRejected()
    {
        await _t.CreateCustomerAsync("12345678950", "a@test.com", 0m);

        await Assert.ThrowsAsync<BankException>(() =>
            _t.Customers.RegisterAsync(new RegisterRequest("X", "Y", "23456789138", "A@test.com", "Sifre123!")));
    }

    [Fact]
    public async Task Register_WithInvalidNationalId_IsRejected()
    {
        await Assert.ThrowsAsync<BankException>(() =>
            _t.Customers.RegisterAsync(new RegisterRequest("X", "Y", "12345678901", "x@test.com", "Sifre123!")));
    }

    [Fact]
    public async Task Login_ChecksPasswordHash()
    {
        await _t.CreateCustomerAsync("12345678950", "a@test.com", 0m);

        Assert.NotNull(await _t.Customers.ValidateCredentialsAsync("a@test.com", "Sifre123!"));
        Assert.Null(await _t.Customers.ValidateCredentialsAsync("a@test.com", "yanlis"));
    }

    [Fact]
    public async Task OpenAccount_AllIbansAreUnique()
    {
        var (customer, first) = await _t.CreateCustomerAsync("12345678950", "a@test.com", 0m);
        for (var i = 0; i < 4; i++)
            await _t.Accounts.OpenAccountAsync(customer.Id, $"Hesap {i}");

        var ibans = (await _t.Accounts.GetAccountsAsync(customer.Id)).Select(a => a.Iban).ToList();
        Assert.Equal(5, ibans.Distinct().Count());
    }

    [Fact]
    public async Task OpenAccount_RespectsMaxAccountLimit()
    {
        var (customer, _) = await _t.CreateCustomerAsync("12345678950", "a@test.com", 0m);
        for (var i = 0; i < 4; i++)
            await _t.Accounts.OpenAccountAsync(customer.Id, $"Hesap {i}");

        await Assert.ThrowsAsync<BankException>(() => _t.Accounts.OpenAccountAsync(customer.Id, "Fazla"));
    }

    [Fact]
    public async Task Deposit_IncreasesBalanceAndCreatesMovement()
    {
        var (customer, account) = await _t.CreateCustomerAsync("12345678950", "a@test.com", 250.50m);

        Assert.Equal(250.50m, account.Balance);
        var movement = Assert.Single(await _t.Accounts.GetTransactionsAsync(customer.Id, account.Id));
        Assert.Equal(TransactionType.Deposit, movement.Type);
        Assert.Equal(250.50m, movement.BalanceAfter);
    }

    [Fact]
    public async Task GetTransactions_OfAnotherCustomersAccount_IsRejected()
    {
        var (_, victimAcc) = await _t.CreateCustomerAsync("12345678950", "a@test.com", 0m);
        var (attacker, _) = await _t.CreateCustomerAsync("23456789138", "b@test.com", 0m);

        await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            _t.Accounts.GetTransactionsAsync(attacker.Id, victimAcc.Id));
    }
}
