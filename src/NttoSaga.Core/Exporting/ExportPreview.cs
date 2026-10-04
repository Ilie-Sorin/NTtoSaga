using NttoSaga.Core.Models;

namespace NttoSaga.Core.Exporting;

public class ExportPreview
{
    public required List<LinieImport> Linii { get; init; }
    public int NumarInregistrari => Linii.Count;
    public int NumarLiniiDbf => Linii.Count * 2;
    public long TotalValoare => Linii.Sum(l => l.ValAmIesire);
    public long TotalTva => Linii.Sum(l => l.TvaCalculata);
    public int NumarLiniiDejaExportate => Linii.Count(l => l.EsteExportata);
}

public class ExportGenerareRezultat
{
    public required Models.Export Export { get; init; }
}
