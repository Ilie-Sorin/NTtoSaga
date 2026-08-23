using NttoSaga.Core.Exporting;
using NttoSaga.Core.Import;
using NttoSaga.Core.Repositories;

namespace NttoSaga.Tests;

public class ExportServiceTests
{
    private const string FisierExemplu =
        @"d:\nttosaga\files\_Contabile_B.C10 Centralizatoare intrari si iesiri nefiscale inclusiv cele generate automat - sample.csv";

    [Fact] // T3 + T10
    public void Export_ANCAFARM1_03_08_produce_14_inregistrari_cu_balanta_zero()
    {
        TestEnv.FolderNou();
        new CsvImporter().Importa(FisierExemplu);

        var liniiRepo = new LiniiImportRepository();
        var linii = liniiRepo.Cauta(gestiuni: ["ANCAFARM1"], dataStart: "2026-08-03", dataSfarsit: "2026-08-03");
        Assert.Equal(7, linii.Count);

        var service = new ExportService();
        var rezultat = service.Genereaza(linii, new DateTime(2026, 8, 3), new DateTime(2026, 8, 3), ["ANCAFARM1"]);

        Assert.Equal(7, rezultat.Export.NrInregistrari);
        Assert.Equal(14, rezultat.Export.NrLiniiDbf);
        Assert.True(File.Exists(rezultat.Export.CaleFisier));

        var gestiuni = new NomenclatoareRepository().ListeazaGestiuni();
        var denumiriTva = new NomenclatoareRepository().ListeazaDenumiriTva();
        var setari = new SetariExport { Cod = "20100", Tip = "A", Um = "BUC" };
        var inregistrari = DbfRecordBuilder.Construieste(linii, gestiuni, denumiriTva, setari);
        Assert.Equal(0m, inregistrari.Sum(r => r.Valoare));
        Assert.Equal(0m, inregistrari.Sum(r => r.Tva));

        File.Delete(rezultat.Export.CaleFisier);
    }

    [Fact] // T5
    public void Al_doilea_export_doar_neexportate_nu_gaseste_nimic()
    {
        TestEnv.FolderNou();
        new CsvImporter().Importa(FisierExemplu);
        var liniiRepo = new LiniiImportRepository();
        var service = new ExportService();

        var linii1 = liniiRepo.Cauta(gestiuni: ["ANCAFARM1"], dataStart: "2026-08-03", dataSfarsit: "2026-08-03", doarNeexportate: true);
        var rezultat1 = service.Genereaza(linii1, new DateTime(2026, 8, 3), new DateTime(2026, 8, 3), ["ANCAFARM1"]);
        File.Delete(rezultat1.Export.CaleFisier);

        var linii2 = liniiRepo.Cauta(gestiuni: ["ANCAFARM1"], dataStart: "2026-08-03", dataSfarsit: "2026-08-03", doarNeexportate: true);
        Assert.Empty(linii2);
    }

    [Fact] // T6
    public void Anulare_export_apoi_reexport_elibereaza_liniile_si_da_index_nou()
    {
        TestEnv.FolderNou();
        new CsvImporter().Importa(FisierExemplu);
        var liniiRepo = new LiniiImportRepository();
        var service = new ExportService();

        var linii1 = liniiRepo.Cauta(gestiuni: ["ANCAFARM1"], dataStart: "2026-08-03", dataSfarsit: "2026-08-03");
        var rezultat1 = service.Genereaza(linii1, new DateTime(2026, 8, 3), new DateTime(2026, 8, 3), ["ANCAFARM1"]);
        var index1 = rezultat1.Export.Id;
        File.Delete(rezultat1.Export.CaleFisier);

        service.Anuleaza(index1);

        var liniiEliberate = liniiRepo.Cauta(gestiuni: ["ANCAFARM1"], dataStart: "2026-08-03", dataSfarsit: "2026-08-03", doarNeexportate: true);
        Assert.Equal(7, liniiEliberate.Count);

        var rezultat2 = service.Genereaza(liniiEliberate, new DateTime(2026, 8, 3), new DateTime(2026, 8, 3), ["ANCAFARM1"]);
        Assert.NotEqual(index1, rezultat2.Export.Id);
        File.Delete(rezultat2.Export.CaleFisier);
    }

    [Fact] // T11
    public void Export_multiplu_ANCAFARM1_ANCAFARM2_LABORATOR_produce_74_inregistrari_sortate()
    {
        TestEnv.FolderNou();
        new CsvImporter().Importa(FisierExemplu);

        var liniiRepo = new LiniiImportRepository();
        var linii = liniiRepo.Cauta(gestiuni: ["ANCAFARM1", "ANCAFARM2", "LABORATOR"], dataStart: "2026-08-03", dataSfarsit: "2026-08-21");
        Assert.Equal(37, linii.Count);

        var service = new ExportService();
        var rezultat = service.Genereaza(linii, new DateTime(2026, 8, 3), new DateTime(2026, 8, 21), ["ANCAFARM1", "ANCAFARM2", "LABORATOR"]);
        Assert.Equal(74, rezultat.Export.NrLiniiDbf);

        var gestiuni = new NomenclatoareRepository().ListeazaGestiuni();
        var denumiriTva = new NomenclatoareRepository().ListeazaDenumiriTva();
        var setari = new SetariExport { Cod = "20100", Tip = "A", Um = "BUC" };
        var inregistrari = DbfRecordBuilder.Construieste(linii, gestiuni, denumiriTva, setari);

        // D8: DocDate crescător, apoi DocNumber, apoi RetailVatPercent — perechea (negativ, pozitiv) rămâne împreună.
        for (int i = 0; i < inregistrari.Count; i += 2)
        {
            Assert.Equal(-1.000m, inregistrari[i].Cantitate);
            Assert.Equal(1.000m, inregistrari[i + 1].Cantitate);
            Assert.Equal(inregistrari[i].NrIntrare, inregistrari[i + 1].NrIntrare);
        }
        var dateOrdonate = inregistrari.Where((_, i) => i % 2 == 0).Select(r => r.Data).ToList();
        Assert.Equal(dateOrdonate.OrderBy(d => d), dateOrdonate);

        File.Delete(rezultat.Export.CaleFisier);
    }

