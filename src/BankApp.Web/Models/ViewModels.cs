using System.ComponentModel.DataAnnotations;
using BankApp.Services.Dtos;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BankApp.Web.Models;

// ViewModel: Bir sayfanın (View) ihtiyaç duyduğu veriyi ve form doğrulama kurallarını taşır.
// [Required], [StringLength] gibi attribute'lar hem sunucu hem tarayıcı tarafında doğrulama yapar.

public class LoginViewModel
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(50)]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(50)]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "T.C. kimlik numarası zorunludur.")]
    [RegularExpression(@"^[1-9]\d{10}$", ErrorMessage = "T.C. kimlik numarası 11 haneli olmalıdır.")]
    [Display(Name = "T.C. Kimlik No")]
    public string NationalId { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    [StringLength(100)]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
    [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre (Tekrar)")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class OpenAccountViewModel
{
    [Required(ErrorMessage = "Hesap adı zorunludur.")]
    [StringLength(50, ErrorMessage = "Hesap adı en fazla 50 karakter olabilir.")]
    [Display(Name = "Hesap Adı")]
    public string Name { get; set; } = string.Empty;
}

public class DepositViewModel
{
    public int AccountId { get; set; }
    public string? AccountName { get; set; }
    public string? Iban { get; set; }

    [Required(ErrorMessage = "Tutar zorunludur.")]
    [Range(0.01, 50000, ErrorMessage = "Tutar 0,01 ile 50.000 TL arasında olmalıdır.")]
    [Display(Name = "Tutar (₺)")]
    public decimal? Amount { get; set; }
}

public class AccountDetailsViewModel
{
    public required AccountDto Account { get; init; }
    public required IReadOnlyList<TransactionDto> Transactions { get; init; }
}

public class TransferViewModel
{
    [Required(ErrorMessage = "Gönderen hesap seçiniz.")]
    [Display(Name = "Gönderen Hesap")]
    public int? FromAccountId { get; set; }

    [Required(ErrorMessage = "Alıcı IBAN zorunludur.")]
    [Display(Name = "Alıcı IBAN")]
    public string ToIban { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tutar zorunludur.")]
    [Range(0.01, 100000, ErrorMessage = "Tutar 0,01 ile 100.000 TL arasında olmalıdır.")]
    [Display(Name = "Tutar (₺)")]
    public decimal? Amount { get; set; }

    [StringLength(200)]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    // Form için doldurulan alanlar (kullanıcıdan gelmez)
    public IReadOnlyList<AccountDto> MyAccounts { get; set; } = [];
    public decimal OtherCustomerFee { get; set; }

    public IEnumerable<SelectListItem> AccountOptions => MyAccounts.Select(a =>
        new SelectListItem($"{a.Name} - {a.Balance:N2} ₺", a.Id.ToString()));

    public TransferRequest ToRequest() =>
        new(FromAccountId!.Value, ToIban, Amount!.Value, Description);
}

public class TransferConfirmViewModel
{
    public required TransferViewModel Form { get; init; }
    public required TransferPreview Preview { get; init; }
}
