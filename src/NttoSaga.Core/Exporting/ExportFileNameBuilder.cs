using System.Globalization;

namespace NttoSaga.Core.Exporting;

/// <summary>Construiește numele fișierului conform §4.7: IN_&lt;start&gt;_&lt;sfârșit&gt;_NT_&lt;index&gt;.dbf</summary>
public static class ExportFileNameBuilder
{
    public static string Construieste(DateTime dataStart, DateTime dataSfarsit, long index, int latimeIndex)
    {
        var start = dataStart.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        var sfarsit = dataSfarsit.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        var indexText = index.ToString(CultureInfo.InvariantCulture);
        if (indexText.Length < latimeIndex)
            indexText = indexText.PadLeft(latimeIndex, '0');
        return $"IN_{start}_{sfarsit}_NT_{indexText}.dbf";
    }
}
