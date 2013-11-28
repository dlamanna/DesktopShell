using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DesktopShell
{
    public partial class ChoiceForm : Form
    {
        public ChoiceForm() { InitializeComponent(); }

        public void ChoiceForm_Load(object sender, EventArgs e)
        {
            //GlobalVar.setBounds(this);
            GlobalVar.setCentered(Screen.FromPoint(Properties.Settings.positionSave), this);
            this.filesGroup.Controls.AddRange(GlobalVar.populateLabels());
            
            int fileCount = GlobalVar.fileChoices.Count;
            foreach (Control c in this.filesGroup.Controls) c.Click += new EventHandler(ChoiceForm_Click);
            this.titleLabel.Left = (350 / 2 - (this.titleLabel.Width / 2));
            this.titleLabel.Text = GlobalVar.searchType + " (" + fileCount.ToString() + ")";
            this.BackColor = this.titleLabel.BackColor = Properties.Settings.backgroundColor;
            this.ForeColor = this.titleLabel.ForeColor = Properties.Settings.foregroundColor;
            this.ClientSize = new System.Drawing.Size(350, (fileCount * 18) + 4);
        }
        public void ChoiceForm_Click(object sender, EventArgs e)
        {
            Regex extension = new Regex(".([a-z]|[A-Z]){3,4}$");
            foreach (FileInfo f in GlobalVar.fileChoices)
            {
                if (((Label)sender).Text.IndexOf(extension.Replace(f.Name,"")) != -1)
                {
                    if (GlobalVar.searchType == "Movie") GlobalVar.Run(@"D:\Program Files (x86)\VLC Media Player\vlc.exe", f.FullName);
                    else GlobalVar.Run(f.FullName);
                    
                    this.Close();
                }
            }
        }
        public void ChoiceForm_DoubleClick(object sender, EventArgs e)
        {
            this.Close();
        }
        private void RandomLabel_Click(object sender, System.EventArgs e)
        {
            Random random = new Random();
            int randNum = random.Next(0,GlobalVar.fileChoices.Count);
            String fileName = ((FileInfo)GlobalVar.fileChoices[randNum]).FullName;

            if (GlobalVar.searchType == "Movie") GlobalVar.Run(@"D:\Program Files (x86)\VLC Media Player\vlc.exe", fileName);
            else GlobalVar.Run(fileName);

            this.Close();
        }
    }
}
