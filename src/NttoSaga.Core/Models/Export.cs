namespace NttoSaga.Core.Models;

public enum StareExport
{
    Activ,
    Anulat
}

public class Export
{
    public long Id { get; set; }
    public string DataOra { get; set; } = "";
    public string NumeFisier { get; set; } = "";
    public string CaleFisier { get; set; } = "";
    /// <summary>Gestiunile selectate, separate prin ';' (D11).</summary>
    public string FiltruGestiuni { get; set; } = "";
    public string DataStart { get; set; } = "";
    public string DataSfarsit { get; set; } = "";
    public int NrInregistrari { get; set; }
    public int NrLiniiDbf { get; set; }
    public long TotalValoare { get; set; }
    public long TotalTva { get; set; }
    public StareExport Stare { get; set; } = StareExport.Activ;
    public string Observatii { get; set; } = "";
}
