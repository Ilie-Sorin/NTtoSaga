namespace NttoSaga.Core.Import;

/// <summary>O linie citită și validată din CSV, gata de import (valorile monetare deja în bani).</summary>
public class CsvRawRow
{
    public int RandCsv { get; set; }
    public string Name { get; set; } = "";
    public string PartnerName { get; set; } = "";
    public string DocNumber { get; set; } = "";
    /// <summary>Format ISO YYYY-MM-DD.</summary>
    public string DocDateIso { get; set; } = "";
    public string DocDateAfisare { get; set; } = "";
    public int RetailVatPercent { get; set; }
    public long ValAmIesireBani { get; set; }
    public long ValVatAmIesireBani { get; set; }
    public long ValAchizitieFaraTVAIesireBani { get; set; }
}
