using System.Reflection;
using System.Threading;

namespace DesktopShell;

public partial class ConfigForm : Form
{
    #region Declarations
    private readonly Regex hexCheck = new("^(#)?([a-fA-F0-9]){6}$");
    private string? checkString;
    private Thread? t = null;
    #endregion

    #region ConfigForm Constructor / Startup
    public ConfigForm() { InitializeComponent(); }
    #endregion

    #region Form Event Handlers
    private void ConfigForm_Load(object sender, EventArgs e)
    {
        // create checkboxes for each monitor
        List<CheckBox> screenCheckBoxList = [];
        for (int i = 0; i < Screen.AllScreens.Length; i++)
        {
            CheckBox tempBox = new()
            {
                AutoSize = true,
                CheckAlign = ContentAlignment.MiddleRight
            };
            if (i < Properties.Settings.multiscreenEnabled.Count)
            {
                if (Properties.Settings.multiscreenEnabled[i])
                {
                    tempBox.CheckState = CheckState.Checked;
                }
                else
                {
                    tempBox.CheckState = CheckState.Unchecked;
                }
            }
            tempBox.Location = new Point(130 + (50 * i), 90);
            tempBox.Name = $"multiScreenCheckbox{i + 1}";
            tempBox.Size = new Size(50, 17);
            tempBox.TabIndex = 3 + i;
            tempBox.Text = $"{i + 1}:";
            tempBox.UseVisualStyleBackColor = true;
            tempBox.Click += TempBox_Click;
            UpdateColorTextBoxes();

            screenCheckBoxList.Add(tempBox);
        }
        foreach (CheckBox c in screenCheckBoxList)
        {
            interfaceBox.Controls.Add(c);
        }

        //checkbox initialization
        if (Properties.Settings.hourlyChimeChecked)
        {
            hourlyChimeCheckbox.CheckState = CheckState.Checked;
        }
        else
        {
            hourlyChimeCheckbox.CheckState = CheckState.Unchecked;
        }

        textColorInputBox.Text = ColorTranslator.ToHtml(Properties.Settings.foregroundColor);           //foreground color initialization            
        backgroundColorInputBox.Text = ColorTranslator.ToHtml(Properties.Settings.backgroundColor);     //background color initialization           
        Location = Cursor.Position;                                                                     //set initial position
    }

    void TempBox_Click(object sender, EventArgs e)
    {
        string whichCheckBox = ((CheckBox)sender).Name.Replace("multiScreenCheckbox", "");
        int checkBoxIdx = Convert.ToInt32(whichCheckBox) - 1;
        Properties.Settings.multiscreenEnabled[checkBoxIdx] = !Properties.Settings.multiscreenEnabled[checkBoxIdx];

        if (GlobalVar.shellInstance != null)
        {
            GlobalVar.InitDropDownRects(GlobalVar.shellInstance);
        }
    }

    private void ConfigForm_FormClosed(object sender, EventArgs e)
    {
        //t.Join();
        //GlobalVar.colorWheelInstance.Close();
        if (t != null)
        {
            //t.Abort();
        }

        GlobalVar.screenSelectorInstance = null;
        GlobalVar.configInstance = null;
    }

    private void CheckKeys_textColor(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true; //prevents beep
            checkString = textColorInputBox.Text;
            if (hexCheck.IsMatch(checkString))
            {
                MessageBox.Show("Font Color Changed");
                Properties.Settings.foregroundColor = ColorTranslator.FromHtml(checkString);    //change setting
                GlobalVar.shellInstance?.ChangeFontColor();                                     //change in program
                Properties.Settings.WriteSettings();
            }
            else
            {
                MessageBox.Show("Incorrect Format: (#123456)");
            }
        }
    }

    private void CheckKeys_backgroundColor(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true; //prevents beep
            checkString = backgroundColorInputBox.Text;
            if (hexCheck.IsMatch(checkString))
            {
                MessageBox.Show("Background Color Changed");
                Properties.Settings.backgroundColor = ColorTranslator.FromHtml(checkString);//Changing in settings                   
                GlobalVar.shellInstance?.ChangeBackgroundColor();                           //Changing in program
                Properties.Settings.WriteSettings();
            }
            else
            {
                MessageBox.Show("Incorrect Format: (#123456)");
            }
        }
    }

    private void HourlyChimeCheckbox_CheckedChanged(object sender, EventArgs e)
    {
        if (GlobalVar.hourlyChime != null)
        {
            GlobalVar.hourlyChime.Enabled = Properties.Settings.hourlyChimeChecked = !GlobalVar.hourlyChime.Enabled;
            Properties.Settings.WriteSettings();
        }
    }

    private void ScreenSelectorButton_Click(object sender, EventArgs e)
    {
        t = new(start: new ThreadStart(ScreenSelectorProc));
        t.IsBackground = true;
        t.Start();
    }

    private void ColorWheel_Click()
    {
        if (GlobalVar.colorWheelInstance == null)
        {
            ThreadStart starter = ColorWheelProc;
            starter += () =>
            {
                UpdateColorTextBoxes();
                if (t != null)
                {
                    t.Join();
                }
            };
            t = new(starter);
            t.IsBackground = true;
            t.Start();
        }
        else
        {
            GlobalVar.Log($"### ConfigForm::ColorWheel_Click() - Colorwheel already opened");
        }
    }

    private delegate void SetControlPropertyThreadSafeDelegate(Control control, string propertyName, object propertyValue);

    public static void SetControlPropertyThreadSafe(Control control, string propertyName, object propertyValue)
    {
        if (control.InvokeRequired)
        {
            _ = control.Invoke(new SetControlPropertyThreadSafeDelegate(SetControlPropertyThreadSafe),
                        args: new object[] { control, propertyName, propertyValue });
        }
        else
        {
            _ = control.GetType().InvokeMember(propertyName, BindingFlags.SetProperty, null, control, new object[] { propertyValue });
        }
    }

    private void UpdateColorTextBoxes()
    {
        SetControlPropertyThreadSafe(control: backgroundColorInputBox,
                                     propertyName: "Text",
                                     propertyValue: ColorTranslator.ToHtml(GlobalVar.backColor));
        SetControlPropertyThreadSafe(control: textColorInputBox,
                                     propertyName: "Text",
                                     propertyValue: ColorTranslator.ToHtml(GlobalVar.fontColor));
        SetControlPropertyThreadSafe(control: BackColorExample,
                                     propertyName: "BackColor",
                                     propertyValue: GlobalVar.backColor);
        SetControlPropertyThreadSafe(control: ForeColorExample,
                                     propertyName: "BackColor",
                                     propertyValue: GlobalVar.fontColor);
        Properties.Settings.WriteSettings();
    }

    private void BackColorWheel_Click(object sender, EventArgs e)
    {
        GlobalVar.settingBackColor = true;
        ColorWheel_Click();
    }

    private void ForeColorWheel_Click(object sender, EventArgs e)
    {
        GlobalVar.settingFontColor = true;
        ColorWheel_Click();
        //ColorWheel_Click(sender, e, "FG");
    }
    #endregion

    public static void ScreenSelectorProc() { Application.Run(GlobalVar.screenSelectorInstance = new Forms.ScreenSelectorForm()); }
    public static void ColorWheelProc() { Application.Run(GlobalVar.colorWheelInstance = new Forms.ColorWheel()); }
}
