using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using NttoSaga.Core.Data;
using NttoSaga.Core.Models;
using NttoSaga.Core.Repositories;

namespace NttoSaga.Core.Import;

public class ImportCsvException : Exception
{
    public ImportCsvException(string message) : base(message) { }
}

/// <summary>
/// Importă raportul CSV „Centralizatoare intrări și ieșiri nefiscale" conform §3 și §5.4.
/// Coloanele se identifică după numele din antet, niciodată după poziție (§3.1).
/// </summary>
public class CsvImporter
{
    private static readonly string[] ColoaneObligatorii =
    [
        "Name", "PartnerName", "DocNumber", "DocDate", "RetailVatPercent",
        "ValAmIesire", "ValVatAmIesire", "ValAchizitieFaraTVAIesire",
    ];

    private readonly LiniiImportRepository _liniiRepo = new();

    public ValidareCsvResult Valideaza(string caleCsv, IEnumerable<NomenclatorGestiune> gestiuniCunoscute)
    {
        var result = new ValidareCsvResult();
        var randuri = Parseaza(caleCsv, result.ColoaneLipsa, result.RanduriInvalide);
        result.RanduriValide.AddRange(randuri);

        if (result.ColoaneLipsa.Count > 0)
            return result;

        var cunoscute = gestiuniCunoscute.ToList();
        var necunoscute = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in randuri)
        {
            if (NomenclatoareRepository.Gaseste(cunoscute, r.Name) is null)
                necunoscute.Add(r.Name);
            if (NomenclatoareRepository.Gaseste(cunoscute, r.PartnerName) is null)
                necunoscute.Add(r.PartnerName);
        }
        result.GestiuniNecunoscute.AddRange(necunoscute);

