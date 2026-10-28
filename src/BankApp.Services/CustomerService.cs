using BankApp.Core.Entities;
using BankApp.Core.Exceptions;
using BankApp.Core.Helpers;
using BankApp.Data;
using BankApp.Services.Dtos;
using BankApp.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankApp.Services;

public class CustomerService : ICustomerService
{
    private readonly BankDbContext _db;
    private readonly IPasswordHasher<Customer> _passwordHasher;
    private readonly IAccountService _accountService;

    public CustomerService(BankDbContext db, IPasswordHasher<Customer> passwordHasher, IAccountService accountService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _accountService = accountService;
    }

    public async Task<CustomerDto> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var nationalId = request.NationalId.Trim();

        if (!NationalIdValidator.IsValid(nationalId))
            throw new BankException("Geçersiz T.C. kimlik numarası.");

        if (await _db.Customers.AnyAsync(c => c.NationalId == nationalId))
            throw new BankException("Bu T.C. kimlik numarası ile kayıtlı bir müşteri zaten var.");

        if (await _db.Customers.AnyAsync(c => c.Email == email))
            throw new BankException("Bu e-posta adresi zaten kullanılıyor.");

        var customer = new Customer
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            NationalId = nationalId,
            Email = email
        };
        // Şifre PBKDF2 algoritması ile hash'lenir (ASP.NET Core Identity'nin hasher'ı).
        customer.PasswordHash = _passwordHasher.HashPassword(customer, request.Password);

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        // Her yeni müşteriye otomatik olarak bir vadesiz hesap açılır.
        await _accountService.OpenAccountAsync(customer.Id, "Vadesiz TL Hesabı");

        return new CustomerDto(customer.Id, customer.FullName, customer.Email);
    }

    public async Task<CustomerDto?> ValidateCredentialsAsync(string email, string password)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.Email == normalized);
        if (customer is null)
            return null;

        var result = _passwordHasher.VerifyHashedPassword(customer, customer.PasswordHash, password);
        return result == PasswordVerificationResult.Failed
            ? null
            : new CustomerDto(customer.Id, customer.FullName, customer.Email);
    }
}
