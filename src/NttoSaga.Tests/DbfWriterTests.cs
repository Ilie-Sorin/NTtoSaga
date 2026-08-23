using NttoSaga.Core.Exporting;
using NttoSaga.Core.Models;

namespace NttoSaga.Tests;

public class DbfWriterTests
{
    private const string FisierExemplu = @"d:\nttosaga\files\IN_03-08-2026_03-08-2026_NT_index.dbf";

    /// <summary>T4 — testul de acceptanță central al §9: comparație octet cu octet cu exemplul real.</summary>
    [Fact]
    public void Genereaza_fisier_identic_octet_cu_octet_cu_exemplul_NT4244_cota21()
    {
        var linie = new LinieImport
        {
            Name = "ANCAFARM1",
            PartnerName = "ANCAFARM2",
            DocNumber = "4244",
            DocDate = "2026-08-03",
            RetailVatPercent = 21,
            ValAmIesire = 145773,
            ValVatAmIesire = 25299,
            ValAchizitieFaraTVAIesire = 54237,
        };

        var gestiuni = new List<NomenclatorGestiune>
        {
            new() { Denumire = "ANCAFARM1", ContMarfa = "371.00001", Activitate = "01", Activ = true },
            new() { Denumire = "ANCAFARM2", ContMarfa = "371.00002", Activitate = "02", Activ = true },
        };
        var denumiriTva = new List<DenumireTva>
        {
            new() { Cota = 21, DenArt = "Parafarmaceutice TVA<>11%", Activ = true },
        };
        var setari = new SetariExport { Cod = "20100", Tip = "A", Um = "BUC" };

        var inregistrari = DbfRecordBuilder.Construieste([linie], gestiuni, denumiriTva, setari);

        var caleTemp = Path.Combine(Path.GetTempPath(), $"nttosaga_test_{Guid.NewGuid():N}.dbf");
        try
        {
            DbfWriter.Scrie(caleTemp, inregistrari, dataCreare: new DateTime(2026, 8, 23));

            var generat = File.ReadAllBytes(caleTemp);
            var exemplu = File.ReadAllBytes(FisierExemplu);

            Assert.Equal(exemplu.Length, generat.Length);
            Assert.Equal(exemplu, generat);
        }
        finally
        {
            if (File.Exists(caleTemp)) File.Delete(caleTemp);
        }
    }

    [Fact]
    public void Depasire_camp_arunca_eroare_explicita_fara_trunchiere()
    {
        var linie = new LinieImport
        {
            Name = "ANCAFARM1",
            PartnerName = "ANCAFARM2",
            DocNumber = "1",
            DocDate = "2026-08-03",
            RetailVatPercent = 21,
            ValAmIesire = 100,
            ValVatAmIesire = 10,
            ValAchizitieFaraTVAIesire = 90,
        };
        var gestiuni = new List<NomenclatorGestiune>
        {
            new() { Denumire = "ANCAFARM1", ContMarfa = "371.00001", Activitate = "01", Activ = true },
            new() { Denumire = "ANCAFARM2", ContMarfa = "371.00002", Activitate = "02", Activ = true },
        };
        var denumiriTva = new List<DenumireTva>
        {
            new() { Cota = 21, DenArt = new string('X', 61), Activ = true }, // 61 > limita de 60
        };
        var setari = new SetariExport { Cod = "20100", Tip = "A", Um = "BUC" };

        var inregistrari = DbfRecordBuilder.Construieste([linie], gestiuni, denumiriTva, setari);
        var caleTemp = Path.Combine(Path.GetTempPath(), $"nttosaga_test_{Guid.NewGuid():N}.dbf");
        try
        {
            Assert.Throws<DbfOverflowException>(() => DbfWriter.Scrie(caleTemp, inregistrari));
        }
        finally
        {
            if (File.Exists(caleTemp)) File.Delete(caleTemp);
        }
    }
}
