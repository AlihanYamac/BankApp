namespace BankApp.Core.Helpers;

public static class MaskHelper
{
    /// <summary>Gizlilik için ismi maskeler: "Ahmet Yılmaz" → "Ah*** Yı****".</summary>
    public static string MaskName(string fullName) =>
        string.Join(' ', fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Length <= 2 ? p : p[..2] + new string('*', p.Length - 2)));
}
