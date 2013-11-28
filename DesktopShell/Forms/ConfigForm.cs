using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DesktopShell
{
    public partial class ConfigForm : Form
    {
        #region Declarations
        private Regex hexCheck = new Regex("^(#)?([a-fA-F0-9]){6}$");
        private string checkString;
        #endregion

        #region ConfigForm Constructor / Startup
        public ConfigForm() { InitializeComponent(); }
        #endregion

        #region Form Event Handlers
        private void ConfigForm_Load(object sender, EventArgs e)
        {
            //checkbox initialization
            if (Properties.Settings.hourlyChimeChecked) this.hourlyChimeCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            else this.hourlyChimeCheckbox.CheckState = System.Windows.Forms.CheckState.Unchecked;
            //foreground color initialization
            this.textColorInputBox.Text = (System.Drawing.ColorTranslator.ToHtml(Properties.Settings.foregroundColor)).ToString();
            //background color initialization
            this.backgroundColorInputBox.Text = (System.Drawing.ColorTranslator.ToHtml(Properties.Settings.backgroundColor)).ToString();
            //set initial position
            this.Location = Cursor.Position;
        }
        private void ConfigForm_FormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e)
        {
            if (GlobalVar.screenSelectorInstance != null) GlobalVar.screenSelectorInstance.Close();
        }
        private void checkKeys_textColor(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; //prevents beep
                checkString = this.textColorInputBox.Text;
                if (hexCheck.IsMatch(checkString))
                {
                    MessageBox.Show("Font Color Changed");
                    //change setting
                    Properties.Settings.foregroundColor = System.Drawing.ColorTranslator.FromHtml(checkString);
                    //change in program
                    GlobalVar.shellInstance.changeFontColor();
                    //write to file
                    Properties.Settings.writeSettings();
                }
                else MessageBox.Show("Incorrect Format: (#123456)");
            }
        }
        private void checkKeys_backgroundColor(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; //prevents beep
                checkString = this.backgroundColorInputBox.Text;
                if (hexCheck.IsMatch(checkString))
                {
                    MessageBox.Show("Background Color Changed");
                    //Changing in settings
                    Properties.Settings.backgroundColor = System.Drawing.ColorTranslator.FromHtml(checkString);
                    //Changing in program
                    GlobalVar.shellInstance.changeBackgroundColor();
                    //Write to file
                    Properties.Settings.writeSettings();
                }
                else MessageBox.Show("Incorrect Format: (#123456)");
            }
        }
        private void hourlyChimeCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            //Toggle / save setting
            GlobalVar.hourlyChime.Enabled = Properties.Settings.hourlyChimeChecked = !GlobalVar.hourlyChime.Enabled;
            //Write to file
            Properties.Settings.writeSettings();
        }
        private void screenSelectorButton_Click(object sender, System.EventArgs e)
        {
            System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(ScreenSelectorProc));
            t.Start();
        }
        #endregion

        public static void ScreenSelectorProc() { Application.Run(GlobalVar.screenSelectorInstance = new DesktopShell.Forms.ScreenSelectorForm()); }
        
    }
}
