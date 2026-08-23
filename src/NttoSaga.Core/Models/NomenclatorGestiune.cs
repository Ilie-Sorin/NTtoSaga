namespace NttoSaga.Core.Models;

public class NomenclatorGestiune
{
    public long Id { get; set; }
    public string Denumire { get; set; } = "";
    public string ContMarfa { get; set; } = "";
    public string Activitate { get; set; } = "";
    public bool Activ { get; set; } = true;
}
