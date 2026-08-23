using NttoSaga.Core.Exporting;
using NttoSaga.Core.Logging;
using NttoSaga.Core.Models;
using NttoSaga.Core.Repositories;
using NttoSaga.Core.Backup;

namespace NttoSaga.App.Screens;

public class ExportDbfControl : UserControl, Forms.IEcranNavigabil
{
    private readonly Action _dupaGenerare;
    private readonly LiniiImportRepository _liniiRepo = new();
    private readonly NomenclatoareRepository _nomenclatoare = new();
    private readonly ExportService _exportService = new();

    private readonly CheckedListBox _listGestiuni = new() { CheckOnClick = true, Height = 140 };
    private readonly Button _btnSelecteazaTot = new() { Text = "Selectează tot", Width = 110 };
    private readonly Button _btnDeselecteazaTot = new() { Text = "Deselectează tot", Width = 110 };
    private readonly DateTimePicker _dtStart = new() { Format = DateTimePickerFormat.Short, Width = 110 };
    private readonly DateTimePicker _dtSfarsit = new() { Format = DateTimePickerFormat.Short, Width = 110 };
    private readonly Button _btnZiuaCurenta = new() { Text = "Ziua curentă", Width = 100 };
    private readonly Button _btnLunaCurenta = new() { Text = "Luna curentă", Width = 100 };
    private readonly Button _btnLunaTrecuta = new() { Text = "Luna trecută", Width = 100 };
    private readonly CheckBox _chkDoarNeexportate = new() { Text = "Doar liniile neexportate", Checked = true, AutoSize = true };

    private readonly Label _lblPreviz = new();
    private readonly DataGridView _gridPreviz = new();
    private readonly Label _lblFisier = new();
    private readonly Button _btnGenereaza = new() { Text = "Generează fișierul DBF", Width = 190, Height = 32 };

    private List<LinieImport> _liniiCurente = [];
    private bool _seSuspendaEvenimente;

