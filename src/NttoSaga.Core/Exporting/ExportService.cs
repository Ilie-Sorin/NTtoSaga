using NttoSaga.Core.Data;
using NttoSaga.Core.Models;
using NttoSaga.Core.Repositories;

namespace NttoSaga.Core.Exporting;

/// <summary>Orchestrează fluxul de export din §6.3/§6.4: previzualizare, generare atomică, regenerare, anulare.</summary>
public class ExportService
{
    private readonly LiniiImportRepository _liniiRepo = new();
    private readonly ExporturiRepository _exporturiRepo = new();
    private readonly NomenclatoareRepository _nomenclatoareRepo = new();

    public ExportPreview Previzualizeaza(IReadOnlyCollection<string> gestiuni, DateTime dataStart, DateTime dataSfarsit, bool doarNeexportate)
    {
        var linii = _liniiRepo.Cauta(
            gestiuni: gestiuni,
            dataStart: dataStart.ToString("yyyy-MM-dd"),
            dataSfarsit: dataSfarsit.ToString("yyyy-MM-dd"),
            doarNeexportate: doarNeexportate ? true : null);
        return new ExportPreview { Linii = linii };
    }

    /// <summary>Numele exact al fișierului care va fi creat, afișat operatorului înainte de generare (§6.3).</summary>
    public string PrevizualizeazaNumeFisier(DateTime dataStart, DateTime dataSfarsit)
    {
        CitesteSetari(out var latimeIndex);
        var indexUrmator = _exporturiRepo.PrevizualizeazaUrmatorulIndex();
        return ExportFileNameBuilder.Construieste(dataStart, dataSfarsit, indexUrmator, latimeIndex);
    }

    private SetariExport CitesteSetari(out int latimeIndex)
    {
        var setariDict = _nomenclatoareRepo.ListeazaSetari();
        latimeIndex = int.TryParse(setariDict.GetValueOrDefault("latime_index", "4"), out var l) ? l : 4;
        return new SetariExport
        {
            Cod = setariDict.GetValueOrDefault("COD", "20100"),
            Tip = setariDict.GetValueOrDefault("TIP", "A"),
            Um = setariDict.GetValueOrDefault("UM", "BUC"),
        };
    }

    /// <summary>Generează fișierul DBF și marchează liniile ca exportate, atomic (§6.3).</summary>
    public ExportGenerareRezultat Genereaza(
        List<LinieImport> linii, DateTime dataStart, DateTime dataSfarsit,
        IEnumerable<string> filtruGestiuni, string? observatii = null)
    {
        if (linii.Count == 0)
            throw new ExportBlockedException("Nu există nicio linie de exportat pentru selecția curentă.");

        var gestiuni = _nomenclatoareRepo.ListeazaGestiuni();
        var denumiriTva = _nomenclatoareRepo.ListeazaDenumiriTva();
        var setari = CitesteSetari(out var latimeIndex);

        // Validare + construire înregistrări ÎNAINTE de a atinge baza/fișierele — dacă o gestiune
        // sau o cotă TVA nu e configurată, nimic nu a fost încă rezervat sau scris.
        var inregistrari = DbfRecordBuilder.Construieste(linii, gestiuni, denumiriTva, setari);

        AppPaths.EnsureFolders();
        using var conn = Database.OpenConnection();
        using var tx = conn.BeginTransaction();

        long index = _exporturiRepo.Rezerva(conn, tx);
        var numeFisier = ExportFileNameBuilder.Construieste(dataStart, dataSfarsit, index, latimeIndex);
        var caleFinala = Path.Combine(AppPaths.FilesFolder, numeFisier);
        var caleTemp = caleFinala + ".tmp";

        try
        {
            DbfWriter.Scrie(caleTemp, inregistrari);
        }
        catch
        {
            tx.Rollback();
            if (File.Exists(caleTemp))
                File.Delete(caleTemp);
            throw;
        }

        if (File.Exists(caleFinala))
            File.Delete(caleFinala);
        File.Move(caleTemp, caleFinala);

        var export = new Models.Export
        {
            Id = index,
            DataOra = DateTime.Now.ToString("o"),
            NumeFisier = numeFisier,
            CaleFisier = caleFinala,
            FiltruGestiuni = string.Join(";", filtruGestiuni),
            DataStart = dataStart.ToString("yyyy-MM-dd"),
            DataSfarsit = dataSfarsit.ToString("yyyy-MM-dd"),
            NrInregistrari = linii.Count,
            NrLiniiDbf = inregistrari.Count,
            TotalValoare = linii.Sum(l => l.ValAmIesire),
            TotalTva = linii.Sum(l => l.ValVatAmIesire),
            Observatii = observatii ?? "",
        };
        _exporturiRepo.Finalizeaza(conn, tx, export);
        _liniiRepo.MarcheazaExportate(conn, tx, linii.Select(l => l.Id), index);

        tx.Commit();
        return new ExportGenerareRezultat { Export = export };
    }

    /// <summary>Rescrie fișierul unui export existent, cu același index, din datele curente din bază.</summary>
    public void Regenereaza(long idExport)
    {
        var export = _exporturiRepo.Gaseste(idExport)
            ?? throw new ExportBlockedException($"Exportul #{idExport} nu a fost găsit în baza de date.");
        var linii = _liniiRepo.DupaExport(idExport);
        var gestiuni = _nomenclatoareRepo.ListeazaGestiuni();
        var denumiriTva = _nomenclatoareRepo.ListeazaDenumiriTva();
        var setari = CitesteSetari(out _);

        var inregistrari = DbfRecordBuilder.Construieste(linii, gestiuni, denumiriTva, setari);

        AppPaths.EnsureFolders();
        var caleTemp = export.CaleFisier + ".tmp";
        DbfWriter.Scrie(caleTemp, inregistrari);
        if (File.Exists(export.CaleFisier))
            File.Delete(export.CaleFisier);
        File.Move(caleTemp, export.CaleFisier);
    }

    /// <summary>Anulează exportul: eliberează liniile (id_export = NULL) pentru un export ulterior (§6.4).</summary>
    public void Anuleaza(long idExport)
    {
        using var conn = Database.OpenConnection();
        using var tx = conn.BeginTransaction();
        _exporturiRepo.Anuleaza(conn, tx, idExport);
        _liniiRepo.ElibereazaExport(conn, tx, idExport);
        tx.Commit();
    }
}
