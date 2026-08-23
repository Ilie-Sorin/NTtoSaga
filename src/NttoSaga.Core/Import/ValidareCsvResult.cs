namespace NttoSaga.Core.Import;

public class ValidareCsvResult
{
    public List<string> ColoaneLipsa { get; } = [];
    public List<(int RandCsv, string Motiv)> RanduriInvalide { get; } = [];
    public List<string> GestiuniNecunoscute { get; } = [];
    public List<CsvRawRow> RanduriValide { get; } = [];

    public bool Valid => ColoaneLipsa.Count == 0 && RanduriInvalide.Count == 0 && GestiuniNecunoscute.Count == 0;

    public List<CsvRawRow> Previzualizare(int nr = 20) => RanduriValide.Take(nr).ToList();
}
