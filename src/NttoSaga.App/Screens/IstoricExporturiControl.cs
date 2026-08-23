using NttoSaga.Core.Exporting;
using NttoSaga.Core.Logging;
using NttoSaga.Core.Repositories;

namespace NttoSaga.App.Screens;

public class IstoricExporturiControl : UserControl, Forms.IEcranNavigabil
{
    private readonly Action _dupaModificare;
    private readonly ExporturiRepository _exporturiRepo = new();
    private readonly ExportService _exportService = new();

    private readonly DataGridView _grid = new();
    private readonly Button _btnRegenereaza = new() { Text = "Regenerează fișierul", Width = 150 };
    private readonly Button _btnAnuleaza = new() { Text = "Anulează exportul", Width = 140 };
    private readonly Button _btnDeschideFolder = new() { Text = "Deschide folderul", Width = 140 };
    private readonly Button _btnDeschideFisier = new() { Text = "Deschide fișierul", Width = 140 };

    public IstoricExporturiControl(Action dupaModificare)
    {
        _dupaModificare = dupaModificare;
        Dock = DockStyle.Fill;
        BuildUi();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lblTitlu = new Label { Text = "Istoric exporturi", Font = new Font("Segoe UI", 13, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
        root.Controls.Add(lblTitlu, 0, 0);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        toolbar.Controls.Add(_btnRegenereaza);
        toolbar.Controls.Add(_btnAnuleaza);
        toolbar.Controls.Add(_btnDeschideFolder);
        toolbar.Controls.Add(_btnDeschideFisier);
        root.Controls.Add(toolbar, 0, 1);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        root.Controls.Add(_grid, 0, 2);

        Controls.Add(root);

        _btnRegenereaza.Click += (s, e) => Regenereaza();
        _btnAnuleaza.Click += (s, e) => Anuleaza();
        _btnDeschideFolder.Click += (s, e) => DeschideFolder();
        _btnDeschideFisier.Click += (s, e) => DeschideFisier();
    }

    public void LaAfisare() => Reincarca();

    public void Reincarca()
    {
        var exporturi = _exporturiRepo.Listeaza();
        _grid.DataSource = exporturi.Select(e => new
        {
            Index = e.Id,
            e.DataOra,
            Gestiuni = e.FiltruGestiuni,
            Interval = $"{e.DataStart} – {e.DataSfarsit}",
            Linii = e.NrInregistrari,
            InregistrariDbf = e.NrLiniiDbf,
            TotalValoare = e.TotalValoare / 100m,
            TotalTva = e.TotalTva / 100m,
            e.NumeFisier,
            Stare = e.Stare == Core.Models.StareExport.Anulat ? "anulat" : "activ",
        }).ToList();
    }

    private long? IdSelectat() => _grid.CurrentRow is null ? null : (long)_grid.CurrentRow.Cells["Index"].Value;

    private void Regenereaza()
    {
        var id = IdSelectat();
        if (id is null) { ArataNicioSelectie(); return; }

        try
        {
            _exportService.Regenereaza(id.Value);
            FileLogger.Info($"Export #{id} regenerat.");
            MessageBox.Show(this, "Fișierul a fost regenerat cu succes.", "Regenerare reușită", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Reincarca();
        }
        catch (Exception ex)
        {
            FileLogger.Eroare($"Regenerare export #{id} eșuată", ex);
            MessageBox.Show(this, "Regenerarea a eșuat:\n\n" + ex.Message, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Anuleaza()
    {
        var id = IdSelectat();
        if (id is null) { ArataNicioSelectie(); return; }

        var confirmare = MessageBox.Show(this,
            $"Sigur anulați exportul #{id}?\n\n" +
            "Liniile incluse vor deveni disponibile pentru un export nou.\n\n" +
            "ATENȚIE: dacă fișierul a fost deja importat în SAGA, înregistrările trebuie ȘTERSE și acolo manual, " +
            "altfel datele se vor dubla la SAGA.",
            "Confirmare anulare export", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirmare != DialogResult.Yes) return;

        _exportService.Anuleaza(id.Value);
        FileLogger.Info($"Export #{id} anulat.");
        _dupaModificare();
        Reincarca();
    }

    private void DeschideFolder()
    {
        var id = IdSelectat();
        if (id is null) { ArataNicioSelectie(); return; }
        var export = _exporturiRepo.Gaseste(id.Value);
        if (export is null) return;

        if (File.Exists(export.CaleFisier))
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{export.CaleFisier}\"");
        else
            System.Diagnostics.Process.Start("explorer.exe", Core.Data.AppPaths.FilesFolder);
    }

    private void DeschideFisier()
    {
        var id = IdSelectat();
        if (id is null) { ArataNicioSelectie(); return; }
        var export = _exporturiRepo.Gaseste(id.Value);
        if (export is null) return;

        if (!File.Exists(export.CaleFisier))
        {
            MessageBox.Show(this, "Fișierul nu se mai găsește pe disc. Folosiți 'Regenerează fișierul'.", "Fișier lipsă", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(export.CaleFisier) { UseShellExecute = true });
    }

    private void ArataNicioSelectie() =>
        MessageBox.Show(this, "Selectați mai întâi un export din listă.", "Nicio selecție", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
