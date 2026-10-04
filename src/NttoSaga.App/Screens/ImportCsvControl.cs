using NttoSaga.Core.Data;
using NttoSaga.Core.Import;
using NttoSaga.Core.Logging;
using NttoSaga.Core.Repositories;
using NttoSaga.Core.Backup;

namespace NttoSaga.App.Screens;

public class ImportCsvControl : UserControl, Forms.IEcranNavigabil
{
    private readonly Action _dupaImport;
    private readonly NomenclatoareRepository _nomenclatoare = new();
    private readonly CsvImporter _importer = new();

    private readonly TextBox _txtCale = new() { ReadOnly = true, Dock = DockStyle.Fill, Text = "(alegeți sau trageți aici fișierul CSV)" };
    private readonly Button _btnAlege = new() { Text = "Alege fișier…", Width = 130 };

    private readonly DataGridView _grid = new();
    private readonly ListBox _listValidari = new();
    private readonly FlowLayoutPanel _panelGestiuniLipsa = new();
    private readonly Label _lblRezumat = new();
    private readonly Button _btnImporta = new() { Text = "Importă", Width = 120, Enabled = false };

    private string? _caleCurenta;
    private ValidareCsvResult? _validare;

    public ImportCsvControl(Action dupaImport)
    {
        _dupaImport = dupaImport;
        Dock = DockStyle.Fill;
        AllowDrop = true;
        BuildUi();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblTitlu = new Label { Text = "Import CSV", Font = new Font("Segoe UI", 13, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
        root.Controls.Add(lblTitlu, 0, 0);

        var panelSus = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        panelSus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panelSus.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panelSus.Controls.Add(_txtCale, 0, 0);
        panelSus.Controls.Add(_btnAlege, 1, 0);
        panelSus.Height = 30;
        root.Controls.Add(panelSus, 0, 1);

        var panelPreview = new GroupBox { Text = "Previzualizare (primele 20 de linii recunoscute)", Dock = DockStyle.Fill };
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        panelPreview.Controls.Add(_grid);
        root.Controls.Add(panelPreview, 0, 2);

        var panelValidare = new GroupBox { Text = "Validare", Dock = DockStyle.Fill };
        var layoutValidare = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        layoutValidare.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        layoutValidare.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        _listValidari.Dock = DockStyle.Fill;
        _panelGestiuniLipsa.Dock = DockStyle.Fill;
        _panelGestiuniLipsa.FlowDirection = FlowDirection.LeftToRight;
        _panelGestiuniLipsa.WrapContents = true;
        _panelGestiuniLipsa.AutoScroll = true;
        layoutValidare.Controls.Add(_listValidari, 0, 0);
        layoutValidare.Controls.Add(_panelGestiuniLipsa, 0, 1);
        panelValidare.Controls.Add(layoutValidare);
        root.Controls.Add(panelValidare, 0, 3);

        var panelJos = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        panelJos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panelJos.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _lblRezumat.Dock = DockStyle.Fill;
        _lblRezumat.TextAlign = ContentAlignment.MiddleLeft;
        panelJos.Controls.Add(_lblRezumat, 0, 0);
        panelJos.Controls.Add(_btnImporta, 1, 0);
        panelJos.Height = 40;
        root.Controls.Add(panelJos, 0, 4);

        Controls.Add(root);

        _btnAlege.Click += (s, e) => AlegeFisier();
        _btnImporta.Click += (s, e) => Importa();

        AllowDrop = true;
        DragEnter += (s, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        };
        DragDrop += (s, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] fisiere && fisiere.Length > 0)
                IncarcaFisier(fisiere[0]);
        };
    }

    public void LaAfisare() { }
    public void Reincarca() { }

