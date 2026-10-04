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

        // Formula corectă a TVA (TVA_ART aplicat la ValAchizitieFaraTVAIesire, rotunjit la 2 zecimale):
        // 21% din 542.37 lei = 113.90 lei — NU 252.99 lei (ValVatAmIesire, din vânzarea cu amănuntul,
        // fără legătură cu baza de calcul a transferului între gestiuni).
        Assert.Equal(-113.90m, inregistrari[0].Tva);
        Assert.Equal(113.90m, inregistrari[1].Tva);

        var caleTemp = Path.Combine(Path.GetTempPath(), $"nttosaga_test_{Guid.NewGuid():N}.dbf");
        try
        {
            DbfWriter.Scrie(caleTemp, inregistrari, dataCreare: new DateTime(2026, 8, 23));

            var generat = File.ReadAllBytes(caleTemp);
            var exemplu = File.ReadAllBytes(FisierExemplu);

            // Fișierul-exemplu real conține valoarea veche, greșită, a TVA (252.99 lei). Formatul
            // (antet, descriptori câmp, toate celelalte câmpuri) trebuie să rămână identic octet-cu-octet;
            // doar cele două zone ale câmpului TVA sunt recalculate aici la valoarea corectă, înainte
            // de comparație, ca testul să continue să garanteze compatibilitatea binară cu SAGA.
            var asteptat = (byte[])exemplu.Clone();
            PatchTvaField(asteptat, randIndex: 0, textNou: "-113.90");
            PatchTvaField(asteptat, randIndex: 1, textNou: "113.90");

            Assert.Equal(asteptat.Length, generat.Length);
            Assert.Equal(asteptat, generat);
        }
        finally
        {
            if (File.Exists(caleTemp)) File.Delete(caleTemp);
        }
    }

    private const int LungimeAntetExemplu = 673;
    private const int LungimeInregistrareExemplu = 310;
    private const int OffsetTvaInInregistrare = 244; // 1 (marcaj șters) + suma lungimilor câmpurilor înaintea TVA
    private const int LungimeCampTva = 15;

    private static void PatchTvaField(byte[] bytes, int randIndex, string textNou)
    {
        var start = LungimeAntetExemplu + randIndex * LungimeInregistrareExemplu + OffsetTvaInInregistrare;
        var text = textNou.PadLeft(LungimeCampTva);
        var octetiNoi = System.Text.Encoding.ASCII.GetBytes(text);
        Array.Copy(octetiNoi, 0, bytes, start, LungimeCampTva);
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