    public ExportDbfControl(Action dupaGenerare)
    {
        _dupaGenerare = dupaGenerare;
        Dock = DockStyle.Fill;
        BuildUi();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, AutoScroll = true };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblTitlu = new Label { Text = "Export DBF", Font = new Font("Segoe UI", 13, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
        root.Controls.Add(lblTitlu, 0, 0);

        var grupSelectie = new GroupBox { Text = "1. Selecție", Dock = DockStyle.Fill, Height = 210 };
        var layoutSel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        layoutSel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        layoutSel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var panelGestiuni = new Panel { Dock = DockStyle.Fill };
        _listGestiuni.Dock = DockStyle.Top;
        var panelButoaneGestiuni = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30 };
        panelButoaneGestiuni.Controls.Add(_btnSelecteazaTot);
        panelButoaneGestiuni.Controls.Add(_btnDeselecteazaTot);
        panelGestiuni.Controls.Add(_listGestiuni);
        panelGestiuni.Controls.Add(panelButoaneGestiuni);
        layoutSel.Controls.Add(panelGestiuni, 0, 0);

        var panelInterval = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
        panelInterval.Controls.Add(new Label { Text = "Interval DocDate:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) });
        panelInterval.Controls.Add(_dtStart);
        panelInterval.Controls.Add(new Label { Text = "–", AutoSize = true, Margin = new Padding(4, 8, 4, 0) });
        panelInterval.Controls.Add(_dtSfarsit);
        panelInterval.Controls.Add(_btnZiuaCurenta);
        _btnZiuaCurenta.Margin = new Padding(16, 4, 4, 4);
        panelInterval.Controls.Add(_btnLunaCurenta);
        panelInterval.Controls.Add(_btnLunaTrecuta);
        panelInterval.Controls.Add(_chkDoarNeexportate);
        _chkDoarNeexportate.Margin = new Padding(16, 8, 4, 4);
        layoutSel.Controls.Add(panelInterval, 1, 0);

        grupSelectie.Controls.Add(layoutSel);
        root.Controls.Add(grupSelectie, 0, 1);

        _lblPreviz.Dock = DockStyle.Top;
        _lblPreviz.Height = 50;
        _lblPreviz.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        root.Controls.Add(_lblPreviz, 0, 2);

        var grupPreviz = new GroupBox { Text = "2. Previzualizare", Dock = DockStyle.Fill };
        _gridPreviz.Dock = DockStyle.Fill;
        _gridPreviz.ReadOnly = true;
        _gridPreviz.AllowUserToAddRows = false;
        _gridPreviz.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grupPreviz.Controls.Add(_gridPreviz);
        root.Controls.Add(grupPreviz, 0, 3);

        var grupGenerare = new GroupBox { Text = "3. Generare", Dock = DockStyle.Fill, Height = 90 };
        var layoutGen = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        _lblFisier.Dock = DockStyle.Fill;
        layoutGen.Controls.Add(_lblFisier, 0, 0);
        layoutGen.Controls.Add(_btnGenereaza, 1, 0);
        grupGenerare.Controls.Add(layoutGen);
        root.Controls.Add(grupGenerare, 0, 4);

        Controls.Add(root);

        _dtStart.Value = DateTime.Today;
        _dtSfarsit.Value = DateTime.Today;

        _btnSelecteazaTot.Click += (s, e) => SeteazaToateBifele(true);
        _btnDeselecteazaTot.Click += (s, e) => SeteazaToateBifele(false);
        _btnZiuaCurenta.Click += (s, e) => { _dtStart.Value = DateTime.Today; _dtSfarsit.Value = DateTime.Today; };
        _btnLunaCurenta.Click += (s, e) =>
        {
            var azi = DateTime.Today;
            _dtStart.Value = new DateTime(azi.Year, azi.Month, 1);
            _dtSfarsit.Value = new DateTime(azi.Year, azi.Month, DateTime.DaysInMonth(azi.Year, azi.Month));
        };
        _btnLunaTrecuta.Click += (s, e) =>
        {
            var lunaTrecuta = DateTime.Today.AddMonths(-1);
            _dtStart.Value = new DateTime(lunaTrecuta.Year, lunaTrecuta.Month, 1);
            _dtSfarsit.Value = new DateTime(lunaTrecuta.Year, lunaTrecuta.Month, DateTime.DaysInMonth(lunaTrecuta.Year, lunaTrecuta.Month));
        };

        _listGestiuni.ItemCheck += (s, e) => BeginInvoke(ActualizeazaPreviz);
        _dtStart.ValueChanged += (s, e) => ActualizeazaListaGestiuni();
        _dtSfarsit.ValueChanged += (s, e) => ActualizeazaListaGestiuni();
        _chkDoarNeexportate.CheckedChanged += (s, e) => ActualizeazaPreviz();

        _btnGenereaza.Click += (s, e) => Genereaza();
    }

    public void LaAfisare() => Reincarca();

    public void Reincarca()
    {
        ActualizeazaListaGestiuni();
    }

    private List<string> GestiuniSelectate() =>
        _listGestiuni.CheckedItems.Cast<string>().Select(ExtrageDenumire).ToList();

    private static string ExtrageDenumire(string itemText)
    {
        var idx = itemText.IndexOf(" (", StringComparison.Ordinal);
        return idx > 0 ? itemText[..idx] : itemText;
    }

    private void ActualizeazaListaGestiuni()
    {
        _seSuspendaEvenimente = true;
        var bifate = new HashSet<string>(GestiuniSelectate());
        _listGestiuni.Items.Clear();

        foreach (var g in _nomenclatoare.ListeazaGestiuni(doarActive: true))
        {
            var neexportate = _liniiRepo.NumaraNeexportate(g.Denumire, _dtStart.Value.ToString("yyyy-MM-dd"), _dtSfarsit.Value.ToString("yyyy-MM-dd"));
            var text = $"{g.Denumire} ({neexportate} linii neexportate)";
            var index = _listGestiuni.Items.Add(text);
            if (bifate.Contains(g.Denumire))
                _listGestiuni.SetItemChecked(index, true);
        }
        _seSuspendaEvenimente = false;
        ActualizeazaPreviz();
    }

    private void SeteazaToateBifele(bool valoare)
    {
        _seSuspendaEvenimente = true;
        for (int i = 0; i < _listGestiuni.Items.Count; i++)
            _listGestiuni.SetItemChecked(i, valoare);
        _seSuspendaEvenimente = false;
        ActualizeazaPreviz();
    }

    private void ActualizeazaPreviz()
    {
        if (_seSuspendaEvenimente) return;

        var gestiuni = GestiuniSelectate();
        if (gestiuni.Count == 0)
        {
            _liniiCurente = [];
            _gridPreviz.DataSource = null;
            _lblPreviz.Text = "Selectați cel puțin o gestiune sursă.";
            _lblFisier.Text = "";
            _btnGenereaza.Enabled = false;
            return;
        }

        var preview = _exportService.Previzualizeaza(gestiuni, _dtStart.Value, _dtSfarsit.Value, _chkDoarNeexportate.Checked);
        _liniiCurente = preview.Linii;

        _gridPreviz.DataSource = _liniiCurente
            .OrderBy(l => l.Name).ThenBy(l => l.DocDate).ThenBy(l => l.DocNumber)
            .Select(l => new
            {
                Gestiune_sursa = l.Name,
                Gestiune_destinatie = l.PartnerName,
                NrDocument = l.DocNumber,
                Data = l.DocDate,
                Cota_TVA = l.RetailVatPercent,
                ValoareVanzare = l.ValAmIesire / 100m,
                TVA = l.ValVatAmIesire / 100m,
                Export = l.IdExport?.ToString() ?? "neexportat",
            }).ToList();

        _lblPreviz.Text = $"{preview.NumarInregistrari} linii găsite → {preview.NumarLiniiDbf} înregistrări DBF · " +
                           $"Total valoare: {preview.TotalValoare / 100m:N2} lei · Total TVA: {preview.TotalTva / 100m:N2} lei" +
                           (preview.NumarLiniiDejaExportate > 0 ? $" · {preview.NumarLiniiDejaExportate} linii deja exportate anterior" : "");

        if (_liniiCurente.Count == 0)
        {
            _lblFisier.Text = "Nu există nicio linie de exportat pentru selecția curentă.";
            _btnGenereaza.Enabled = false;
        }
        else
        {
            var numeFisier = _exportService.PrevizualizeazaNumeFisier(_dtStart.Value, _dtSfarsit.Value);
            _lblFisier.Text = $"Se va crea: {numeFisier} în {Core.Data.AppPaths.FilesFolder}";
            _btnGenereaza.Enabled = true;
        }
    }

    private void Genereaza()
    {
        if (_liniiCurente.Count == 0) return;

        if (!_chkDoarNeexportate.Checked)
        {
            var dejaExportate = _liniiCurente.Where(l => l.EsteExportata).ToList();
            if (dejaExportate.Count > 0)
            {
                var exporturiAfectate = string.Join(", ", dejaExportate.Select(l => l.IdExport).Distinct());
                var confirmare = MessageBox.Show(this,
                    $"Selecția include {dejaExportate.Count} linii deja exportate (exporturile #{exporturiAfectate}). " +
                    "Aceste linii vor fi incluse din nou în noul fișier DBF, ceea ce poate duce la dublarea datelor în SAGA " +
                    "dacă exportul anterior a fost deja importat acolo.\n\nContinuați oricum?",
                    "Confirmare — linii deja exportate", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirmare != DialogResult.Yes) return;
            }
        }

        try
        {
            new BackupService().Copiaza();
            var gestiuni = GestiuniSelectate();
            var rezultat = _exportService.Genereaza(_liniiCurente, _dtStart.Value, _dtSfarsit.Value, gestiuni);
            FileLogger.Info($"Export generat: {rezultat.Export.NumeFisier}, {rezultat.Export.NrInregistrari} linii, {rezultat.Export.NrLiniiDbf} înregistrări DBF.");

            var raspuns = MessageBox.Show(this,
                $"Fișierul a fost generat cu succes:\n{rezultat.Export.NumeFisier}\n\nDoriți să deschideți folderul?",
                "Export finalizat", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (raspuns == DialogResult.Yes)
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{rezultat.Export.CaleFisier}\"");

            _dupaGenerare();
            ActualizeazaListaGestiuni();
        }
        catch (ExportBlockedException ex)
        {
            MessageBox.Show(this, ex.Message, "Export blocat", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (DbfOverflowException ex)
        {
            MessageBox.Show(this, ex.Message, "Export oprit", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            FileLogger.Eroare("Generare export eșuată", ex);
            MessageBox.Show(this, "Generarea a eșuat:\n\n" + ex.Message, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
