using BankApp.Core.Exceptions;
using BankApp.Services.Interfaces;
using BankApp.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BankApp.Web.Controllers;

/// <summary>Hesap listesi, hesap detayı (hareketler), yeni hesap açma ve para yatırma.</summary>
public class AccountsController : BaseController
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task<IActionResult> Index()
    {
        var accounts = await _accountService.GetAccountsAsync(CurrentCustomerId);
        return View(accounts);
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var model = new AccountDetailsViewModel
            {
                Account = await _accountService.GetAccountAsync(CurrentCustomerId, id),
                Transactions = await _accountService.GetTransactionsAsync(CurrentCustomerId, id)
            };
            return View(model);
        }
        catch (AccountNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public IActionResult Create() => View(new OpenAccountViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(OpenAccountViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var account = await _accountService.OpenAccountAsync(CurrentCustomerId, model.Name);
            ShowSuccess($"\"{account.Name}\" hesabınız açıldı.");
            return RedirectToAction(nameof(Details), new { id = account.Id });
        }
        catch (BankException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Deposit(int id)
    {
        try
        {
            var account = await _accountService.GetAccountAsync(CurrentCustomerId, id);
            return View(new DepositViewModel { AccountId = account.Id, AccountName = account.Name, Iban = account.Iban });
        }
        catch (AccountNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Deposit(DepositViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _accountService.DepositAsync(CurrentCustomerId, model.AccountId, model.Amount!.Value);
            ShowSuccess($"{model.Amount:N2} ₺ hesabınıza yatırıldı.");
            return RedirectToAction(nameof(Details), new { id = model.AccountId });
        }
        catch (BankException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
