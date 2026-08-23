using NttoSaga.Core.Exporting;

namespace NttoSaga.Tests;

public class BusinessRulesTests
{
    [Fact] // T12 — D8: sortare numerică, nu alfabetică
    public void DocNumberComparer_sorteaza_numeric_nu_alfabetic()
    {
        var lista = new List<string> { "10000", "9999", "1910", "2984" };
        lista.Sort(DocNumberComparer.Instance);
        Assert.Equal(new[] { "1910", "2984", "9999", "10000" }, lista);
    }

    [Theory] // D9 — index cu zerouri de completare pe 4 poziții, natural peste 9999
    [InlineData(7, 4, "0007")]
    [InlineData(42, 4, "0042")]
    [InlineData(1337, 4, "1337")]
    [InlineData(10023, 4, "10023")]
    public void ExportFileNameBuilder_formateaza_indexul_corect(long index, int latime, string asteptat)
    {
        var nume = ExportFileNameBuilder.Construieste(
            new DateTime(2026, 8, 3), new DateTime(2026, 8, 31), index, latime);
        Assert.Equal($"IN_03-08-2026_31-08-2026_NT_{asteptat}.dbf", nume);
    }
}
