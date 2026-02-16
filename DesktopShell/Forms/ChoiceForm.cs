namespace DesktopShell;

public partial class ChoiceForm : Form
{
    public ChoiceForm()
    {
        InitializeComponent();
    }

    public void ChoiceForm_Load(object sender, EventArgs e)
    {
        GlobalVar.SetCentered(Screen.FromPoint(Properties.Settings.PositionSave), this);
        filesGroup.Controls.AddRange(GlobalVar.PopulateLabels());

        int fileCount = GlobalVar.FileChoices.Count;
        foreach (Control c in filesGroup.Controls)
        {
            c.Click += new EventHandler(ChoiceForm_Click);
        }

        titleLabel.Left = 350 / 2 - (titleLabel.Width / 2);
        titleLabel.Text = $"{GlobalVar.SearchType} ({fileCount})";
        BackColor = titleLabel.BackColor = Properties.Settings.BackgroundColor;
        ForeColor = titleLabel.ForeColor = Properties.Settings.ForegroundColor;
        ClientSize = new Size(GlobalVar.Width, (fileCount * 18) + 4);
        Location = new Point(Location.X, Location.Y + 20);
    }

    public void ChoiceForm_Click(object? sender, EventArgs e)
    {
        if (sender == null)
        {
            GlobalVar.Log($"### ChoiceForm::ChoiceForm_Click sender = null");
            return;
        }
        Regex extension = new(".([a-z]|[A-Z]){3,4}$");
        foreach (FileInfo f in GlobalVar.FileChoices)
        {
            if (((Label)sender).Text.Contains(extension.Replace(f.Name, ""), StringComparison.CurrentCulture))
            {
                GlobalVar.Run(f.FullName);
                Close();
            }
        }
    }

    public void ChoiceForm_DoubleClick(object sender, EventArgs e)
    {
        Close();
    }

    private void RandomLabel_Click(object sender, EventArgs e)
    {
        Random random = new();
        int randNum = random.Next(0, GlobalVar.FileChoices.Count);

        if (GlobalVar.FileChoices != null && GlobalVar.FileChoices[randNum] != null)
        {
            if (GlobalVar.FileChoices[randNum] is FileInfo fileInfo)
            {
                GlobalVar.Run(fileInfo.FullName);
                Close();
            }
        }
    }
}