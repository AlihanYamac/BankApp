using BankApp.Core.Helpers;

namespace BankApp.Tests;

public class HelperTests
{
    [Fact]
    public void GeneratedIban_IsValidAndHasCorrectFormat()
    {
        for (var i = 0; i < 100; i++)
        {
            var iban = IbanHelper.GenerateRandom();

            Assert.Equal(26, iban.Length);
            Assert.StartsWith("TR", iban);
            Assert.Equal(IbanHelper.BankCode, iban.Substring(4, 5));
            Assert.True(IbanHelper.IsValid(iban));
        }
    }

    [Theory]
    [InlineData("TR330006100519786457841326")] // Gerçek formatta, kontrol hanesi doğru örnek IBAN
    [InlineData("tr33 0006 1005 1978 6457 8413 26")]
    public void IsValid_AcceptsCorrectIbans(string iban)
    {
        Assert.True(IbanHelper.IsValid(iban));
    }

    [Theory]
    [InlineData("TR340006100519786457841326")] // Kontrol hanesi bozuk
    [InlineData("TR33000610051978645784132")]  // Eksik hane
    [InlineData("DE89370400440532013000")]     // Başka ülke
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_RejectsWrongIbans(string? iban)
    {
        Assert.False(IbanHelper.IsValid(iban));
    }

    [Fact]
    public void Format_GroupsByFour()
    {
        Assert.Equal("TR33 0006 1005 1978 6457 8413 26", IbanHelper.Format("TR330006100519786457841326"));
    }

    [Theory]
    [InlineData("10000000146", true)]
    [InlineData("12345678950", true)]
    [InlineData("23456789138", true)]
    [InlineData("12345678901", false)]
    [InlineData("02345678950", false)]
    [InlineData("1234567895", false)]
    [InlineData("abcdefghijk", false)]
    public void NationalIdValidator_Works(string tckn, bool expected)
    {
        Assert.Equal(expected, NationalIdValidator.IsValid(tckn));
    }

    [Fact]
    public void MaskName_HidesMostOfTheName()
    {
        Assert.Equal("Ay** De***", MaskHelper.MaskName("Ayşe Demir"));
    }
}
