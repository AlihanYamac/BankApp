using BankApp.Data;
using BankApp.Services.Dtos;
using BankApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BankApp.Services;

/// <summary>
/// Uygulama ilk açıldığında veritabanını oluşturur (migration) ve
/// denemek için demo müşteriler, hesaplar ve örnek işlemler ekler.
/// </summary>
public class DbSeeder
{
    public const string DemoPassword = "Demo123!";

    private readonly BankDbContext _db;
    private readonly ICustomerService _customers;
    private readonly IAccountService _accounts;
    private readonly ITransferService _transfers;

    public DbSeeder(BankDbContext db, ICustomerService customers, IAccountService accounts, ITransferService transfers)
    {
        _db = db;
        _customers = customers;
        _accounts = accounts;
        _transfers = transfers;
    }

    public async Task SeedAsync()
    {
        await _db.Database.MigrateAsync();

        if (await _db.Customers.AnyAsync())
            return;

        var ahmet = await _customers.RegisterAsync(
            new RegisterRequest("Ahmet", "Yılmaz", "12345678950", "ahmet@demo.com", DemoPassword));
        var ayse = await _customers.RegisterAsync(
            new RegisterRequest("Ayşe", "Demir", "23456789138", "ayse@demo.com", DemoPassword));

        var ahmetMain = (await _accounts.GetAccountsAsync(ahmet.Id))[0];
        var ahmetSavings = await _accounts.OpenAccountAsync(ahmet.Id, "Birikim Hesabı");
        var ayseMain = (await _accounts.GetAccountsAsync(ayse.Id))[0];

        await _accounts.DepositAsync(ahmet.Id, ahmetMain.Id, 15_000m);
        await _accounts.DepositAsync(ayse.Id, ayseMain.Id, 8_000m);

        // Örnek hareketler: kendi hesabına (komisyonsuz) ve başkasına (komisyonlu) transfer
        await _transfers.TransferAsync(ahmet.Id,
            new TransferRequest(ahmetMain.Id, ahmetSavings.Iban, 2_500m, "Birikim"));
        await _transfers.TransferAsync(ahmet.Id,
            new TransferRequest(ahmetMain.Id, ayseMain.Iban, 750m, "Kira payı"));
    }
}
