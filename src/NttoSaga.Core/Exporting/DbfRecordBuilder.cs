using NttoSaga.Core.Models;
using NttoSaga.Core.Repositories;

namespace NttoSaga.Core.Exporting;

public class SetariExport
{
    public required string Cod { get; init; }
    public required string Tip { get; init; }
    public required string Um { get; init; }
}

/// <summary>
/// Transformă liniile importate în perechile de înregistrări DBF (§4.5) în ordinea impusă de D8.
/// </summary>
public static class DbfRecordBuilder
{
    public static List<LinieImport> Sorteaza(IEnumerable<LinieImport> linii) =>
        linii
            .OrderBy(l => l.DocDate, StringComparer.Ordinal)
            .ThenBy(l => l.DocNumber, DocNumberComparer.Instance)
            .ThenBy(l => l.RetailVatPercent)
            .ToList();

    public static List<DbfNtRecord> Construieste(
        IEnumerable<LinieImport> liniiNesortate,
        IReadOnlyList<NomenclatorGestiune> gestiuni,
        IReadOnlyList<DenumireTva> denumiriTva,
        SetariExport setari)
    {
        var linii = Sorteaza(liniiNesortate);
        var rezultat = new List<DbfNtRecord>(linii.Count * 2);

        foreach (var linie in linii)
        {
            var sursa = NomenclatoareRepository.Gaseste(gestiuni, linie.Name)
                ?? throw new ExportBlockedException(
                    $"Gestiunea sursă '{linie.Name}' (NT {linie.DocNumber} din {linie.DocDate}) nu este configurată " +
                    "în nomenclatorul de gestiuni. Adăugați-o din ecranul Setări înainte de a continua exportul.");
            var destinatie = NomenclatoareRepository.Gaseste(gestiuni, linie.PartnerName)
                ?? throw new ExportBlockedException(
                    $"Gestiunea destinație '{linie.PartnerName}' (NT {linie.DocNumber} din {linie.DocDate}) nu este " +
                    "configurată în nomenclatorul de gestiuni. Adăugați-o din ecranul Setări înainte de a continua exportul.");

            var denArt = denumiriTva.FirstOrDefault(d => d.Cota == linie.RetailVatPercent && d.Activ)
                ?? throw new ExportBlockedException(
                    $"Nu există o denumire de articol configurată pentru cota de TVA {linie.RetailVatPercent}% " +
                    $"(NT {linie.DocNumber} din {linie.DocDate}). Adăugați-o din ecranul Setări.");

            var data = DateTime.ParseExact(linie.DocDate, "yyyy-MM-dd", null);
            var valoare = linie.ValAchizitieFaraTVAIesire / 100m;
            var tva = linie.TvaCalculata / 100m;
            var pretVanz = linie.ValAmIesire / 100m;
            var nrDocument = $"{sursa.Prescurtare}{linie.DocNumber}";

            rezultat.Add(new DbfNtRecord
            {
                NrIntrare = nrDocument,
                Cod = setari.Cod,
                Data = data,
                Tip = setari.Tip,
                DenArt = denArt.DenArt,
                Um = setari.Um,
                Cantitate = -1.000m,
                TvaArt = linie.RetailVatPercent,
                Valoare = -valoare,
                Tva = -tva,
                Cont = $"{sursa.ContMarfa}.{linie.RetailVatPercent}",
                PretVanz = pretVanz,
                Grupa = sursa.Activitate,
            });

            rezultat.Add(new DbfNtRecord
            {
                NrIntrare = nrDocument,
                Cod = setari.Cod,
                Data = data,
                Tip = setari.Tip,
                DenArt = denArt.DenArt,
                Um = setari.Um,
                Cantitate = 1.000m,
                TvaArt = linie.RetailVatPercent,
                Valoare = valoare,
                Tva = tva,
                Cont = $"{destinatie.ContMarfa}.{linie.RetailVatPercent}",
                PretVanz = pretVanz,
                Grupa = destinatie.Activitate,
            });
        }

        return rezultat;
    }
}
