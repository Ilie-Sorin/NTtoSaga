namespace NttoSaga.Core.Models;

public class LinieImport
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string PartnerName { get; set; } = "";
    public string DocNumber { get; set; } = "";
    /// <summary>Format ISO YYYY-MM-DD.</summary>
    public string DocDate { get; set; } = "";
    public int RetailVatPercent { get; set; }
    /// <summary>Valoare în bani (întreg), nu float.</summary>
    public long ValAmIesire { get; set; }
    public long ValVatAmIesire { get; set; }
    public long ValAchizitieFaraTVAIesire { get; set; }
    public long? IdExport { get; set; }
    public string FisierSursa { get; set; } = "";
    public string DataImport { get; set; } = "";

    public bool EsteExportata => IdExport.HasValue;

    /// <summary>TVA corect de export — vezi <see cref="TvaCalculator"/>. Rezultat în bani, ca ValAmIesire/ValVatAmIesire.</summary>
    public long TvaCalculata => TvaCalculator.CalculeazaTvaBani(RetailVatPercent, ValAchizitieFaraTVAIesire);

    /// <summary>Compară valorile economice (nu id/metadate) — folosit la reimport (D3).</summary>
    public bool AreAceleasiValori(LinieImport other) =>
        ValAmIesire == other.ValAmIesire &&
        ValVatAmIesire == other.ValVatAmIesire &&
        ValAchizitieFaraTVAIesire == other.ValAchizitieFaraTVAIesire;
}
