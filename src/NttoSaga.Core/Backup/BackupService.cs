using NttoSaga.Core.Data;

namespace NttoSaga.Core.Backup;

/// <summary>Copie de siguranță a bazei de date, automată înainte de import/export și manuală din Setări (§6.5, §7).</summary>
public class BackupService
{
    private const int PastreazaUltimele = 30;

    public string Copiaza()
    {
        AppPaths.EnsureFolders();
        var nume = $"nttosaga_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var cale = Path.Combine(AppPaths.BackupFolder, nume);

        if (File.Exists(AppPaths.DatabaseFile))
            File.Copy(AppPaths.DatabaseFile, cale, overwrite: true);

        Curata();
        return cale;
    }

    private static void Curata()
    {
        var fisiere = Directory.GetFiles(AppPaths.BackupFolder, "nttosaga_*.db")
            .OrderByDescending(f => f)
            .ToList();
        foreach (var vechi in fisiere.Skip(PastreazaUltimele))
            File.Delete(vechi);
    }
}
