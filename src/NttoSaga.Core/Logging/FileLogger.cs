using NttoSaga.Core.Data;

namespace NttoSaga.Core.Logging;

/// <summary>Jurnal text zilnic pentru diagnostic la distanță (§7).</summary>
public static class FileLogger
{
    private static readonly object Lock = new();

    public static void Info(string mesaj) => Scrie("INFO", mesaj);
    public static void Eroare(string mesaj) => Scrie("EROARE", mesaj);
    public static void Eroare(string mesaj, Exception ex) => Scrie("EROARE", $"{mesaj} — {ex}");

    private static void Scrie(string nivel, string mesaj)
    {
        try
        {
            AppPaths.EnsureFolders();
            var cale = Path.Combine(AppPaths.LogFolder, $"{DateTime.Now:yyyy-MM-dd}.log");
            var linie = $"{DateTime.Now:HH:mm:ss} [{nivel}] {mesaj}{Environment.NewLine}";
            lock (Lock)
            {
                File.AppendAllText(cale, linie);
            }
        }
        catch
        {
            // Jurnalizarea nu trebuie niciodată să oprească aplicația.
        }
    }
}
