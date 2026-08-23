using System.Globalization;
using System.Text;

namespace NttoSaga.Core.Exporting;

/// <summary>
/// Scrie fișiere dBASE III (fără memo) în formatul acceptat de importul SAGA, conform §4.1/§4.6.
/// Nu folosește nicio bibliotecă externă — structura e simplă și fixă, scrisă direct pe octeți.
/// </summary>
public static class DbfWriter
{
    private sealed record CampDbf(string Nume, char Tip, byte Lungime, byte Zecimale);

    private static readonly CampDbf[] Campuri =
    [
        new("NR_NIR", 'C', 16, 0),
        new("NR_INTRARE", 'C', 16, 0),
        new("GESTIUNE", 'C', 4, 0),
        new("DEN_GEST", 'C', 36, 0),
        new("COD", 'C', 5, 0),
        new("DATA", 'D', 8, 0),
        new("SCADENT", 'D', 8, 0),
        new("TIP", 'C', 1, 0),
        new("TVAI", 'N', 1, 0),
        new("COD_ART", 'C', 16, 0),
        new("DEN_ART", 'C', 60, 0),
        new("UM", 'C', 5, 0),
        new("CANTITATE", 'N', 14, 3),
        new("DEN_TIP", 'C', 36, 0),
        new("TVA_ART", 'N', 2, 0),
        new("VALOARE", 'N', 15, 2),
        new("TVA", 'N', 15, 2),
        new("CONT", 'C', 20, 0),
        new("PRET_VANZ", 'N', 15, 2),
        new("GRUPA", 'C', 16, 0),
    ];

    private const int LungimeAntet = 673; // 32 + 20*32 + 1
    private const int LungimeInregistrare = 310; // 1 + suma lungimilor de câmp

