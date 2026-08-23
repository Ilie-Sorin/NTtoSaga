using NttoSaga.App.Screens;
using NttoSaga.Core.Repositories;

namespace NttoSaga.App.Forms;

public class MainForm : Form
{
    private readonly Panel _sidebar = new();
    private readonly Panel _content = new();
    private readonly Label _lblNeexportate = new();
    private readonly LiniiImportRepository _liniiRepo = new();

    private readonly Dictionary<string, Control> _ecrane = new();
    private readonly Dictionary<string, Button> _butoane = new();
    private string _ecranCurent = "";

    public MainForm()
    {
        Text = "NT to SAGA";
        Width = 1200;
        Height = 780;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 650);

        _sidebar.Dock = DockStyle.Left;
        _sidebar.Width = 220;
        _sidebar.BackColor = Color.FromArgb(37, 44, 58);
        Controls.Add(_sidebar);

        _content.Dock = DockStyle.Fill;
        _content.Padding = new Padding(12);
        Controls.Add(_content);
        _content.BringToFront();

        var lblTitlu = new Label
        {
            Text = "NT to SAGA",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 56,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        _sidebar.Controls.Add(lblTitlu);

        _lblNeexportate.Dock = DockStyle.Bottom;
        _lblNeexportate.Height = 70;
        _lblNeexportate.ForeColor = Color.White;
        _lblNeexportate.BackColor = Color.FromArgb(52, 61, 80);
        _lblNeexportate.TextAlign = ContentAlignment.MiddleCenter;
        _lblNeexportate.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _sidebar.Controls.Add(_lblNeexportate);

        var importCsv = new ImportCsvControl(ReincarcaTot);
        var dateImportate = new DateImportateControl();
        var exportDbf = new ExportDbfControl(ReincarcaTot);
        var istoricExporturi = new IstoricExporturiControl(ReincarcaTot);
        var setari = new SetariControl(ReincarcaTot);

        AdaugaEcran("import", "Import CSV", importCsv);
        AdaugaEcran("date", "Date importate", dateImportate);
        AdaugaEcran("export", "Export DBF", exportDbf);
        AdaugaEcran("istoric", "Istoric exporturi", istoricExporturi);
        AdaugaEcran("setari", "Setări și nomenclatoare", setari);

        // Butoanele se adaugă în ordine inversă fiindcă fiecare e Dock=Top (ultimul adăugat apare primul).
        foreach (var cheie in new[] { "setari", "istoric", "export", "date", "import" })
            _sidebar.Controls.Add(_butoane[cheie]);
        _sidebar.Controls.SetChildIndex(lblTitlu, _sidebar.Controls.Count - 1);

        AratatEcran("import");
        ReincarcaTot();
    }

    private void AdaugaEcran(string cheie, string text, Control ecran)
    {
        ecran.Dock = DockStyle.Fill;
        ecran.Visible = false;
        _content.Controls.Add(ecran);
        _ecrane[cheie] = ecran;

        var buton = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 52,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 0, 0),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(37, 44, 58),
            Font = new Font("Segoe UI", 10),
        };
        buton.FlatAppearance.BorderSize = 0;
        buton.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 70, 90);
        buton.Click += (s, e) => AratatEcran(cheie);
        _butoane[cheie] = buton;
    }

    private void AratatEcran(string cheie)
    {
        if (_ecranCurent == cheie) return;
        if (_ecranCurent != "" && _ecrane.TryGetValue(_ecranCurent, out var vechi))
            vechi.Visible = false;

        _ecranCurent = cheie;
        var nou = _ecrane[cheie];
        nou.Visible = true;
        nou.BringToFront();

        foreach (var (k, b) in _butoane)
            b.BackColor = k == cheie ? Color.FromArgb(70, 110, 160) : Color.FromArgb(37, 44, 58);

        if (nou is IEcranNavigabil navigabil)
            navigabil.LaAfisare();
    }

    private void ReincarcaTot()
    {
        ActualizeazaContorNeexportate();
        foreach (var ecran in _ecrane.Values)
            if (ecran is IEcranNavigabil navigabil)
                navigabil.Reincarca();
    }

    private void ActualizeazaContorNeexportate()
    {
        var n = _liniiRepo.NumaraTotalNeexportate();
        _lblNeexportate.Text = n switch
        {
            0 => "Nicio linie\nneexportată",
            1 => "1 linie\nneexportată",
            _ => $"{n} linii\nneexportate",
        };
    }
}

/// <summary>Ecranele care trebuie să știe când sunt afișate sau când datele s-au schimbat în altă parte.</summary>
public interface IEcranNavigabil
{
    void LaAfisare();
    void Reincarca();
}
