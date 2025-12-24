namespace DesktopShell;

public partial class ChoiceForm : Form
{
    public ChoiceForm()
    {
        InitializeComponent();
    }

    public void ChoiceForm_Load(object sender, EventArgs e)
    {
        GlobalVar.SetCentered(Screen.FromPoint(Properties.Settings.positionSave), this);
        filesGroup.Controls.AddRange(GlobalVar.PopulateLabels());

        int fileCount = GlobalVar.fileChoices.Count;
        foreach (Control c in filesGroup.Controls)
        {
            c.Click += new EventHandler(ChoiceForm_Click);
        }

        titleLabel.Left = (350 / 2 - (titleLabel.Width / 2));
        titleLabel.Text = $"{GlobalVar.searchType} ({fileCount})";
        BackColor = titleLabel.BackColor = Properties.Settings.backgroundColor;
        ForeColor = titleLabel.ForeColor = Properties.Settings.foregroundColor;
        ClientSize = new System.Drawing.Size(GlobalVar.width, (fileCount * 18) + 4);
        Location = new System.Drawing.Point(Location.X, Location.Y + 20);
    }

    public void ChoiceForm_Click(object? sender, EventArgs e)
    {
        if (sender == null)
        {
            GlobalVar.Log($"### ChoiceForm::ChoiceForm_Click sender = null");
            return;
        }
        Regex extension = new(".([a-z]|[A-Z]){3,4}$");
        foreach (FileInfo f in GlobalVar.fileChoices)
        {
            if (((Label)sender).Text.Contains(extension.Replace(f.Name, ""), StringComparison.CurrentCulture))
            {
                if (GlobalVar.searchType == "Movie")
                {
                    GlobalVar.Run(@"D:\Program Files (x86)\VLC Media Player\vlc.exe", f.FullName);
                }
                else
                {
                    GlobalVar.Run(f.FullName);
                }

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
        int randNum = random.Next(0, GlobalVar.fileChoices.Count);

        if (GlobalVar.fileChoices != null && GlobalVar.fileChoices[randNum] != null)
        {
            if (GlobalVar.fileChoices[randNum] is FileInfo fileInfo)
            {
                string fileName = fileInfo.FullName;
                if (GlobalVar.searchType == "Movie")
                {
                    GlobalVar.Run(@"D:\Program Files (x86)\VLC Media Player\vlc.exe", fileName);
                }
                else
                {
                    GlobalVar.Run(fileName);
                }
                Close();
            }
        }
    }
}