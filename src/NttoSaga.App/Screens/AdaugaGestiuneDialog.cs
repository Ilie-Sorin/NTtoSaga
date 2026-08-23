namespace NttoSaga.App.Screens;

/// <summary>Dialog rapid pentru completarea unei gestiuni noi descoperite la import (§6.1).</summary>
public class AdaugaGestiuneDialog : Form
{
    private readonly TextBox _txtCont = new();
    private readonly TextBox _txtActivitate = new();

    public string ContMarfa => _txtCont.Text.Trim();
    public string Activitate => _txtActivitate.Text.Trim();

    public AdaugaGestiuneDialog(string denumire)
    {
        Text = "Adăugare gestiune în nomenclator";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 420;
        Height = 240;

        var lay = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 5 };
        lay.Controls.Add(new Label
        {
            Text = $"Gestiunea '{denumire}' apare în CSV dar nu este în nomenclator.\nCompletați contul de marfă și codul de activitate:",
            AutoSize = false, Height = 50, Dock = DockStyle.Fill,
        }, 0, 0);
        lay.SetColumnSpan(lay.Controls[0], 2);

        lay.Controls.Add(new Label { Text = "Cont marfă (ex. 371.00005):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        _txtCont.Dock = DockStyle.Fill;
        lay.Controls.Add(_txtCont, 1, 1);

        lay.Controls.Add(new Label { Text = "Activitate (ex. 05):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        _txtActivitate.Dock = DockStyle.Fill;
        lay.Controls.Add(_txtActivitate, 1, 2);

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
            if (_txtCont.Text.Trim().Length == 0 || _txtActivitate.Text.Trim().Length == 0)
            {
                MessageBox.Show(this, "Completați ambele câmpuri.", "Date lipsă", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
            }
        };
    }
}
