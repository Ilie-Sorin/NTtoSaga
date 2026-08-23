using NttoSaga.Core.Data;

namespace NttoSaga.Tests;

/// <summary>Izolează fiecare test într-un folder temporar propriu (bază SQLite + foldere).</summary>
public static class TestEnv
{
    public static string FolderNou()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nttosaga_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        Environment.SetEnvironmentVariable("NTTOSAGA_ROOT", dir);
        Database.EnsureInitialized();
        return dir;
    }
}
