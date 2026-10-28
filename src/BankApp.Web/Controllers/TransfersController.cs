using BankApp.Core.Exceptions;
using BankApp.Services.Interfaces;
using BankApp.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BankApp.Web.Controllers;

/// <summary>
/// Para transferi akışı (gerçek bankacılık uygulamalarındaki gibi 3 adım):
///   1) Create (GET/POST)  → Form doldurulur
///   2) Confirm (görüntü)  → Komisyon ve toplam tutar gösterilir, kullanıcı onaylar
///   3) Execute (POST)     → Transfer yapılır, dekont (Receipt) gösterilir
/// </summary>
public class TransfersController : BaseController
{
    private readonly ITransferService _transferService;
    private readonly IAccountService _accountService;

    public TransfersController(ITransferService transferService, IAccountService accountService)
    {
        _transferService = transferService;
        _accountService = accountService;
    }

    public async Task<IActionResult> Index()
    {
        var history = await _transferService.GetHistoryAsync(CurrentCustomerId);
        return View(history);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? fromAccountId)
    {
        var model = new TransferViewModel { FromAccountId = fromAccountId };
        await FillFormAsync(model);
        return View(model);
    }

    /// <summary>Formu doğrular ve onay ekranını gösterir (henüz para çıkmaz).</summary>
    [HttpPost]
    public async Task<IActionResult> Create(TransferViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FillFormAsync(model);
            return View(model);
        }

        try
        {
            var preview = await _transferService.PreviewAsync(CurrentCustomerId, model.ToRequest());
            return View("Confirm", new TransferConfirmViewModel { Form = model, Preview = preview });
        }
        catch (BankException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await FillFormAsync(model);
            return View(model);
        }
    }

    /// <summary>Kullanıcı onayladıktan sonra transferi gerçekleştirir.</summary>
    [HttpPost]
    public async Task<IActionResult> Execute(TransferViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FillFormAsync(model);
            return View(nameof(Create), model);
        }

        try
        {
            var result = await _transferService.TransferAsync(CurrentCustomerId, model.ToRequest());
            return View("Receipt", result);
        }
        catch (BankException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await FillFormAsync(model);
            return View(nameof(Create), model);
        }
    }

    private async Task FillFormAsync(TransferViewModel model)
    {
        model.MyAccounts = await _accountService.GetAccountsAsync(CurrentCustomerId);
        model.OtherCustomerFee = _transferService.CalculateFee(isOwnAccount: false);
    }
}
