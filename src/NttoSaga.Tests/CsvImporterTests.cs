using NttoSaga.Core.Import;
using NttoSaga.Core.Repositories;

namespace NttoSaga.Tests;

public class CsvImporterTests
{
    private const string FisierExemplu =
        @"d:\nttosaga\files\_Contabile_B.C10 Centralizatoare intrari si iesiri nefiscale inclusiv cele generate automat - sample.csv";

    private const string FisierComplet =
        @"d:\nttosaga\files\_Contabile_B.C10 Centralizatoare intrari si iesiri nefiscale inclusiv cele generate automat.csv";

    [Fact] // T1
    public void Import_pe_baza_goala_insereaza_37_linii_fara_erori()
    {
        TestEnv.FolderNou();
        var rezultat = new CsvImporter().Importa(FisierExemplu);

        Assert.Equal(37, rezultat.Inserate);
        Assert.Equal(0, rezultat.IgnorateIdentice);
        Assert.Equal(0, rezultat.Actualizate);
        Assert.Empty(rezultat.Conflicte);
        Assert.Equal(0, rezultat.IgnorateValoareZero);
    }

    [Fact] // T2
    public void Reimport_acelasi_fisier_nu_insereaza_nimic_nou()
    {
        TestEnv.FolderNou();
        var importer = new CsvImporter();
        importer.Importa(FisierExemplu);

        var rezultat = importer.Importa(FisierExemplu);

        Assert.Equal(0, rezultat.Inserate);
        Assert.Equal(37, rezultat.IgnorateIdentice);
        Assert.Equal(0, rezultat.Actualizate);
        Assert.Empty(rezultat.Conflicte);
    }

    [Fact] // D2
    public void Linie_cu_ValAmIesire_zero_este_ignorata_si_raportata_distinct()
    {
        TestEnv.FolderNou();
        var caleTemp = Path.Combine(Path.GetTempPath(), $"nttosaga_test_{Guid.NewGuid():N}.csv");
        File.WriteAllText(caleTemp,
            "Name,PartnerName,DocNumber,DocDate,RetailVatPercent,ValAmIesire,ValVatAmIesire,ValAchizitieFaraTVAIesire\r\n" +
            "ANCAFARM1,ANCAFARM2,999,01.08.2026,21,0,0,0\r\n");
        try
        {
            var rezultat = new CsvImporter().Importa(caleTemp);
            Assert.Equal(0, rezultat.Inserate);
            Assert.Equal(1, rezultat.IgnorateValoareZero);
        }
        finally
        {
            File.Delete(caleTemp);
        }
    }

    [Fact] // D3 — linie neexportată, valori diferite la reimport → se actualizează
    public void Reimport_linie_neexportata_cu_valori_diferite_se_actualizeaza()
    {
        TestEnv.FolderNou();
        var caleTemp = Path.Combine(Path.GetTempPath(), $"nttosaga_test_{Guid.NewGuid():N}.csv");
        var importer = new CsvImporter();

        File.WriteAllText(caleTemp,
            "Name,PartnerName,DocNumber,DocDate,RetailVatPercent,ValAmIesire,ValVatAmIesire,ValAchizitieFaraTVAIesire\r\n" +
            "ANCAFARM1,ANCAFARM2,321,01.08.2026,21,100.00,10.00,90.00\r\n");
        importer.Importa(caleTemp);

        File.WriteAllText(caleTemp,
            "Name,PartnerName,DocNumber,DocDate,RetailVatPercent,ValAmIesire,ValVatAmIesire,ValAchizitieFaraTVAIesire\r\n" +
            "ANCAFARM1,ANCAFARM2,321,01.08.2026,21,150.00,15.00,135.00\r\n");
        var rezultat = importer.Importa(caleTemp);
        File.Delete(caleTemp);

        Assert.Equal(1, rezultat.Actualizate);
        Assert.Equal(0, rezultat.Inserate);
        Assert.Empty(rezultat.Conflicte);

        var linie = new LiniiImportRepository().Cauta(gestiuni: ["ANCAFARM1"]).Single();
        Assert.Equal(15000, linie.ValAmIesire);
    }

    [Fact] // T7
    public void Valideaza_semnaleaza_gestiune_necunoscuta_in_nomenclator()
    {
        TestEnv.FolderNou();
        var gestiuniCunoscute = new NomenclatoareRepository().ListeazaGestiuni();

        var rezultat = new CsvImporter().Valideaza(FisierComplet, gestiuniCunoscute);

        Assert.False(rezultat.Valid);
        Assert.Contains("PRODUCTIE", rezultat.GestiuniNecunoscute);
    }
}
