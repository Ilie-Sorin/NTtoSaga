namespace NttoSaga.Core.Exporting;

/// <summary>Exportul nu poate continua — de regulă o gestiune sau o cotă TVA fără configurare (§6.3).</summary>
public class ExportBlockedException : Exception
{
    public ExportBlockedException(string message) : base(message) { }
}
