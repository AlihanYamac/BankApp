using BankApp.Core.Entities;
using BankApp.Data;
using BankApp.Services.Interfaces;
using BankApp.Services.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankApp.Services;

public static class DependencyInjection
{
    /// <summary>
    /// Veritabanı ve iş servislerini DI (Dependency Injection) konteynerine kaydeder.
    /// Program.cs içinde tek satırla çağrılır: builder.Services.AddBankServices(config)
    /// </summary>
    public static IServiceCollection AddBankServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BankDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("BankDb")));

        services.Configure<BankOptions>(configuration.GetSection(BankOptions.SectionName));

        // Scoped: Her HTTP isteği için bir örnek oluşturulur (DbContext ile aynı ömür).
        services.AddScoped<IPasswordHasher<Customer>, PasswordHasher<Customer>>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<DbSeeder>();

        return services;
    }
}
