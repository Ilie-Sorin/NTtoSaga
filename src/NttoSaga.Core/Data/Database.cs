using Microsoft.Data.Sqlite;

namespace NttoSaga.Core.Data;

/// <summary>
/// Creează schema SQLite și populează nomenclatoarele implicite la prima pornire (§5, §7).
/// </summary>
public static class Database
{
    public static string ConnectionString => $"Data Source={AppPaths.DatabaseFile}";

    public static SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using (var pragma = conn.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
        }
        return conn;
    }

    /// <summary>Creează folderele, baza de date și schema dacă nu există; populează valorile implicite.</summary>
    public static void EnsureInitialized()
    {
        AppPaths.EnsureFolders();
        bool dbExisted = File.Exists(AppPaths.DatabaseFile);

        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        ExecuteNonQuery(conn, tx, """
            CREATE TABLE IF NOT EXISTS linii_import (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                partner_name TEXT NOT NULL,
                doc_number TEXT NOT NULL,
                doc_date TEXT NOT NULL,
                retail_vat_percent INTEGER NOT NULL,
                val_am_iesire INTEGER NOT NULL,
                val_vat_am_iesire INTEGER NOT NULL,
                val_achizitie_fara_tva_iesire INTEGER NOT NULL,
                id_export INTEGER NULL REFERENCES exporturi(id),
                fisier_sursa TEXT NOT NULL,
                data_import TEXT NOT NULL
            );
            """);

        ExecuteNonQuery(conn, tx,
            "CREATE UNIQUE INDEX IF NOT EXISTS ux_linii ON linii_import(name, doc_number, doc_date, retail_vat_percent);");
        ExecuteNonQuery(conn, tx,
            "CREATE INDEX IF NOT EXISTS ix_linii_filtru ON linii_import(name, doc_date, id_export);");

        ExecuteNonQuery(conn, tx, """
            CREATE TABLE IF NOT EXISTS exporturi (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                data_ora TEXT NOT NULL,
                nume_fisier TEXT NOT NULL,
                cale_fisier TEXT NOT NULL,
                filtru_gestiuni TEXT NOT NULL,
                data_start TEXT NOT NULL,
                data_sfarsit TEXT NOT NULL,
                nr_inregistrari INTEGER NOT NULL,
                nr_linii_dbf INTEGER NOT NULL,
                total_valoare INTEGER NOT NULL,
                total_tva INTEGER NOT NULL,
                stare TEXT NOT NULL,
                observatii TEXT NOT NULL DEFAULT ''
            );
            """);

        ExecuteNonQuery(conn, tx, """
            CREATE TABLE IF NOT EXISTS nomenclator_gestiuni (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                denumire TEXT NOT NULL UNIQUE,
                cont_marfa TEXT NOT NULL,
                activitate TEXT NOT NULL,
                activ INTEGER NOT NULL DEFAULT 1
            );
            """);

        ExecuteNonQuery(conn, tx, """
            CREATE TABLE IF NOT EXISTS denumiri_tva (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                cota INTEGER NOT NULL UNIQUE,
                den_art TEXT NOT NULL,
                activ INTEGER NOT NULL DEFAULT 1
            );
            """);

        ExecuteNonQuery(conn, tx, """
            CREATE TABLE IF NOT EXISTS setari (
                cheie TEXT PRIMARY KEY,
                valoare TEXT NOT NULL
            );
            """);

        if (!dbExisted)
        {
            SeedNomenclatorGestiuni(conn, tx);
            SeedDenumiriTva(conn, tx);
            SeedSetari(conn, tx);
        }

        tx.Commit();
    }

    private static void SeedNomenclatorGestiuni(SqliteConnection conn, SqliteTransaction tx)
    {
        (string Denumire, string ContMarfa, string Activitate)[] seed =
        [
            ("ANCAFARM1", "371.00001", "01"),
            ("ANCAFARM2", "371.00002", "02"),
            ("ANCAFARM3", "371.00003", "03"),
            ("ANCAFARM4", "371.00004", "04"),
            ("DEPOZIT ANCA1", "371.00006", "06"),
            ("DEPOZIT", "371.00009", "09"),
            ("LABORATOR", "371.00008", "08"),
        ];

        foreach (var (denumire, contMarfa, activitate) in seed)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO nomenclator_gestiuni (denumire, cont_marfa, activitate, activ)
                VALUES ($denumire, $cont, $act, 1);
                """;
            cmd.Parameters.AddWithValue("$denumire", denumire);
            cmd.Parameters.AddWithValue("$cont", contMarfa);
            cmd.Parameters.AddWithValue("$act", activitate);
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedDenumiriTva(SqliteConnection conn, SqliteTransaction tx)
    {
        (int Cota, string DenArt)[] seed =
        [
            (0, "Scutite de TVA"),
            (11, "Medicamente TVA 11%"),
            (21, "Parafarmaceutice TVA<>11%"),
        ];

        foreach (var (cota, denArt) in seed)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO denumiri_tva (cota, den_art, activ) VALUES ($cota, $den, 1);";
            cmd.Parameters.AddWithValue("$cota", cota);
            cmd.Parameters.AddWithValue("$den", denArt);
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedSetari(SqliteConnection conn, SqliteTransaction tx)
    {
        (string Cheie, string Valoare)[] seed =
        [
            ("COD", "20100"),
            ("TIP", "A"),
            ("UM", "BUC"),
            ("latime_index", "4"),
            ("folder_csv_implicit", AppPaths.FilesFolder),
            ("folder_dbf_implicit", AppPaths.FilesFolder),
        ];

        foreach (var (cheie, valoare) in seed)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO setari (cheie, valoare) VALUES ($cheie, $valoare);";
            cmd.Parameters.AddWithValue("$cheie", cheie);
            cmd.Parameters.AddWithValue("$valoare", valoare);
            cmd.ExecuteNonQuery();
        }
    }

    private static void ExecuteNonQuery(SqliteConnection conn, SqliteTransaction tx, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
