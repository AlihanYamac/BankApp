using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using BankApp.Services;
using BankApp.Web.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.WebEncoders;

var builder = WebApplication.CreateBuilder(args);

// Para ve tarih gösterimleri Türkçe formatta olsun (1.234,56 ₺)
var turkish = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = turkish;
CultureInfo.DefaultThreadCurrentUICulture = turkish;

// Türkçe karakterler (ı, ş, ğ, ₺) HTML'de &#x131; gibi kodlanmadan yazılsın
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// Veritabanı + iş servisleri (BankApp.Services/DependencyInjection.cs)
builder.Services.AddBankServices(builder.Configuration);

// Cookie tabanlı oturum (login) yönetimi
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
    });

builder.Services.AddControllersWithViews(options =>
{
    // Varsayılan olarak tüm sayfalar giriş gerektirir; [AllowAnonymous] olanlar hariç.
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(policy));

    // Tüm POST isteklerinde CSRF (sahte istek) koruması
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // "1.500,50" ve "1500.50" formatlarının ikisini de doğru okuyan tutar binder'ı
    options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
});

var app = builder.Build();

// Uygulama açılırken veritabanını oluştur / güncelle ve demo veriyi ekle
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
