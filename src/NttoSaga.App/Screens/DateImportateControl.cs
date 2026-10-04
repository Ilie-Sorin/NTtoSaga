using NttoSaga.Core.Logging;
using NttoSaga.Core.Repositories;

namespace NttoSaga.App.Screens;

public class DateImportateControl : UserControl, Forms.IEcranNavigabil
{
    private readonly LiniiImportRepository _liniiRepo = new();
    private readonly NomenclatoareRepository _nomenclatoare = new();

    private readonly ComboBox _cmbGestiune = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    private readonly ComboBox _cmbStare = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    private readonly ComboBox _cmbCota = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    private readonly CheckBox _chkInterval = new() { Text = "Filtrează interval", AutoSize = true };
    private readonly DateTimePicker _dtStart = new() { Width = 110, Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _dtSfarsit = new() { Width = 110, Format = DateTimePickerFormat.Short };
    private readonly TextBox _txtCautare = new() { Width = 140, PlaceholderText = "Nr. document…" };
    private readonly Button _btnFiltreaza = new() { Text = "Filtrează", Width = 90 };
    private readonly Button _btnSelecteazaTot = new() { Text = "Selectează tot", Width = 110 };
    private readonly Button _btnSterge = new() { Text = "Șterge înregistrările selectate", Width = 200 };

    private readonly DataGridView _grid = new();
    private readonly Label _lblTotaluri = new();

    public DateImportateControl()
    {
        Dock = DockStyle.Fill;
        BuildUi();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblTitlu = new Label { Text = "Date importate", Font = new Font("Segoe UI", 13, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
        root.Controls.Add(lblTitlu, 0, 0);

        var filtre = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        filtre.Controls.Add(new Label { Text = "Gestiune:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
        filtre.Controls.Add(_cmbGestiune);
        filtre.Controls.Add(new Label { Text = "Stare:", AutoSize = true, Margin = new Padding(10, 8, 4, 0) });
        filtre.Controls.Add(_cmbStare);
        filtre.Controls.Add(new Label { Text = "Cotă TVA:", AutoSize = true, Margin = new Padding(10, 8, 4, 0) });
        filtre.Controls.Add(_cmbCota);
        filtre.Controls.Add(_chkInterval);
        _chkInterval.Margin = new Padding(10, 8, 4, 0);
        filtre.Controls.Add(_dtStart);
        filtre.Controls.Add(new Label { Text = "–", AutoSize = true, Margin = new Padding(4, 8, 4, 0) });
        filtre.Controls.Add(_dtSfarsit);
        filtre.Controls.Add(_txtCautare);
        _txtCautare.Margin = new Padding(10, 4, 4, 0);
        filtre.Controls.Add(_btnFiltreaza);
        _btnFiltreaza.Margin = new Padding(10, 4, 4, 0);
        filtre.Controls.Add(_btnSelecteazaTot);
        _btnSelecteazaTot.Margin = new Padding(20, 4, 4, 0);
        filtre.Controls.Add(_btnSterge);
        _btnSterge.Margin = new Padding(4, 4, 4, 0);
        root.Controls.Add(filtre, 0, 1);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = true;
        root.Controls.Add(_grid, 0, 2);

        _lblTotaluri.Dock = DockStyle.Fill;
        _lblTotaluri.Height = 28;
        _lblTotaluri.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        root.Controls.Add(_lblTotaluri, 0, 3);

        Controls.Add(root);

        _btnFiltreaza.Click += (s, e) => Filtreaza();
        _btnSelecteazaTot.Click += (s, e) => _grid.SelectAll();
        _btnSterge.Click += (s, e) => StergeSelectia();
        _txtCautare.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Filtreaza(); };
    }

    public void LaAfisare() => Reincarca();

    public void Reincarca()
    {
        var gestiuneSelectata = _cmbGestiune.SelectedItem as string;
        _cmbGestiune.Items.Clear();
        _cmbGestiune.Items.Add("(toate)");
        foreach (var g in _nomenclatoare.ListeazaGestiuni())
            _cmbGestiune.Items.Add(g.Denumire);
        _cmbGestiune.SelectedItem = gestiuneSelectata is not null && _cmbGestiune.Items.Contains(gestiuneSelectata)
            ? gestiuneSelectata : "(toate)";

        var cotaSelectata = _cmbCota.SelectedItem as string;
        _cmbCota.Items.Clear();
        _cmbCota.Items.Add("(toate)");
        foreach (var t in _nomenclatoare.ListeazaDenumiriTva().Select(d => d.Cota).Distinct().OrderBy(c => c))
            _cmbCota.Items.Add(t.ToString());
        _cmbCota.SelectedItem = cotaSelectata is not null && _cmbCota.Items.Contains(cotaSelectata) ? cotaSelectata : "(toate)";

        if (_cmbStare.Items.Count == 0)
        {
            _cmbStare.Items.AddRange(["(toate)", "Exportat", "Neexportat"]);
            _cmbStare.SelectedIndex = 0;
        }

        Filtreaza();
    }

    private void Filtreaza()
    {
        IEnumerable<string>? gestiuni = _cmbGestiune.SelectedItem as string is { } g && g != "(toate)" ? [g] : null;
        bool? doarNeexportate = (_cmbStare.SelectedItem as string) switch
        {
            "Exportat" => false,
            "Neexportat" => true,
            _ => null,
        };
        int? cota = (_cmbCota.SelectedItem as string) is { } c && c != "(toate)" ? int.Parse(c) : null;
        string? dataStart = _chkInterval.Checked ? _dtStart.Value.ToString("yyyy-MM-dd") : null;
        string? dataSfarsit = _chkInterval.Checked ? _dtSfarsit.Value.ToString("yyyy-MM-dd") : null;

        var linii = _liniiRepo.Cauta(gestiuni, dataStart, dataSfarsit, doarNeexportate, cota, _txtCautare.Text);
        var gestiuniNomenclator = _nomenclatoare.ListeazaGestiuni();

        _grid.DataSource = linii.Select(l => new
        {
            l.Id,
            Gestiune_sursa = l.Name,
            Gestiune_destinatie = l.PartnerName,
            NrDocument = $"{NomenclatoareRepository.Gaseste(gestiuniNomenclator, l.Name)?.Prescurtare}{l.DocNumber}",
            Data = l.DocDate,
            Cota_TVA = l.RetailVatPercent,
            ValoareVanzare = l.ValAmIesire / 100m,
            TVA = l.TvaCalculata / 100m,
            ValoareAchizitie = l.ValAchizitieFaraTVAIesire / 100m,
            Export = l.IdExport?.ToString() ?? "neexportat",
        }).ToList();

        if (_grid.Columns["Id"] is { } colId) colId.Visible = false;

        _lblTotaluri.Text = $"{linii.Count} linii · Total valoare vânzare: {linii.Sum(l => l.ValAmIesire) / 100m:N2} lei · " +
                             $"Total TVA: {linii.Sum(l => l.TvaCalculata) / 100m:N2} lei";
    }

    private void StergeSelectia()
    {
        var linii = _grid.SelectedRows.Cast<DataGridViewRow>()
            .Where(r => !r.IsNewRow)
            .Select(r => (Id: (long)r.Cells["Id"].Value, Export: r.Cells["Export"].Value?.ToString()))
            .ToList();
        if (linii.Count == 0)
        {
            MessageBox.Show(this, "Selectați cel puțin o linie din tabel.", "Nicio selecție", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var nrExportate = linii.Count(l => l.Export != "neexportat");

        string mesaj;
        if (linii.Count == 1)
        {
            mesaj = nrExportate == 1
                ? $"Linia selectată a fost deja EXPORTATĂ (exportul #{linii[0].Export}).\n\n" +
                  "Ștergerea NU modifică fișierul DBF deja generat — dacă acel fișier a fost deja importat în SAGA, " +
                  "corectarea de acolo trebuie făcută manual.\n\nȘtergeți definitiv linia selectată?"
                : "Ștergeți definitiv linia selectată?";
        }
        else
        {
            mesaj = nrExportate > 0
                ? $"Ați selectat {linii.Count} linii, dintre care {nrExportate} au fost deja EXPORTATE.\n\n" +
                  "Ștergerea NU modifică fișierele DBF deja generate — dacă acele fișiere au fost deja importate în SAGA, " +
                  $"corectarea de acolo trebuie făcută manual.\n\nȘtergeți definitiv cele {linii.Count} linii selectate?"
                : $"Ștergeți definitiv cele {linii.Count} linii selectate?";
        }

        var confirmare = MessageBox.Show(this, mesaj, "Confirmare ștergere",
            MessageBoxButtons.YesNo, nrExportate > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Question);
        if (confirmare != DialogResult.Yes) return;

        foreach (var l in linii)
        {
            if (_liniiRepo.Sterge(l.Id))
            {
                FileLogger.Info($"Linie import #{l.Id} ștearsă manual din ecranul Date importate" +
                                 (l.Export != "neexportat" ? $" (era exportată în exportul #{l.Export})." : "."));
            }
        }

        Filtreaza();
    }
}
