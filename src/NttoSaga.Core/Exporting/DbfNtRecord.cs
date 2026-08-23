namespace NttoSaga.Core.Exporting;

/// <summary>O înregistrare (linie) din fișierul DBF de note de transfer, conform §4.1.</summary>
public class DbfNtRecord
{
    public required string NrIntrare { get; init; }      // NR_INTRARE
    public required string Cod { get; init; }             // COD
    public required DateTime Data { get; init; }           // DATA / SCADENT
    public required string Tip { get; init; }              // TIP
    public required string DenArt { get; init; }           // DEN_ART
    public required string Um { get; init; }               // UM
    public required decimal Cantitate { get; init; }       // CANTITATE
    public required int TvaArt { get; init; }               // TVA_ART
    public required decimal Valoare { get; init; }          // VALOARE
    public required decimal Tva { get; init; }              // TVA
    public required string Cont { get; init; }             // CONT
    public required decimal PretVanz { get; init; }         // PRET_VANZ
    public required string Grupa { get; init; }             // GRUPA
}
