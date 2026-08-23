namespace NttoSaga.Core.Exporting;

/// <summary>O valoare nu încape în câmpul DBF țintă — exportul se oprește (§4.6, nicio trunchiere silențioasă).</summary>
public class DbfOverflowException : Exception
{
    public DbfOverflowException(string message) : base(message) { }
}
