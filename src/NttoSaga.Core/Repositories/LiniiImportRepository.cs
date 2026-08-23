using Microsoft.Data.Sqlite;
using NttoSaga.Core.Models;

namespace NttoSaga.Core.Repositories;

public class LiniiImportRepository
{
    // ---- Operații folosite în tranzacția de import (CsvImporter) ----

    public LinieImport? GasesteDupaCheie(SqliteConnection conn, SqliteTransaction tx,
        string name, string docNumber, string docDate, int retailVatPercent)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT id, name, partner_name, doc_number, doc_date, retail_vat_percent,
                   val_am_iesire, val_vat_am_iesire, val_achizitie_fara_tva_iesire,
                   id_export, fisier_sursa, data_import
            FROM linii_import
            WHERE name = $name AND doc_number = $docNumber AND doc_date = $docDate
              AND retail_vat_percent = $cota;
            """;
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$docNumber", docNumber);
        cmd.Parameters.AddWithValue("$docDate", docDate);
        cmd.Parameters.AddWithValue("$cota", retailVatPercent);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Citeste(reader) : null;
    }

    public long Insereaza(SqliteConnection conn, SqliteTransaction tx, LinieImport linie)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO linii_import
                (name, partner_name, doc_number, doc_date, retail_vat_percent,
                 val_am_iesire, val_vat_am_iesire, val_achizitie_fara_tva_iesire,
                 id_export, fisier_sursa, data_import)
            VALUES
                ($name, $partnerName, $docNumber, $docDate, $cota,
                 $valAm, $valVatAm, $valAchizitie,
                 NULL, $fisierSursa, $dataImport);
            SELECT last_insert_rowid();
            """;
        AdaugaParametri(cmd, linie);
        return (long)cmd.ExecuteScalar()!;
    }

    public void Actualizeaza(SqliteConnection conn, SqliteTransaction tx, long id, LinieImport linie)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            UPDATE linii_import
            SET val_am_iesire = $valAm,
                val_vat_am_iesire = $valVatAm,
                val_achizitie_fara_tva_iesire = $valAchizitie,
                partner_name = $partnerName,
                fisier_sursa = $fisierSursa,
                data_import = $dataImport
            WHERE id = $id;
            """;
        AdaugaParametri(cmd, linie);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private static void AdaugaParametri(SqliteCommand cmd, LinieImport linie)
    {
        cmd.Parameters.AddWithValue("$name", linie.Name);
        cmd.Parameters.AddWithValue("$partnerName", linie.PartnerName);
        cmd.Parameters.AddWithValue("$docNumber", linie.DocNumber);
        cmd.Parameters.AddWithValue("$docDate", linie.DocDate);
        cmd.Parameters.AddWithValue("$cota", linie.RetailVatPercent);
        cmd.Parameters.AddWithValue("$valAm", linie.ValAmIesire);
        cmd.Parameters.AddWithValue("$valVatAm", linie.ValVatAmIesire);
        cmd.Parameters.AddWithValue("$valAchizitie", linie.ValAchizitieFaraTVAIesire);
        cmd.Parameters.AddWithValue("$fisierSursa", linie.FisierSursa);
        cmd.Parameters.AddWithValue("$dataImport", linie.DataImport);
    }

    // ---- Operații de citire / interfață ----

    public List<LinieImport> Cauta(
        IEnumerable<string>? gestiuni = null,
        string? dataStart = null,
        string? dataSfarsit = null,
        bool? doarNeexportate = null,
        int? cotaTva = null,
        string? docNumberCautat = null)
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();

        var conditii = new List<string>();
        if (gestiuni is not null)
        {
            var lista = gestiuni.ToList();
            if (lista.Count > 0)
            {
                var nume = new List<string>();
                for (int i = 0; i < lista.Count; i++)
                {
                    var p = $"$g{i}";
                    nume.Add(p);
                    cmd.Parameters.AddWithValue(p, lista[i]);
                }
                conditii.Add($"name IN ({string.Join(",", nume)})");
            }
        }
        if (dataStart is not null)
        {
            conditii.Add("doc_date >= $dataStart");
            cmd.Parameters.AddWithValue("$dataStart", dataStart);
        }
        if (dataSfarsit is not null)
        {
            conditii.Add("doc_date <= $dataSfarsit");
            cmd.Parameters.AddWithValue("$dataSfarsit", dataSfarsit);
        }
        if (doarNeexportate == true)
            conditii.Add("id_export IS NULL");
        else if (doarNeexportate == false)
            conditii.Add("id_export IS NOT NULL");
        if (cotaTva is not null)
        {
            conditii.Add("retail_vat_percent = $cota");
            cmd.Parameters.AddWithValue("$cota", cotaTva.Value);
        }
        if (!string.IsNullOrWhiteSpace(docNumberCautat))
        {
            conditii.Add("doc_number LIKE $docNr");
            cmd.Parameters.AddWithValue("$docNr", $"%{docNumberCautat.Trim()}%");
        }

        cmd.CommandText = """
            SELECT id, name, partner_name, doc_number, doc_date, retail_vat_percent,
                   val_am_iesire, val_vat_am_iesire, val_achizitie_fara_tva_iesire,
                   id_export, fisier_sursa, data_import
            FROM linii_import
            """ + (conditii.Count > 0 ? " WHERE " + string.Join(" AND ", conditii) : "") +
            " ORDER BY doc_date, doc_number, retail_vat_percent;";

        using var reader = cmd.ExecuteReader();
        var result = new List<LinieImport>();
        while (reader.Read())
            result.Add(Citeste(reader));
        return result;
    }

    /// <summary>Linii ale unei gestiuni (ca sursă) neexportate în intervalul dat — pentru afișarea contorului din §6.3.</summary>
    public int NumaraNeexportate(string gestiune, string dataStart, string dataSfarsit)
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM linii_import
            WHERE name = $gestiune AND id_export IS NULL
              AND doc_date >= $dataStart AND doc_date <= $dataSfarsit;
            """;
        cmd.Parameters.AddWithValue("$gestiune", gestiune);
        cmd.Parameters.AddWithValue("$dataStart", dataStart);
        cmd.Parameters.AddWithValue("$dataSfarsit", dataSfarsit);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int NumaraTotalNeexportate()
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM linii_import WHERE id_export IS NULL;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public bool Sterge(long id)
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM linii_import WHERE id = $id AND id_export IS NULL;";
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public void MarcheazaExportate(SqliteConnection conn, SqliteTransaction tx, IEnumerable<long> ids, long idExport)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE linii_import SET id_export = $idExport WHERE id = $id;";
        var pId = cmd.CreateParameter(); pId.ParameterName = "$id"; cmd.Parameters.Add(pId);
        var pExp = cmd.CreateParameter(); pExp.ParameterName = "$idExport"; pExp.Value = idExport; cmd.Parameters.Add(pExp);
        foreach (var id in ids)
        {
            pId.Value = id;
            cmd.ExecuteNonQuery();
        }
    }

    public void ElibereazaExport(SqliteConnection conn, SqliteTransaction tx, long idExport)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE linii_import SET id_export = NULL WHERE id_export = $idExport;";
        cmd.Parameters.AddWithValue("$idExport", idExport);
        cmd.ExecuteNonQuery();
    }

    public List<LinieImport> DupaExport(long idExport)
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, partner_name, doc_number, doc_date, retail_vat_percent,
                   val_am_iesire, val_vat_am_iesire, val_achizitie_fara_tva_iesire,
                   id_export, fisier_sursa, data_import
            FROM linii_import WHERE id_export = $idExport
            ORDER BY doc_date, doc_number, retail_vat_percent;
            """;
        cmd.Parameters.AddWithValue("$idExport", idExport);
        using var reader = cmd.ExecuteReader();
        var result = new List<LinieImport>();
        while (reader.Read())
            result.Add(Citeste(reader));
        return result;
    }

    private static LinieImport Citeste(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Name = reader.GetString(1),
        PartnerName = reader.GetString(2),
        DocNumber = reader.GetString(3),
        DocDate = reader.GetString(4),
        RetailVatPercent = (int)reader.GetInt64(5),
        ValAmIesire = reader.GetInt64(6),
        ValVatAmIesire = reader.GetInt64(7),
        ValAchizitieFaraTVAIesire = reader.GetInt64(8),
        IdExport = reader.IsDBNull(9) ? null : reader.GetInt64(9),
        FisierSursa = reader.GetString(10),
        DataImport = reader.GetString(11),
    };
}
