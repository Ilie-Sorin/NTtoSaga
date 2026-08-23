using NttoSaga.Core.Backup;
using NttoSaga.Core.Logging;
using NttoSaga.Core.Models;
using NttoSaga.Core.Repositories;

namespace NttoSaga.App.Screens;

public class SetariControl : UserControl, Forms.IEcranNavigabil
{
    private readonly Action _dupaModificare;
    private readonly NomenclatoareRepository _repo = new();

    private readonly DataGridView _gridGestiuni = new();
    private readonly DataGridView _gridTva = new();
    private readonly TextBox _txtCod = new() { Width = 100 };
    private readonly TextBox _txtTip = new() { Width = 50 };
    private readonly TextBox _txtUm = new() { Width = 50 };
    private readonly TextBox _txtLatimeIndex = new() { Width = 50 };
    private readonly TextBox _txtFolderCsv = new() { Width = 400 };
    private readonly TextBox _txtFolderDbf = new() { Width = 400 };
    private readonly Label _lblBackup = new();

    public SetariControl(Action dupaModificare)
    {
        _dupaModificare = dupaModificare;
        Dock = DockStyle.Fill;
        BuildUi();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblTitlu = new Label { Text = "Setări și nomenclatoare", Font = new Font("Segoe UI", 13, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
        root.Controls.Add(lblTitlu, 0, 0);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(TabGestiuni());
        tabs.TabPages.Add(TabTva());
        tabs.TabPages.Add(TabConstante());
        root.Controls.Add(tabs, 0, 1);

        var panelBackup = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        var btnBackup = new Button { Text = "Copie de siguranță a bazei de date", Width = 220 };
        btnBackup.Click += (s, e) =>
        {
            var cale = new BackupService().Copiaza();
            _lblBackup.Text = $"Copiat: {cale}";
            FileLogger.Info($"Copie de siguranță manuală creată: {cale}");
        };
        panelBackup.Controls.Add(btnBackup);
        _lblBackup.AutoSize = true;
        _lblBackup.Margin = new Padding(10, 8, 0, 0);
        panelBackup.Controls.Add(_lblBackup);
        root.Controls.Add(panelBackup, 0, 2);

        Controls.Add(root);
    }

    // ---- Tab Gestiuni ----

    private TabPage TabGestiuni()
    {
        var tab = new TabPage("Gestiuni");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _gridGestiuni.Dock = DockStyle.Fill;
        _gridGestiuni.AutoGenerateColumns = false;
        _gridGestiuni.AllowUserToAddRows = false;
        _gridGestiuni.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _gridGestiuni.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Id", Visible = false });
        _gridGestiuni.Columns.Add(new DataGridViewTextBoxColumn { Name = "Denumire", HeaderText = "Denumire" });
        _gridGestiuni.Columns.Add(new DataGridViewTextBoxColumn { Name = "ContMarfa", HeaderText = "Cont marfă" });
        _gridGestiuni.Columns.Add(new DataGridViewTextBoxColumn { Name = "Activitate", HeaderText = "Activitate" });
        _gridGestiuni.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Activ", HeaderText = "Activ" });
        layout.Controls.Add(_gridGestiuni, 0, 0);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        var btnAdauga = new Button { Text = "Adaugă gestiune nouă", Width = 160 };
        var btnSalveaza = new Button { Text = "Salvează modificările", Width = 160 };
        btnAdauga.Click += (s, e) => AdaugaGestiune();
        btnSalveaza.Click += (s, e) => SalveazaGestiuni();
        toolbar.Controls.Add(btnAdauga);
        toolbar.Controls.Add(btnSalveaza);
        layout.Controls.Add(toolbar, 0, 1);

        tab.Controls.Add(layout);
        return tab;
    }

    private void AdaugaGestiune()
    {
        using var dlg = new AdaugaGestiuneCompletDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _repo.Adauga(dlg.Denumire, dlg.ContMarfa, dlg.Activitate);
            IncarcaGestiuni();
            _dupaModificare();
        }
    }

    private void SalveazaGestiuni()
    {
        foreach (DataGridViewRow row in _gridGestiuni.Rows)
        {
            if (row.IsNewRow) continue;
            var id = Convert.ToInt64(row.Cells["Id"].Value);
            var denumire = row.Cells["Denumire"].Value?.ToString() ?? "";
            var cont = row.Cells["ContMarfa"].Value?.ToString() ?? "";
            var activitate = row.Cells["Activitate"].Value?.ToString() ?? "";
            var activ = row.Cells["Activ"].Value is bool b && b;
            if (denumire.Trim().Length == 0)
            {
                MessageBox.Show(this, "Denumirea gestiunii nu poate fi goală.", "Date nevalide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _repo.Actualizeaza(id, denumire, cont, activitate, activ);
        }
        FileLogger.Info("Nomenclator gestiuni actualizat din ecranul Setări.");
        MessageBox.Show(this, "Modificările au fost salvate.", "Salvat", MessageBoxButtons.OK, MessageBoxIcon.Information);
        IncarcaGestiuni();
        _dupaModificare();
    }

    private void IncarcaGestiuni()
    {
        _gridGestiuni.Rows.Clear();
        foreach (var g in _repo.ListeazaGestiuni())
            _gridGestiuni.Rows.Add(g.Id, g.Denumire, g.ContMarfa, g.Activitate, g.Activ);
    }

    // ---- Tab Denumiri TVA ----

    private TabPage TabTva()
    {
        var tab = new TabPage("Denumiri articole pe cotă TVA");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblAvertisment = new Label
        {
            Text = "Atenție: denumirea trebuie să corespundă EXACT articolului din nomenclatorul SAGA " +
                   "(inclusiv majuscule și spații). O diferență de un caracter duce la eșecul importului sau la un articol duplicat.",
            AutoSize = false, Height = 40, Dock = DockStyle.Fill, ForeColor = Color.DarkRed,
        };
        layout.Controls.Add(lblAvertisment, 0, 0);

        _gridTva.Dock = DockStyle.Fill;
        _gridTva.AutoGenerateColumns = false;
        _gridTva.AllowUserToAddRows = false;
        _gridTva.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _gridTva.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Id", Visible = false });
        _gridTva.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cota", HeaderText = "Cotă TVA (%)" });
        _gridTva.Columns.Add(new DataGridViewTextBoxColumn { Name = "DenArt", HeaderText = "Denumire articol (DEN_ART)" });
        _gridTva.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Activ", HeaderText = "Activ" });
        layout.Controls.Add(_gridTva, 0, 1);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        var btnAdauga = new Button { Text = "Adaugă cotă nouă", Width = 140 };
        var btnSalveaza = new Button { Text = "Salvează modificările", Width = 160 };
        btnAdauga.Click += (s, e) => AdaugaCotaTva();
        btnSalveaza.Click += (s, e) => SalveazaTva();
        toolbar.Controls.Add(btnAdauga);
        toolbar.Controls.Add(btnSalveaza);
        layout.Controls.Add(toolbar, 0, 2);

        tab.Controls.Add(layout);
        return tab;
    }

    private void AdaugaCotaTva()
    {
        using var dlg = new AdaugaCotaTvaDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _repo.AdaugaDenumireTva(dlg.Cota, dlg.DenArt);
            IncarcaTva();
            _dupaModificare();
        }
    }

    private void SalveazaTva()
    {
        foreach (DataGridViewRow row in _gridTva.Rows)
        {
            if (row.IsNewRow) continue;
            var id = Convert.ToInt64(row.Cells["Id"].Value);
            if (!int.TryParse(row.Cells["Cota"].Value?.ToString(), out var cota))
            {
                MessageBox.Show(this, "Cota TVA trebuie să fie un număr întreg.", "Date nevalide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var denArt = row.Cells["DenArt"].Value?.ToString() ?? "";
            var activ = row.Cells["Activ"].Value is bool b && b;
            _repo.ActualizeazaDenumireTva(id, cota, denArt, activ);
        }
        FileLogger.Info("Denumiri articole pe cotă TVA actualizate din ecranul Setări.");
        MessageBox.Show(this, "Modificările au fost salvate.", "Salvat", MessageBoxButtons.OK, MessageBoxIcon.Information);
        IncarcaTva();
        _dupaModificare();
    }

    private void IncarcaTva()
    {
        _gridTva.Rows.Clear();
        foreach (var d in _repo.ListeazaDenumiriTva())
            _gridTva.Rows.Add(d.Id, d.Cota, d.DenArt, d.Activ);
    }

    // ---- Tab Constante ----

    private TabPage TabConstante()
    {
        var tab = new TabPage("Constante și foldere");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(10) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        void AdaugaRand(string eticheta, Control control, int rand)
        {
            layout.Controls.Add(new Label { Text = eticheta, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 8, 8, 0) }, 0, rand);
            layout.Controls.Add(control, 1, rand);
        }

        AdaugaRand("COD (constant DBF):", _txtCod, 0);
        AdaugaRand("TIP (constant DBF):", _txtTip, 1);
        AdaugaRand("UM (constant DBF):", _txtUm, 2);
        AdaugaRand("Lățime index export:", _txtLatimeIndex, 3);
        AdaugaRand("Folder implicit CSV:", _txtFolderCsv, 4);
        AdaugaRand("Folder implicit DBF:", _txtFolderDbf, 5);

        var btnSalveaza = new Button { Text = "Salvează setările", Width = 150 };
        btnSalveaza.Click += (s, e) => SalveazaConstante();
        layout.Controls.Add(btnSalveaza, 1, 6);

        tab.Controls.Add(layout);
        return tab;
    }

    private void SalveazaConstante()
    {
        _repo.SalveazaSetare("COD", _txtCod.Text.Trim());
        _repo.SalveazaSetare("TIP", _txtTip.Text.Trim());
        _repo.SalveazaSetare("UM", _txtUm.Text.Trim());
        _repo.SalveazaSetare("latime_index", _txtLatimeIndex.Text.Trim());
        _repo.SalveazaSetare("folder_csv_implicit", _txtFolderCsv.Text.Trim());
        _repo.SalveazaSetare("folder_dbf_implicit", _txtFolderDbf.Text.Trim());
        FileLogger.Info("Setări constante actualizate din ecranul Setări.");
        MessageBox.Show(this, "Setările au fost salvate.", "Salvat", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void IncarcaConstante()
    {
        var setari = _repo.ListeazaSetari();
        _txtCod.Text = setari.GetValueOrDefault("COD", "20100");
        _txtTip.Text = setari.GetValueOrDefault("TIP", "A");
        _txtUm.Text = setari.GetValueOrDefault("UM", "BUC");
        _txtLatimeIndex.Text = setari.GetValueOrDefault("latime_index", "4");
        _txtFolderCsv.Text = setari.GetValueOrDefault("folder_csv_implicit", "");
        _txtFolderDbf.Text = setari.GetValueOrDefault("folder_dbf_implicit", "");
    }

    public void LaAfisare() => Reincarca();

    public void Reincarca()
    {
        IncarcaGestiuni();
        IncarcaTva();
        IncarcaConstante();
    }
}
