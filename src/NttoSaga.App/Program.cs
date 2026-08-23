using NttoSaga.Core.Data;
using NttoSaga.Core.Logging;

namespace NttoSaga.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.ThreadException += (s, e) =>
        {
            FileLogger.Eroare("Excepție netratată în interfață", e.Exception);
            MessageBox.Show(
                "A apărut o eroare neașteptată:\n\n" + e.Exception.Message +
                "\n\nDetaliile au fost salvate în jurnalul aplicației (folderul log).",
                "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        try
        {
            Database.EnsureInitialized();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Aplicația nu a putut porni deoarece nu a reușit să pregătească baza de date:\n\n" +
                ex.Message + "\n\nVerificați că folderul d:\\nttosaga poate fi scris și reporniți aplicația.",
                "Eroare la pornire", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Application.Run(new Forms.MainForm());
    }
}
