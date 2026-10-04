using Microsoft.Data.Sqlite;
using NttoSaga.Core.Models;

namespace NttoSaga.Core.Repositories;

public class NomenclatoareRepository
{
    public List<NomenclatorGestiune> ListeazaGestiuni(bool doarActive = false)
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = doarActive
            ? "SELECT id, denumire, cont_marfa, activitate, activ, prescurtare FROM nomenclator_gestiuni WHERE activ = 1 ORDER BY denumire;"
            : "SELECT id, denumire, cont_marfa, activitate, activ, prescurtare FROM nomenclator_gestiuni ORDER BY denumire;";
        using var reader = cmd.ExecuteReader();
        var result = new List<NomenclatorGestiune>();
        while (reader.Read())
        {
            result.Add(new NomenclatorGestiune
            {
                Id = reader.GetInt64(0),
                Denumire = reader.GetString(1),
                ContMarfa = reader.GetString(2),
                Activitate = reader.GetString(3),
                Activ = reader.GetInt64(4) != 0,
                Prescurtare = reader.GetString(5),
            });
        }
        return result;
    }

    /// <summary>Potrivire pe șirul complet, insensibilă la majuscule, ignorând spațiile de capăt (§4.4).</summary>
    public static NomenclatorGestiune? Gaseste(IEnumerable<NomenclatorGestiune> gestiuni, string denumire)
    {
        var cautat = denumire.Trim();
        return gestiuni.FirstOrDefault(g =>
            string.Equals(g.Denumire.Trim(), cautat, StringComparison.OrdinalIgnoreCase));
    }

    public void Adauga(string denumire, string contMarfa, string activitate, string prescurtare = "")
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO nomenclator_gestiuni (denumire, cont_marfa, activitate, activ, prescurtare)
            VALUES ($denumire, $cont, $act, 1, $prescurtare);
            """;
        cmd.Parameters.AddWithValue("$denumire", denumire.Trim());
        cmd.Parameters.AddWithValue("$cont", contMarfa.Trim());
        cmd.Parameters.AddWithValue("$act", activitate.Trim());
        cmd.Parameters.AddWithValue("$prescurtare", prescurtare.Trim());
        cmd.ExecuteNonQuery();
    }

    public void Actualizeaza(long id, string denumire, string contMarfa, string activitate, bool activ, string prescurtare)
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE nomenclator_gestiuni
            SET denumire = $denumire, cont_marfa = $cont, activitate = $act, activ = $activ, prescurtare = $prescurtare
            WHERE id = $id;
            """;
        cmd.Parameters.AddWithValue("$denumire", denumire.Trim());
        cmd.Parameters.AddWithValue("$cont", contMarfa.Trim());
        cmd.Parameters.AddWithValue("$act", activitate.Trim());
        cmd.Parameters.AddWithValue("$activ", activ ? 1 : 0);
        cmd.Parameters.AddWithValue("$prescurtare", prescurtare.Trim());
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public List<DenumireTva> ListeazaDenumiriTva(bool doarActive = false)
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = doarActive
            ? "SELECT id, cota, den_art, activ FROM denumiri_tva WHERE activ = 1 ORDER BY cota;"
            : "SELECT id, cota, den_art, activ FROM denumiri_tva ORDER BY cota;";
        using var reader = cmd.ExecuteReader();
        var result = new List<DenumireTva>();
        while (reader.Read())
        {
            result.Add(new DenumireTva
            {
                Id = reader.GetInt64(0),
                Cota = (int)reader.GetInt64(1),
                DenArt = reader.GetString(2),
                Activ = reader.GetInt64(3) != 0,
            });
        }
        return result;
    }

    public void AdaugaDenumireTva(int cota, string denArt)
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO denumiri_tva (cota, den_art, activ) VALUES ($cota, $den, 1);";
        cmd.Parameters.AddWithValue("$cota", cota);
        cmd.Parameters.AddWithValue("$den", denArt);
        cmd.ExecuteNonQuery();
    }

    public void ActualizeazaDenumireTva(long id, int cota, string denArt, bool activ)
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE denumiri_tva SET cota = $cota, den_art = $den, activ = $activ WHERE id = $id;";
        cmd.Parameters.AddWithValue("$cota", cota);
        cmd.Parameters.AddWithValue("$den", denArt);
        cmd.Parameters.AddWithValue("$activ", activ ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public Dictionary<string, string> ListeazaSetari()
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT cheie, valoare FROM setari;";
        using var reader = cmd.ExecuteReader();
        var result = new Dictionary<string, string>();
        while (reader.Read())
            result[reader.GetString(0)] = reader.GetString(1);
        return result;
    }

    public void SalveazaSetare(string cheie, string valoare)
    {
        using var conn = Data.Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO setari (cheie, valoare) VALUES ($cheie, $valoare)
            ON CONFLICT(cheie) DO UPDATE SET valoare = excluded.valoare;
            """;
        cmd.Parameters.AddWithValue("$cheie", cheie);
        cmd.Parameters.AddWithValue("$valoare", valoare);
        cmd.ExecuteNonQuery();
    }
}
