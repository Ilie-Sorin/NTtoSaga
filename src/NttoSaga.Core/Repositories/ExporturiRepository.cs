using Microsoft.Data.Sqlite;
using NttoSaga.Core.Models;

namespace NttoSaga.Core.Repositories;

public class ExporturiRepository
{
    /// <summary>
    /// Indexul pe care îl va primi următorul export, pentru afișarea numelui exact al fișierului
    /// înainte de generare (§6.3). Doar o previzualizare — valoarea reală se alocă la Rezerva().
    /// </summary>
    public long PrevizualizeazaUrmatorulIndex()
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT seq FROM sqlite_sequence WHERE name = 'exporturi';";
        var rezultat = cmd.ExecuteScalar();
        return (rezultat is null ? 0L : (long)rezultat) + 1;
    }

    /// <summary>
    /// Rezervă un rând (și deci indexul autoincrement) în cadrul tranzacției de export.
    /// Dacă tranzacția e anulată (scrierea DBF eșuează), SQLite reface sqlite_sequence
    /// și numărul devine disponibil din nou — corect, fiindcă niciun fișier nu a existat.
    /// </summary>
    public long Rezerva(SqliteConnection conn, SqliteTransaction tx)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO exporturi
                (data_ora, nume_fisier, cale_fisier, filtru_gestiuni, data_start, data_sfarsit,
                 nr_inregistrari, nr_linii_dbf, total_valoare, total_tva, stare, observatii)
            VALUES ('', '', '', '', '', '', 0, 0, 0, 0, 'in_curs', '');
            SELECT last_insert_rowid();
            """;
        return (long)cmd.ExecuteScalar()!;
    }

    public void Finalizeaza(SqliteConnection conn, SqliteTransaction tx, Export export)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            UPDATE exporturi
            SET data_ora = $dataOra, nume_fisier = $numeFisier, cale_fisier = $caleFisier,
                filtru_gestiuni = $filtru, data_start = $dataStart, data_sfarsit = $dataSfarsit,
                nr_inregistrari = $nrInreg, nr_linii_dbf = $nrLiniiDbf,
                total_valoare = $totalValoare, total_tva = $totalTva, stare = 'activ'
            WHERE id = $id;
            """;
        cmd.Parameters.AddWithValue("$dataOra", export.DataOra);
        cmd.Parameters.AddWithValue("$numeFisier", export.NumeFisier);
        cmd.Parameters.AddWithValue("$caleFisier", export.CaleFisier);
        cmd.Parameters.AddWithValue("$filtru", export.FiltruGestiuni);
        cmd.Parameters.AddWithValue("$dataStart", export.DataStart);
        cmd.Parameters.AddWithValue("$dataSfarsit", export.DataSfarsit);
        cmd.Parameters.AddWithValue("$nrInreg", export.NrInregistrari);
        cmd.Parameters.AddWithValue("$nrLiniiDbf", export.NrLiniiDbf);
        cmd.Parameters.AddWithValue("$totalValoare", export.TotalValoare);
        cmd.Parameters.AddWithValue("$totalTva", export.TotalTva);
        cmd.Parameters.AddWithValue("$id", export.Id);
        cmd.ExecuteNonQuery();
    }

    public void Anuleaza(SqliteConnection conn, SqliteTransaction tx, long id)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE exporturi SET stare = 'anulat' WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public Export? Gaseste(long id)
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SelectSql + " WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Citeste(reader) : null;
    }

    public List<Export> Listeaza()
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SelectSql + " WHERE stare != 'in_curs' ORDER BY id DESC;";
        using var reader = cmd.ExecuteReader();
        var result = new List<Export>();
        while (reader.Read())
            result.Add(Citeste(reader));
        return result;
    }

    private const string SelectSql = """
        SELECT id, data_ora, nume_fisier, cale_fisier, filtru_gestiuni, data_start, data_sfarsit,
               nr_inregistrari, nr_linii_dbf, total_valoare, total_tva, stare, observatii
        FROM exporturi
        """;

    private static Export Citeste(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        DataOra = reader.GetString(1),
        NumeFisier = reader.GetString(2),
        CaleFisier = reader.GetString(3),
        FiltruGestiuni = reader.GetString(4),
        DataStart = reader.GetString(5),
        DataSfarsit = reader.GetString(6),
        NrInregistrari = (int)reader.GetInt64(7),
        NrLiniiDbf = (int)reader.GetInt64(8),
        TotalValoare = reader.GetInt64(9),
        TotalTva = reader.GetInt64(10),
        Stare = reader.GetString(11) == "anulat" ? StareExport.Anulat : StareExport.Activ,
        Observatii = reader.GetString(12),
    };
}
