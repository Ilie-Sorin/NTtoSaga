using NttoSaga.Core.Models;

namespace NttoSaga.Core.Import;

public class ConflictImport
{
    public required LinieImport Existent { get; init; }
    public required CsvRawRow DinCsv { get; init; }
}

public class ImportResult
{
    public int Inserate { get; set; }
    public int IgnorateIdentice { get; set; }
    public int Actualizate { get; set; }
    public List<ConflictImport> Conflicte { get; } = [];
    public int IgnorateValoareZero { get; set; }
    public int TotalRanduriProcesate { get; set; }
}
