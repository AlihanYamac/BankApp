namespace BankApp.Core.Helpers;

/// <summary>
/// T.C. Kimlik Numarası algoritma kontrolü:
///  - 11 hane, ilk hane 0 olamaz
///  - 10. hane = ((1,3,5,7,9. hanelerin toplamı * 7) - (2,4,6,8. hanelerin toplamı)) mod 10
///  - 11. hane = (ilk 10 hanenin toplamı) mod 10
/// </summary>
public static class NationalIdValidator
{
    public static bool IsValid(string? nationalId)
    {
        if (nationalId is null || nationalId.Length != 11 || !nationalId.All(char.IsDigit) || nationalId[0] == '0')
            return false;

        var d = nationalId.Select(c => c - '0').ToArray();
        var oddSum = d[0] + d[2] + d[4] + d[6] + d[8];
        var evenSum = d[1] + d[3] + d[5] + d[7];

        var digit10 = ((oddSum * 7 - evenSum) % 10 + 10) % 10;
        var digit11 = (d.Take(9).Sum() + digit10) % 10;

        return d[9] == digit10 && d[10] == digit11;
    }
}