    private void AlegeFisier()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Fișiere CSV (*.csv)|*.csv|Toate fișierele (*.*)|*.*",
            InitialDirectory = _nomenclatoare.ListeazaSetari().GetValueOrDefault("folder_csv_implicit", AppPaths.FilesFolder),
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            IncarcaFisier(dlg.FileName);
    }

    private void IncarcaFisier(string cale)
    {
        _caleCurenta = cale;
        _txtCale.Text = cale;
        _lblRezumat.Text = "";
        ValideazaFisierCurent();
    }

    private void ValideazaFisierCurent()
    {
        if (_caleCurenta is null) return;

        try
        {
            var gestiuni = _nomenclatoare.ListeazaGestiuni();
            _validare = _importer.Valideaza(_caleCurenta, gestiuni);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Nu am putut citi fișierul CSV:\n\n" + ex.Message, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _validare = null;
            _btnImporta.Enabled = false;
            return;
        }

        AfiseazaValidare(_validare);
    }

    private void AfiseazaValidare(ValidareCsvResult v)
    {
        _grid.DataSource = v.Previzualizare(20).Select(r => new
        {
            Gestiune_sursa = r.Name,
            Gestiune_destinatie = r.PartnerName,
            NrDocument = r.DocNumber,
            Data = r.DocDateAfisare,
            Cota_TVA = r.RetailVatPercent,
            ValoareVanzare = r.ValAmIesireBani / 100m,
            TVA = Core.Models.TvaCalculator.CalculeazaTvaBani(r.RetailVatPercent, r.ValAchizitieFaraTVAIesireBani) / 100m,
            ValoareAchizitie = r.ValAchizitieFaraTVAIesireBani / 100m,
        }).ToList();

        _listValidari.Items.Clear();
        if (v.ColoaneLipsa.Count > 0)
            _listValidari.Items.Add("Coloane lipsă în fișier: " + string.Join(", ", v.ColoaneLipsa));
        foreach (var (rand, motiv) in v.RanduriInvalide.Take(50))
            _listValidari.Items.Add($"Rândul {rand}: {motiv}");
        if (v.RanduriInvalide.Count > 50)
            _listValidari.Items.Add($"… și încă {v.RanduriInvalide.Count - 50} rânduri cu erori.");
        if (v.Valid)
            _listValidari.Items.Add($"Validare reușită: {v.RanduriValide.Count} linii recunoscute.");

        _panelGestiuniLipsa.Controls.Clear();
        foreach (var denumire in v.GestiuniNecunoscute)
        {
            var btn = new Button { Text = $"Adaugă '{denumire}' în nomenclator", AutoSize = true, Margin = new Padding(4) };
            btn.Click += (s, e) => AdaugaGestiuneLipsa(denumire);
            _panelGestiuniLipsa.Controls.Add(btn);
        }

        _btnImporta.Enabled = v.Valid;
    }

    private void AdaugaGestiuneLipsa(string denumire)
    {
        using var dlg = new AdaugaGestiuneDialog(denumire);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _nomenclatoare.Adauga(denumire, dlg.ContMarfa, dlg.Activitate);
            ValideazaFisierCurent();
        }
    }

    private void Importa()
    {
        if (_caleCurenta is null || _validare is null || !_validare.Valid) return;

        try
        {
            new BackupService().Copiaza();
            var rezultat = _importer.Importa(_caleCurenta);
            FileLogger.Info($"Import CSV '{_caleCurenta}': {rezultat.Inserate} inserate, {rezultat.IgnorateIdentice} ignorate identice, " +
                             $"{rezultat.Actualizate} actualizate, {rezultat.Conflicte.Count} conflicte, {rezultat.IgnorateValoareZero} ignorate (valoare 0).");

            _lblRezumat.Text = $"Import finalizat: {rezultat.Inserate} inserate · {rezultat.IgnorateIdentice} ignorate (identice) · " +
                                $"{rezultat.Actualizate} actualizate · {rezultat.Conflicte.Count} conflicte · {rezultat.IgnorateValoareZero} ignorate (valoare 0).";

            if (rezultat.Conflicte.Count > 0)
            {
                var mesaj = "Atenție: există linii deja exportate cu valori diferite în CSV. Acestea NU au fost modificate " +
                            "(datele au fost deja predate în SAGA). Corectați manual în SAGA dacă e nevoie:\n\n" +
                            string.Join("\n", rezultat.Conflicte.Take(10).Select(c =>
                                $"- {c.Existent.Name} / NT {c.Existent.DocNumber} / {c.Existent.DocDate} / cota {c.Existent.RetailVatPercent}%: " +
                                $"bază={c.Existent.ValAmIesire / 100m}, CSV={c.DinCsv.ValAmIesireBani / 100m}"));
                MessageBox.Show(this, mesaj, "Conflicte la import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show(this, _lblRezumat.Text, "Import finalizat", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            _dupaImport();
            ValideazaFisierCurent();
        }
        catch (ImportCsvException ex)
        {
            MessageBox.Show(this, ex.Message, "Import respins", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            FileLogger.Eroare("Import CSV eșuat", ex);
            MessageBox.Show(this, "Importul a eșuat:\n\n" + ex.Message, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
