namespace NttoSaga.App.Screens;

/// <summary>Dialog pentru adăugarea unei cote de TVA noi și a denumirii articolului aferent (§4.3, extensibil).</summary>
public class AdaugaCotaTvaDialog : Form
{
    private readonly TextBox _txtCota = new();
    private readonly TextBox _txtDenArt = new();

    public int Cota => int.TryParse(_txtCota.Text.Trim(), out var v) ? v : 0;
    public string DenArt => _txtDenArt.Text.Trim();

    public AdaugaCotaTvaDialog()
    {
        Text = "Adăugare cotă TVA";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 420;
        Height = 210;

        var lay = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 4 };
        lay.Controls.Add(new Label { Text = "Cotă TVA (%):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _txtCota.Dock = DockStyle.Fill;
        lay.Controls.Add(_txtCota, 1, 0);
        lay.Controls.Add(new Label { Text = "Denumire articol (DEN_ART):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        _txtDenArt.Dock = DockStyle.Fill;
        lay.Controls.Add(_txtDenArt, 1, 1);
        var lblAvertisment = new Label
        {
            Text = "Trebuie să corespundă exact articolului din SAGA.",
            AutoSize = false, Dock = DockStyle.Fill, ForeColor = Color.DarkRed,
        };
        lay.Controls.Add(lblAvertisment, 0, 2);
        lay.SetColumnSpan(lblAvertisment, 2);

        var btnOk = new Button { Text = "Adaugă", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
        var btnCancel = new Button { Text = "Renunță", DialogResult = DialogResult.Cancel, Dock = DockStyle.Fill };
        var panelButoane = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        panelButoane.Controls.Add(btnCancel);
        panelButoane.Controls.Add(btnOk);
        lay.Controls.Add(panelButoane, 0, 3);
        lay.SetColumnSpan(panelButoane, 2);

        Controls.Add(lay);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        btnOk.Click += (s, e) =>
        {
            if (_txtCota.Text.Trim().Length == 0 || DenArt.Length == 0)
            {
                MessageBox.Show(this, "Completați ambele câmpuri.", "Date lipsă", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
            }
        };
    }
}