        return result;
    }

    public ImportResult Importa(string caleCsv)
    {
        var coloaneLipsa = new List<string>();
        var randuriInvalide = new List<(int, string)>();
        var randuri = Parseaza(caleCsv, coloaneLipsa, randuriInvalide);

        if (coloaneLipsa.Count > 0)
            throw new ImportCsvException(
                $"Fișierul CSV nu conține coloanele necesare: {string.Join(", ", coloaneLipsa)}. " +
                "Verificați că ați exportat raportul corect din sistemul de gestiune.");
        if (randuriInvalide.Count > 0)
        {
            var exemplu = randuriInvalide[0];
            throw new ImportCsvException(
                $"Fișierul CSV conține {randuriInvalide.Count} rând(uri) cu date nevalide " +
                $"(ex.: rândul {exemplu.Item1} — {exemplu.Item2}). Corectați fișierul sursă și reîncercați.");
        }

        var result = new ImportResult { TotalRanduriProcesate = randuri.Count };
        var numeFisier = Path.GetFileName(caleCsv);
        var dataImport = DateTime.Now.ToString("o", CultureInfo.InvariantCulture);

        using var conn = Database.OpenConnection();
        using var tx = conn.BeginTransaction();

        foreach (var r in randuri)
        {
            if (r.ValAmIesireBani == 0)
            {
                result.IgnorateValoareZero++;
                continue;
            }

            var nou = new LinieImport
            {
                Name = r.Name,
                PartnerName = r.PartnerName,
                DocNumber = r.DocNumber,
                DocDate = r.DocDateIso,
                RetailVatPercent = r.RetailVatPercent,
                ValAmIesire = r.ValAmIesireBani,
                ValVatAmIesire = r.ValVatAmIesireBani,
                ValAchizitieFaraTVAIesire = r.ValAchizitieFaraTVAIesireBani,
                FisierSursa = numeFisier,
                DataImport = dataImport,
            };

            var existent = _liniiRepo.GasesteDupaCheie(conn, tx, r.Name, r.DocNumber, r.DocDateIso, r.RetailVatPercent);
            if (existent is null)
            {
                _liniiRepo.Insereaza(conn, tx, nou);
                result.Inserate++;
                continue;
            }

            bool identice = existent.AreAceleasiValori(nou);
            if (identice)
            {
                result.IgnorateIdentice++;
            }
            else if (existent.IdExport is null)
            {
                _liniiRepo.Actualizeaza(conn, tx, existent.Id, nou);
                result.Actualizate++;
            }
            else
            {
                result.Conflicte.Add(new ConflictImport { Existent = existent, DinCsv = r });
            }
        }

        tx.Commit();
        return result;
    }

    /// <summary>Citește și validează structural fișierul; nu atinge baza de date.</summary>
    private static List<CsvRawRow> Parseaza(string caleCsv, List<string> coloaneLipsa, List<(int, string)> randuriInvalide)
    {
        var linii = File.ReadAllLines(caleCsv, System.Text.Encoding.UTF8);

        int indexAntet = -1;
        string[] antetTokens = [];
        for (int i = 0; i < linii.Length; i++)
        {
            var tokens = linii[i].Split(',');
            if (ColoaneObligatorii.All(c => tokens.Contains(c)))
            {
                indexAntet = i;
                antetTokens = tokens;
                break;
            }
        }

        if (indexAntet == -1)
        {
            coloaneLipsa.AddRange(ColoaneObligatorii);
            return [];
        }

        var lipsa = ColoaneObligatorii.Where(c => !antetTokens.Contains(c)).ToList();
        if (lipsa.Count > 0)
        {
            coloaneLipsa.AddRange(lipsa);
            return [];
        }

        var liniiUtile = new List<(int NrLinie, string Text)>();
        for (int i = indexAntet + 1; i < linii.Length; i++)
        {
            var linie = linii[i];
            if (linie.Replace(",", "").Trim().Length == 0)
                continue;
            liniiUtile.Add((i + 1, linie));
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true,
            BadDataFound = null,
            MissingFieldFound = null,
        };

        var textCsv = linii[indexAntet] + "\n" + string.Join("\n", liniiUtile.Select(l => l.Text));
        using var stringReader = new StringReader(textCsv);
        using var csv = new CsvReader(stringReader, config);
        csv.Read();
        csv.ReadHeader();

        var rezultat = new List<CsvRawRow>();
        int pozitie = 0;
        while (csv.Read())
        {
            int nrLinieFisier = liniiUtile[pozitie].NrLinie;
            pozitie++;

            var row = new CsvRawRow { RandCsv = nrLinieFisier };
            var erori = new List<string>();

            row.Name = (csv.GetField("Name") ?? "").Trim();
            row.PartnerName = (csv.GetField("PartnerName") ?? "").Trim();
            row.DocNumber = (csv.GetField("DocNumber") ?? "").Trim();
            var docDateRaw = (csv.GetField("DocDate") ?? "").Trim();
            row.DocDateAfisare = docDateRaw;

            // Rând de subtotal/total al raportului (Name, PartnerName și DocNumber toate goale) — se ignoră silențios,
            // nu e o eroare de date (§3.1: rândurile finale de total nu conțin o linie de transfer reală).
            if (row.Name.Length == 0 && row.PartnerName.Length == 0 && row.DocNumber.Length == 0)
                continue;

            if (row.Name.Length == 0) erori.Add("lipsește Name (gestiune sursă)");
            if (row.PartnerName.Length == 0) erori.Add("lipsește PartnerName (gestiune destinație)");
            if (row.DocNumber.Length == 0) erori.Add("lipsește DocNumber");

            if (DateTime.TryParseExact(docDateRaw, "dd.MM.yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var docDate))
            {
                row.DocDateIso = docDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            else
            {
                erori.Add($"DocDate nevalid ('{docDateRaw}', așteptat format zz.ll.aaaa)");
            }

            if (!TryParseCota(csv.GetField("RetailVatPercent"), out var cota))
                erori.Add($"RetailVatPercent nevalid ('{csv.GetField("RetailVatPercent")}')");
            else
                row.RetailVatPercent = cota;

            if (!TryParseBani(csv.GetField("ValAmIesire"), out var valAm))
                erori.Add($"ValAmIesire nevalid ('{csv.GetField("ValAmIesire")}')");
            else
                row.ValAmIesireBani = valAm;

            if (!TryParseBani(csv.GetField("ValVatAmIesire"), out var valVat))
                erori.Add($"ValVatAmIesire nevalid ('{csv.GetField("ValVatAmIesire")}')");
            else
                row.ValVatAmIesireBani = valVat;

            if (!TryParseBani(csv.GetField("ValAchizitieFaraTVAIesire"), out var valAchiz))
                erori.Add($"ValAchizitieFaraTVAIesire nevalid ('{csv.GetField("ValAchizitieFaraTVAIesire")}')");
            else
                row.ValAchizitieFaraTVAIesireBani = valAchiz;

            if (erori.Count > 0)
            {
                randuriInvalide.Add((nrLinieFisier, string.Join("; ", erori)));
                continue;
            }

            rezultat.Add(row);
        }

        return rezultat;
    }

    private static bool TryParseCota(string? text, out int cota)
    {
        cota = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (!decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var val)) return false;
        cota = (int)val;
        return true;
    }

    private static bool TryParseBani(string? text, out long bani)
    {
        bani = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (!decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var val)) return false;
        bani = (long)Math.Round(val * 100m, 0, MidpointRounding.AwayFromZero);
        return true;
    }
}
