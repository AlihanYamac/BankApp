using BankApp.Core.Enums;
using BankApp.Core.Exceptions;
using BankApp.Services.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BankApp.Tests;

public class TransferServiceTests : IDisposable
{
    // Geçerli (algoritmaya uygun) test T.C. kimlik numaraları
    private const string Tckn1 = "12345678950";
    private const string Tckn2 = "23456789138";

    private readonly TestDb _t = new();

    public void Dispose() => _t.Dispose();

    [Fact]
    public async Task Transfer_ToOwnAccount_HasNoCommission()
    {
        var (customer, main) = await _t.CreateCustomerAsync(Tckn1, "a@test.com", 1000m);
        var savings = await _t.Accounts.OpenAccountAsync(customer.Id, "Birikim");

        var result = await _t.Transfers.TransferAsync(customer.Id, new TransferRequest(main.Id, savings.Iban, 300m, null));

        Assert.Equal(TransferType.OwnAccount, result.Type);
        Assert.Equal(0m, result.Fee);
        Assert.Equal(700m, (await _t.Accounts.GetAccountAsync(customer.Id, main.Id)).Balance);
        Assert.Equal(300m, (await _t.Accounts.GetAccountAsync(customer.Id, savings.Id)).Balance);

        var movements = await _t.Accounts.GetTransactionsAsync(customer.Id, main.Id);
        Assert.DoesNotContain(movements, m => m.Type == TransactionType.Commission);
    }

    [Fact]
    public async Task Transfer_ToOtherCustomer_Charges399Commission()
    {
        var (sender, senderAcc) = await _t.CreateCustomerAsync(Tckn1, "a@test.com", 1000m);
        var (receiver, receiverAcc) = await _t.CreateCustomerAsync(Tckn2, "b@test.com", 0m);

        var result = await _t.Transfers.TransferAsync(sender.Id, new TransferRequest(senderAcc.Id, receiverAcc.Iban, 100m, "Kira"));

        Assert.Equal(TransferType.OtherCustomer, result.Type);
        Assert.Equal(3.99m, result.Fee);
        Assert.Equal(103.99m, result.TotalDebit);
        Assert.Equal(896.01m, (await _t.Accounts.GetAccountAsync(sender.Id, senderAcc.Id)).Balance);
        // Alıcıya komisyon yansımaz, sadece tutar geçer
        Assert.Equal(100m, (await _t.Accounts.GetAccountAsync(receiver.Id, receiverAcc.Id)).Balance);
    }

    [Fact]
    public async Task Transfer_ToOtherCustomer_CommissionAppearsInAccountMovements()
    {
        var (sender, senderAcc) = await _t.CreateCustomerAsync(Tckn1, "a@test.com", 1000m);
        var (receiver, receiverAcc) = await _t.CreateCustomerAsync(Tckn2, "b@test.com", 0m);

        await _t.Transfers.TransferAsync(sender.Id, new TransferRequest(senderAcc.Id, receiverAcc.Iban, 100m, null));

        var senderMovements = await _t.Accounts.GetTransactionsAsync(sender.Id, senderAcc.Id);
        var transferOut = Assert.Single(senderMovements, m => m.Type == TransactionType.TransferOut);
        var commission = Assert.Single(senderMovements, m => m.Type == TransactionType.Commission);

        Assert.Equal(-100m, transferOut.Amount);
        Assert.Equal(900m, transferOut.BalanceAfter);
        Assert.Equal(-3.99m, commission.Amount);
        Assert.Equal(896.01m, commission.BalanceAfter);
        Assert.Equal(transferOut.ReferenceNo, commission.ReferenceNo);

        var receiverMovements = await _t.Accounts.GetTransactionsAsync(receiver.Id, receiverAcc.Id);
        var transferIn = Assert.Single(receiverMovements);
        Assert.Equal(TransactionType.TransferIn, transferIn.Type);
        Assert.Equal(100m, transferIn.Amount);
    }

    [Fact]
    public async Task Transfer_WithInsufficientBalanceForCommission_FailsAndChangesNothing()
    {
        var (sender, senderAcc) = await _t.CreateCustomerAsync(Tckn1, "a@test.com", 100m);
        var (_, receiverAcc) = await _t.CreateCustomerAsync(Tckn2, "b@test.com", 0m);

        // 100 TL var ama 100 + 3,99 TL gerekiyor
        await Assert.ThrowsAsync<InsufficientBalanceException>(() =>
            _t.Transfers.TransferAsync(sender.Id, new TransferRequest(senderAcc.Id, receiverAcc.Iban, 100m, null)));

        _t.Db.ChangeTracker.Clear();
        Assert.Equal(100m, (await _t.Accounts.GetAccountAsync(sender.Id, senderAcc.Id)).Balance);
        Assert.Equal(0, await _t.Db.Transfers.CountAsync());
    }

