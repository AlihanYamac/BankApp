using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace BankApp.Web.Controllers;

/// <summary>Tüm controller'ların ortak özellikleri.</summary>
public abstract class BaseController : Controller
{
    /// <summary>Giriş yapmış müşterinin Id'si (login sırasında cookie'ye yazılan claim'den okunur).</summary>
    protected int CurrentCustomerId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new InvalidOperationException("Kullanıcı oturumu bulunamadı."));

    protected void ShowSuccess(string message) => TempData["Success"] = message;
    protected void ShowError(string message) => TempData["Error"] = message;
}
