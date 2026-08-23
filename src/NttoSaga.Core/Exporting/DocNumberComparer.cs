namespace NttoSaga.Core.Exporting;

/// <summary>
/// Sortare DocNumber: numeric dacă e integral cifre, altfel alfabetic (D8).
/// Compară mai întâi lungimea (pentru șiruri numerice fără zerouri de completare,
/// lungimea mai mare înseamnă valoare mai mare), apoi ordinea lexicografică.
/// </summary>
public class DocNumberComparer : IComparer<string>
{
    public static readonly DocNumberComparer Instance = new();

    public int Compare(string? a, string? b)
    {
        a ??= ""; b ??= "";
        bool aNumeric = a.Length > 0 && a.All(char.IsDigit);
        bool bNumeric = b.Length > 0 && b.All(char.IsDigit);

        if (aNumeric && bNumeric)
        {
            if (a.Length != b.Length) return a.Length.CompareTo(b.Length);
            return string.CompareOrdinal(a, b);
        }

        return string.CompareOrdinal(a, b);
    }
}