    [Fact]
    public async Task Transfer_ExactBalanceIncludingCommission_Succeeds()
    {
        var (sender, senderAcc) = await _t.CreateCustomerAsync(Tckn1, "a@test.com", 103.99m);
        var (_, receiverAcc) = await _t.CreateCustomerAsync(Tckn2, "b@test.com", 0m);

        var result = await _t.Transfers.TransferAsync(sender.Id, new TransferRequest(senderAcc.Id, receiverAcc.Iban, 100m, null));

        Assert.Equal(0m, result.NewBalance);
    }

    [Fact]
    public async Task Transfer_FromAnotherCustomersAccount_IsRejected()
    {
        var (_, victimAcc) = await _t.CreateCustomerAsync(Tckn1, "a@test.com", 1000m);
        var (attacker, attackerAcc) = await _t.CreateCustomerAsync(Tckn2, "b@test.com", 0m);

        await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            _t.Transfers.TransferAsync(attacker.Id, new TransferRequest(victimAcc.Id, attackerAcc.Iban, 500m, null)));
    }

    [Fact]
    public async Task Transfer_ToSameAccount_IsRejected()
    {
        var (customer, account) = await _t.CreateCustomerAsync(Tckn1, "a@test.com", 1000m);

        var ex = await Assert.ThrowsAsync<BankException>(() =>
            _t.Transfers.TransferAsync(customer.Id, new TransferRequest(account.Id, account.Iban, 10m, null)));
        Assert.Contains("aynı olamaz", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10.555)]
    [InlineData(100_000.01)]
    public async Task Transfer_WithInvalidAmount_IsRejected(double rawAmount)
    {
        var amount = (decimal)rawAmount; // Attribute'lar decimal sabit kabul etmediği için double'dan çevrilir
        var (sender, senderAcc) = await _t.CreateCustomerAsync(Tckn1, "a@test.com", 1000m);
        var (_, receiverAcc) = await _t.CreateCustomerAsync(Tckn2, "b@test.com", 0m);

        await Assert.ThrowsAsync<BankException>(() =>
            _t.Transfers.TransferAsync(sender.Id, new TransferRequest(senderAcc.Id, receiverAcc.Iban, amount, null)));
    }

    [Fact]
    public async Task Transfer_ToInvalidIban_IsRejected()
    {
        var (customer, account) = await _t.CreateCustomerAsync(Tckn1, "a@test.com", 1000m);

        await Assert.ThrowsAsync<BankException>(() =>
            _t.Transfers.TransferAsync(customer.Id, new TransferRequest(account.Id, "TR000000000000000000000000", 10m, null)));
    }

    [Fact]
    public async Task Transfer_AcceptsIbanWithSpacesAndLowercase()
    {
        var (sender, senderAcc) = await _t.CreateCustomerAsync(Tckn1, "a@test.com", 1000m);
        var (_, receiverAcc) = await _t.CreateCustomerAsync(Tckn2, "b@test.com", 0m);
        var messyIban = Core.Helpers.IbanHelper.Format(receiverAcc.Iban).ToLowerInvariant();

        var result = await _t.Transfers.TransferAsync(sender.Id, new TransferRequest(senderAcc.Id, messyIban, 10m, null));

        Assert.Equal(receiverAcc.Iban, result.ToIban);
    }

    [Fact]
    public async Task Preview_ShowsFeeWithoutMovingMoney()
    {
        var (sender, senderAcc) = await _t.CreateCustomerAsync(Tckn1, "a@test.com", 1000m);
        var (_, receiverAcc) = await _t.CreateCustomerAsync(Tckn2, "b@test.com", 0m);

        var preview = await _t.Transfers.PreviewAsync(sender.Id, new TransferRequest(senderAcc.Id, receiverAcc.Iban, 50m, null));

        Assert.Equal(3.99m, preview.Fee);
        Assert.Equal(53.99m, preview.TotalDebit);
        Assert.Equal(1000m, (await _t.Accounts.GetAccountAsync(sender.Id, senderAcc.Id)).Balance);
    }
}
