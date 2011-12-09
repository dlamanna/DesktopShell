namespace DesktopShell
{
    partial class configForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.interfaceBox = new System.Windows.Forms.GroupBox();
            this.backgroundColorLabel = new System.Windows.Forms.Label();
            this.textColorLabel = new System.Windows.Forms.Label();
            this.backgroundColorInputBox = new System.Windows.Forms.TextBox();
            this.textColorInputBox = new System.Windows.Forms.TextBox();
            this.functionalityBox = new System.Windows.Forms.GroupBox();
            this.hourlyChimeCheckbox = new System.Windows.Forms.CheckBox();
            this.interfaceBox.SuspendLayout();
            this.functionalityBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // interfaceBox
            // 
            this.interfaceBox.Controls.Add(this.backgroundColorLabel);
            this.interfaceBox.Controls.Add(this.textColorLabel);
            this.interfaceBox.Controls.Add(this.backgroundColorInputBox);
            this.interfaceBox.Controls.Add(this.textColorInputBox);
            this.interfaceBox.Location = new System.Drawing.Point(12, 12);
            this.interfaceBox.Name = "interfaceBox";
            this.interfaceBox.Size = new System.Drawing.Size(307, 77);
            this.interfaceBox.TabIndex = 0;
            this.interfaceBox.TabStop = false;
            this.interfaceBox.Text = "Interface";
            // 
            // backgroundColorLabel
            // 
            this.backgroundColorLabel.AutoSize = true;
            this.backgroundColorLabel.Location = new System.Drawing.Point(5, 53);
            this.backgroundColorLabel.Name = "backgroundColorLabel";
            this.backgroundColorLabel.Size = new System.Drawing.Size(121, 13);
            this.backgroundColorLabel.TabIndex = 3;
            this.backgroundColorLabel.Text = "Background Color (hex):";
            // 
            // textColorLabel
            // 
            this.textColorLabel.AutoSize = true;
            this.textColorLabel.Location = new System.Drawing.Point(5, 31);
            this.textColorLabel.Name = "textColorLabel";
            this.textColorLabel.Size = new System.Drawing.Size(84, 13);
            this.textColorLabel.TabIndex = 2;
            this.textColorLabel.Text = "Text Color (hex):";
            // 
            // backgroundColorInputBox
            // 
            this.backgroundColorInputBox.Location = new System.Drawing.Point(136, 50);
            this.backgroundColorInputBox.Name = "backgroundColorInputBox";
            this.backgroundColorInputBox.Size = new System.Drawing.Size(152, 20);
            this.backgroundColorInputBox.Text = GlobalVar.GetSetting(2);
            this.backgroundColorInputBox.TabIndex = 1;
            this.backgroundColorInputBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.checkKeys_backgroundColor);
            // 
            // textColorInputBox
            // 
            this.textColorInputBox.Location = new System.Drawing.Point(136, 24);
            this.textColorInputBox.Name = "textColorInputBox";
            this.textColorInputBox.Size = new System.Drawing.Size(152, 20);
            this.textColorInputBox.Text = GlobalVar.GetSetting(1);
            this.textColorInputBox.TabIndex = 0;
            this.textColorInputBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.checkKeys_textColor);
            // 
            // functionalityBox
            // 
            this.functionalityBox.Controls.Add(this.hourlyChimeCheckbox);
            this.functionalityBox.Location = new System.Drawing.Point(14, 116);
            this.functionalityBox.Name = "functionalityBox";
            this.functionalityBox.Size = new System.Drawing.Size(305, 73);
            this.functionalityBox.TabIndex = 1;
            this.functionalityBox.TabStop = false;
            this.functionalityBox.Text = "Functionality";
            // 
            // hourlyChimeCheckbox
            // 
            this.hourlyChimeCheckbox.AutoSize = true;
            if(System.Convert.ToBoolean(GlobalVar.GetSetting(3))) this.hourlyChimeCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            else this.hourlyChimeCheckbox.CheckState = System.Windows.Forms.CheckState.Unchecked;
            this.hourlyChimeCheckbox.Location = new System.Drawing.Point(6, 33);
            this.hourlyChimeCheckbox.Name = "hourlyChimeCheckbox";
            this.hourlyChimeCheckbox.Size = new System.Drawing.Size(88, 17);
            this.hourlyChimeCheckbox.TabIndex = 0;
            this.hourlyChimeCheckbox.Text = "Hourly Chime";
            this.hourlyChimeCheckbox.UseVisualStyleBackColor = true;
            this.hourlyChimeCheckbox.CheckedChanged += new System.EventHandler(this.hourlyChimeCheckbox_CheckedChanged);
            // 
            // configForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(330, 213);
            this.Controls.Add(this.functionalityBox);
            this.Controls.Add(this.interfaceBox);
            this.Name = "configForm";
            this.Text = "Options";
            this.interfaceBox.ResumeLayout(false);
            this.interfaceBox.PerformLayout();
            this.functionalityBox.ResumeLayout(false);
            this.functionalityBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox interfaceBox;
        private System.Windows.Forms.GroupBox functionalityBox;
        private System.Windows.Forms.Label backgroundColorLabel;
        private System.Windows.Forms.Label textColorLabel;
        private System.Windows.Forms.TextBox backgroundColorInputBox;
        private System.Windows.Forms.TextBox textColorInputBox;
        private System.Windows.Forms.CheckBox hourlyChimeCheckbox;
    }
}