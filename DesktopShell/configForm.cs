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
    public partial class configForm : Form
    {
        private Regex hexCheck = new Regex("^(#)?([a-fA-F0-9]){6}$");
        private string checkString;

        public configForm()
        {
            InitializeComponent();
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
                    GlobalVar.shellInstance.changeFontColor(checkString);
                }
                else MessageBox.Show("Incorrect Format: (#000000)");
            }
            GlobalVar.SetSetting(1, checkString);
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
                    GlobalVar.shellInstance.changeBackgroundColor(checkString);
                }
                else MessageBox.Show("Incorrect Format: (#000000)");
            }
            GlobalVar.SetSetting(2, checkString);
        }
        
        private void hourlyChimeCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            GlobalVar.hourlyChime.Enabled = !GlobalVar.hourlyChime.Enabled;
            GlobalVar.SetSetting(3, Convert.ToString(GlobalVar.hourlyChime.Enabled));
        }
    }
}
