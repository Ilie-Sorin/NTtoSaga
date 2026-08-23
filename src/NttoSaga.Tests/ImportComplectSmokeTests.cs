using NttoSaga.Core.Import;

namespace NttoSaga.Tests;

/// <summary>Test de fum pe raportul CSV complet real (933 linii), nu doar eșantionul.</summary>
public class ImportCompletSmokeTests
{
    private const string FisierComplet =
        @"d:\nttosaga\files\_Contabile_B.C10 Centralizatoare intrari si iesiri nefiscale inclusiv cele generate automat.csv";

    [Fact]
    public void Importul_fisierului_complet_reuseste_fara_exceptii()
    {
        TestEnv.FolderNou();
        var rezultat = new CsvImporter().Importa(FisierComplet);

        Assert.Equal(933, rezultat.TotalRanduriProcesate);
        Assert.Equal(933, rezultat.Inserate);
        Assert.Empty(rezultat.Conflicte);
    }

    [Fact]
    public void Reimportul_fisierului_complet_nu_modifica_nimic()
    {
        TestEnv.FolderNou();
        var importer = new CsvImporter();
        importer.Importa(FisierComplet);

        var rezultat = importer.Importa(FisierComplet);

        Assert.Equal(0, rezultat.Inserate);
        Assert.Equal(933, rezultat.IgnorateIdentice);
        Assert.Empty(rezultat.Conflicte);
    }
}
