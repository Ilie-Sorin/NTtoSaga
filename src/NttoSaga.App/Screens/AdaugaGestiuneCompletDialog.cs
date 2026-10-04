namespace NttoSaga.App.Screens;

/// <summary>Dialog complet pentru adăugarea unei gestiuni noi din ecranul Setări.</summary>
public class AdaugaGestiuneCompletDialog : Form
{
    private readonly TextBox _txtDenumire = new();
    private readonly TextBox _txtPrescurtare = new();
    private readonly TextBox _txtCont = new();
    private readonly TextBox _txtActivitate = new();

    public string Denumire => _txtDenumire.Text.Trim();
    public string Prescurtare => _txtPrescurtare.Text.Trim();
    public string ContMarfa => _txtCont.Text.Trim();
    public string Activitate => _txtActivitate.Text.Trim();

    public AdaugaGestiuneCompletDialog()
    {
        Text = "Adăugare gestiune";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 380;
        Height = 260;

        var lay = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 5 };
        lay.Controls.Add(new Label { Text = "Denumire:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _txtDenumire.Dock = DockStyle.Fill;
        lay.Controls.Add(_txtDenumire, 1, 0);
        lay.Controls.Add(new Label { Text = "Prescurtare (prefix NrDocument):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        _txtPrescurtare.Dock = DockStyle.Fill;
        lay.Controls.Add(_txtPrescurtare, 1, 1);
        lay.Controls.Add(new Label { Text = "Cont marfă:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        _txtCont.Dock = DockStyle.Fill;
        lay.Controls.Add(_txtCont, 1, 2);
        lay.Controls.Add(new Label { Text = "Activitate:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 3);
        _txtActivitate.Dock = DockStyle.Fill;
        lay.Controls.Add(_txtActivitate, 1, 3);

        var btnOk = new Button { Text = "Adaugă", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
        var btnCancel = new Button { Text = "Renunță", DialogResult = DialogResult.Cancel, Dock = DockStyle.Fill };
        var panelButoane = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        panelButoane.Controls.Add(btnCancel);
        panelButoane.Controls.Add(btnOk);
        lay.Controls.Add(panelButoane, 0, 4);
        lay.SetColumnSpan(panelButoane, 2);

        Controls.Add(lay);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        btnOk.Click += (s, e) =>
        {
            if (Denumire.Length == 0 || ContMarfa.Length == 0 || Activitate.Length == 0)
            {
                MessageBox.Show(this, "Completați toate câmpurile.", "Date lipsă", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
            }
        };
    }
}