    [Fact] // T8
    public void Export_cu_cota_zero_foloseste_Scutite_de_TVA_si_cont_fara_zerouri()
    {
        TestEnv.FolderNou();
        var caleTemp = Path.Combine(Path.GetTempPath(), $"nttosaga_test_{Guid.NewGuid():N}.csv");
        File.WriteAllText(caleTemp,
            "Name,PartnerName,DocNumber,DocDate,RetailVatPercent,ValAmIesire,ValVatAmIesire,ValAchizitieFaraTVAIesire\r\n" +
            "ANCAFARM1,ANCAFARM2,777,01.08.2026,0,50.00,0,50.00\r\n");
        try
        {
            new CsvImporter().Importa(caleTemp);
        }
        finally
        {
            File.Delete(caleTemp);
        }

        var liniiRepo = new LiniiImportRepository();
        var linii = liniiRepo.Cauta(gestiuni: ["ANCAFARM1"]);
        var gestiuni = new NomenclatoareRepository().ListeazaGestiuni();
        var denumiriTva = new NomenclatoareRepository().ListeazaDenumiriTva();
        var setari = new SetariExport { Cod = "20100", Tip = "A", Um = "BUC" };
        var inregistrari = DbfRecordBuilder.Construieste(linii, gestiuni, denumiriTva, setari);

        Assert.All(inregistrari, r => Assert.Equal("Scutite de TVA", r.DenArt));
        Assert.Equal("371.00001.0", inregistrari[0].Cont);
        Assert.Equal("371.00002.0", inregistrari[1].Cont);
    }

    [Fact] // T13
    public void Export_cu_PartnerName_necunoscut_este_blocat()
    {
        TestEnv.FolderNou();
        var nomenclatoare = new NomenclatoareRepository();
        nomenclatoare.Adauga("SURSA_X", "371.00099", "99");

        var liniiRepo = new LiniiImportRepository();
        var caleTemp = Path.Combine(Path.GetTempPath(), $"nttosaga_test_{Guid.NewGuid():N}.csv");
        File.WriteAllText(caleTemp,
            "Name,PartnerName,DocNumber,DocDate,RetailVatPercent,ValAmIesire,ValVatAmIesire,ValAchizitieFaraTVAIesire\r\n" +
            "SURSA_X,DESTINATIE_NECUNOSCUTA,1,01.08.2026,21,100,10,90\r\n");
        try
        {
            new CsvImporter().Importa(caleTemp);
        }
        finally
        {
            File.Delete(caleTemp);
        }

        var linii = liniiRepo.Cauta(gestiuni: ["SURSA_X"]);
        var service = new ExportService();

        Assert.Throws<ExportBlockedException>(() =>
            service.Genereaza(linii, new DateTime(2026, 8, 1), new DateTime(2026, 8, 1), ["SURSA_X"]));
    }

    [Fact] // D3 — conflict: linie deja exportată, reimportată cu valori diferite
    public void Reimport_dupa_export_cu_valori_diferite_produce_conflict_fara_modificare()
    {
        TestEnv.FolderNou();
        var caleTemp = Path.Combine(Path.GetTempPath(), $"nttosaga_test_{Guid.NewGuid():N}.csv");
        File.WriteAllText(caleTemp,
            "Name,PartnerName,DocNumber,DocDate,RetailVatPercent,ValAmIesire,ValVatAmIesire,ValAchizitieFaraTVAIesire\r\n" +
            "ANCAFARM1,ANCAFARM2,555,01.08.2026,21,100.00,10.00,90.00\r\n");

        var importer = new CsvImporter();
        importer.Importa(caleTemp);

        var liniiRepo = new LiniiImportRepository();
        var linii = liniiRepo.Cauta(gestiuni: ["ANCAFARM1"]);
        var service = new ExportService();
        var rezultat = service.Genereaza(linii, new DateTime(2026, 8, 1), new DateTime(2026, 8, 1), ["ANCAFARM1"]);
        File.Delete(rezultat.Export.CaleFisier);

        File.WriteAllText(caleTemp,
            "Name,PartnerName,DocNumber,DocDate,RetailVatPercent,ValAmIesire,ValVatAmIesire,ValAchizitieFaraTVAIesire\r\n" +
            "ANCAFARM1,ANCAFARM2,555,01.08.2026,21,200.00,20.00,180.00\r\n");
        var rezultatReimport = importer.Importa(caleTemp);
        File.Delete(caleTemp);

        Assert.Single(rezultatReimport.Conflicte);
        Assert.Equal(10000, rezultatReimport.Conflicte[0].Existent.ValAmIesire); // 100.00 -> neschimbat
        Assert.Equal(20000, rezultatReimport.Conflicte[0].DinCsv.ValAmIesireBani); // 200.00 -> valoarea din CSV
    }
}