    /// <summary>
    /// Scrie înregistrările în <paramref name="caleFisier"/>.
    /// <paramref name="dataCreare"/> implicit = azi; parametrizabil pentru teste reproductibile.
    /// </summary>
    public static void Scrie(string caleFisier, IReadOnlyList<DbfNtRecord> inregistrari, DateTime? dataCreare = null)
    {
        var data = dataCreare ?? DateTime.Now;

        using var stream = new FileStream(caleFisier, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        ScrieAntet(writer, inregistrari.Count, data);
        ScrieDescriptoriCampuri(writer);
        writer.Write((byte)0x0D); // terminator antet

        foreach (var inreg in inregistrari)
            ScrieInregistrare(writer, inreg);

        writer.Write((byte)0x1A); // terminator fișier
    }

    private static void ScrieAntet(BinaryWriter writer, int numarInregistrari, DateTime dataCreare)
    {
        writer.Write((byte)0x03);
        writer.Write((byte)(dataCreare.Year % 100));
        writer.Write((byte)dataCreare.Month);
        writer.Write((byte)dataCreare.Day);
        writer.Write((uint)numarInregistrari);
        writer.Write((ushort)LungimeAntet);
        writer.Write((ushort)LungimeInregistrare);
        writer.Write(new byte[20]); // rezervat, inclusiv octetul 29 (language driver) = 0x00
    }

    private static void ScrieDescriptoriCampuri(BinaryWriter writer)
    {
        foreach (var camp in Campuri)
        {
            var numeBytes = new byte[11];
            var numeAscii = Encoding.ASCII.GetBytes(camp.Nume);
            Array.Copy(numeAscii, numeBytes, numeAscii.Length);
            writer.Write(numeBytes);
            writer.Write((byte)camp.Tip);
            writer.Write(new byte[4]); // adresă câmp — nefolosită
            writer.Write(camp.Lungime);
            writer.Write(camp.Zecimale);
            writer.Write(new byte[14]); // rezervat
        }
    }

    private static void ScrieInregistrare(BinaryWriter writer, DbfNtRecord r)
    {
        writer.Write((byte)0x20); // marcaj ștergere: neșters

        Scrie(writer, "NR_NIR", 'C', 16, 0, "");
        Scrie(writer, "NR_INTRARE", 'C', 16, 0, r.NrIntrare);
        Scrie(writer, "GESTIUNE", 'C', 4, 0, "");
        Scrie(writer, "DEN_GEST", 'C', 36, 0, "");
        Scrie(writer, "COD", 'C', 5, 0, r.Cod);
        Scrie(writer, "DATA", 'D', 8, 0, r.Data);
        Scrie(writer, "SCADENT", 'D', 8, 0, r.Data);
        Scrie(writer, "TIP", 'C', 1, 0, r.Tip);
        Scrie(writer, "TVAI", 'N', 1, 0, 0m);
        Scrie(writer, "COD_ART", 'C', 16, 0, "");
        Scrie(writer, "DEN_ART", 'C', 60, 0, r.DenArt);
        Scrie(writer, "UM", 'C', 5, 0, r.Um);
        Scrie(writer, "CANTITATE", 'N', 14, 3, r.Cantitate);
        Scrie(writer, "DEN_TIP", 'C', 36, 0, "");
        Scrie(writer, "TVA_ART", 'N', 2, 0, (decimal)r.TvaArt);
        Scrie(writer, "VALOARE", 'N', 15, 2, r.Valoare);
        Scrie(writer, "TVA", 'N', 15, 2, r.Tva);
        Scrie(writer, "CONT", 'C', 20, 0, r.Cont);
        Scrie(writer, "PRET_VANZ", 'N', 15, 2, r.PretVanz);
        Scrie(writer, "GRUPA", 'C', 16, 0, r.Grupa);
    }

    private static void Scrie(BinaryWriter writer, string camp, char tip, int lungime, int zecimale, string valoare)
    {
        var ascii = StripDiacritics(valoare);
        if (ascii.Length > lungime)
            throw new DbfOverflowException(
                $"Valoarea '{valoare}' pentru câmpul {camp} are {ascii.Length} caractere, dar limita este {lungime}. " +
                "Exportul a fost oprit — scurtați valoarea din configurare sau din datele sursă.");
        var text = ascii.PadRight(lungime);
        writer.Write(Encoding.ASCII.GetBytes(text));
    }

    private static void Scrie(BinaryWriter writer, string camp, char tip, int lungime, int zecimale, decimal valoare)
    {
        var text = valoare.ToString(zecimale > 0 ? "F" + zecimale : "F0", CultureInfo.InvariantCulture);
        if (text.Length > lungime)
            throw new DbfOverflowException(
                $"Valoarea {text} pentru câmpul {camp} depășește lungimea maximă de {lungime} poziții. " +
                "Exportul a fost oprit — verificați sumele din liniile selectate.");
        var padded = text.PadLeft(lungime);
        writer.Write(Encoding.ASCII.GetBytes(padded));
    }

    private static void Scrie(BinaryWriter writer, string camp, char tip, int lungime, int zecimale, DateTime valoare)
    {
        var text = valoare.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        writer.Write(Encoding.ASCII.GetBytes(text.PadRight(lungime)[..lungime]));
    }

    /// <summary>Elimină diacriticele românești și orice alt caracter non-ASCII (§4.6).</summary>
    public static string StripDiacritics(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            sb.Append(ch switch
            {
                'ă' or 'â' or 'à' or 'á' or 'ä' => 'a',
                'Ă' or 'Â' or 'À' or 'Á' or 'Ä' => 'A',
                'î' or 'ì' or 'í' or 'ï' => 'i',
                'Î' or 'Ì' or 'Í' or 'Ï' => 'I',
                'ș' or 'ş' => 's',
                'Ș' or 'Ş' => 'S',
                'ț' or 'ţ' => 't',
                'Ț' or 'Ţ' => 'T',
                'ô' or 'ö' or 'ò' or 'ó' => 'o',
                'Ô' or 'Ö' or 'Ò' or 'Ó' => 'O',
                'ù' or 'ú' or 'ü' => 'u',
                'Ù' or 'Ú' or 'Ü' => 'U',
                _ => ch,
            });
        }

        var normalizat = sb.ToString().Normalize(NormalizationForm.FormD);
        var final = new StringBuilder(normalizat.Length);
        foreach (var ch in normalizat)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            final.Append(ch < 128 ? ch : '?');
        }
        return final.ToString();
    }
}
