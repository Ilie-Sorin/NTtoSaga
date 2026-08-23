namespace NttoSaga.Core.Data;

/// <summary>
/// Rezolvă locațiile fixe impuse de specificație (§7): rădăcina aplicației este
/// întotdeauna d:\nttosaga, indiferent de unde rulează executabilul.
/// Poate fi suprascrisă prin variabila de mediu NTTOSAGA_ROOT, folosită doar de teste.
/// </summary>
public static class AppPaths
{
    /// <summary>
    /// Recitită la fiecare acces (nu memorată static) pentru ca testele automate să poată
    /// izola fiecare caz într-un folder temporar propriu prin variabila de mediu.
    /// </summary>
    public static string Root => Environment.GetEnvironmentVariable("NTTOSAGA_ROOT") ?? @"d:\nttosaga";

    public static string DataFolder => Path.Combine(Root, "data");
    public static string DatabaseFile => Path.Combine(DataFolder, "nttosaga.db");
    public static string FilesFolder => Path.Combine(Root, "files");
    public static string BackupFolder => Path.Combine(Root, "backup");
    public static string LogFolder => Path.Combine(Root, "log");

    public static void EnsureFolders()
    {
        Directory.CreateDirectory(DataFolder);
        Directory.CreateDirectory(FilesFolder);
        Directory.CreateDirectory(BackupFolder);
        Directory.CreateDirectory(LogFolder);
    }
}
