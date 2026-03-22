namespace DesktopShell
{
    partial class ConfigForm
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
            if(disposing && (components != null)) {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigForm));
            this.interfaceBox = new System.Windows.Forms.GroupBox();
            this.BackColorExample = new System.Windows.Forms.Label();
            this.BackColorWheel = new System.Windows.Forms.Label();
            this.ForeColorExample = new System.Windows.Forms.Label();
            this.ForeColorWheel = new System.Windows.Forms.Label();
            this.screenSelectorLabel = new System.Windows.Forms.Label();
            this.backgroundColorLabel = new System.Windows.Forms.Label();
            this.textColorLabel = new System.Windows.Forms.Label();
            this.backgroundColorInputBox = new System.Windows.Forms.TextBox();
            this.textColorInputBox = new System.Windows.Forms.TextBox();
            this.functionalityBox = new System.Windows.Forms.GroupBox();
            this.hourlyChimeCheckbox = new System.Windows.Forms.CheckBox();
            this.toolTipperBox = new System.Windows.Forms.GroupBox();
            this.fadeInLabel = new System.Windows.Forms.Label();
            this.fadeInComboBox = new System.Windows.Forms.ComboBox();
            this.fadeOutLabel = new System.Windows.Forms.Label();
            this.fadeOutComboBox = new System.Windows.Forms.ComboBox();
            this.alertColorLabel = new System.Windows.Forms.Label();
            this.alertColorInputBox = new System.Windows.Forms.TextBox();
            this.AlertColorWheel = new System.Windows.Forms.Label();
            this.AlertColorExample = new System.Windows.Forms.Label();
            this.interfaceBox.SuspendLayout();
            this.functionalityBox.SuspendLayout();
            this.toolTipperBox.SuspendLayout();
            this.SuspendLayout();
            //
            // interfaceBox
            //
            this.interfaceBox.Controls.Add(this.BackColorExample);
            this.interfaceBox.Controls.Add(this.BackColorWheel);
            this.interfaceBox.Controls.Add(this.ForeColorExample);
            this.interfaceBox.Controls.Add(this.ForeColorWheel);
            this.interfaceBox.Controls.Add(this.screenSelectorLabel);
            this.interfaceBox.Controls.Add(this.backgroundColorLabel);
            this.interfaceBox.Controls.Add(this.textColorLabel);
            this.interfaceBox.Controls.Add(this.backgroundColorInputBox);
            this.interfaceBox.Controls.Add(this.textColorInputBox);
            this.interfaceBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.interfaceBox.Location = new System.Drawing.Point(12, 12);
            this.interfaceBox.Name = "interfaceBox";
            this.interfaceBox.Size = new System.Drawing.Size(307, 98);
            this.interfaceBox.TabIndex = 2;
            this.interfaceBox.TabStop = false;
            this.interfaceBox.Text = "Interface";
            //
            // BackColorExample
            //
            this.BackColorExample.BackColor = System.Drawing.Color.Black;
            this.BackColorExample.Location = new System.Drawing.Point(281, 50);
            this.BackColorExample.Name = "BackColorExample";
            this.BackColorExample.Size = new System.Drawing.Size(18, 18);
            this.BackColorExample.TabIndex = 10;
            //
            // BackColorWheel
            //
            this.BackColorWheel.Image = ((System.Drawing.Image)(resources.GetObject("BackColorWheel.Image")));
            this.BackColorWheel.Location = new System.Drawing.Point(256, 50);
            this.BackColorWheel.Name = "BackColorWheel";
            this.BackColorWheel.Size = new System.Drawing.Size(20, 20);
            this.BackColorWheel.TabIndex = 9;
            this.BackColorWheel.Click += new System.EventHandler(this.BackColorWheel_Click);
            //
            // ForeColorExample
            //
            this.ForeColorExample.BackColor = System.Drawing.Color.Lime;
            this.ForeColorExample.Location = new System.Drawing.Point(281, 24);
            this.ForeColorExample.Name = "ForeColorExample";
            this.ForeColorExample.Size = new System.Drawing.Size(18, 18);
            this.ForeColorExample.TabIndex = 8;
            //
            // ForeColorWheel
            //
            this.ForeColorWheel.Image = ((System.Drawing.Image)(resources.GetObject("ForeColorWheel.Image")));
            this.ForeColorWheel.Location = new System.Drawing.Point(256, 24);
            this.ForeColorWheel.Name = "ForeColorWheel";
            this.ForeColorWheel.Size = new System.Drawing.Size(20, 20);
            this.ForeColorWheel.TabIndex = 7;
            this.ForeColorWheel.Click += new System.EventHandler(this.ForeColorWheel_Click);
            //
            // screenSelectorLabel
            //
            this.screenSelectorLabel.AutoSize = true;
            this.screenSelectorLabel.Location = new System.Drawing.Point(5, 79);
            this.screenSelectorLabel.Name = "screenSelectorLabel";
            this.screenSelectorLabel.Size = new System.Drawing.Size(49, 13);
            this.screenSelectorLabel.TabIndex = 4;
            this.screenSelectorLabel.Text = "Screens:";
            //
            // backgroundColorLabel
            //
            this.backgroundColorLabel.AutoSize = true;
            this.backgroundColorLabel.Location = new System.Drawing.Point(5, 53);
            this.backgroundColorLabel.Name = "backgroundColorLabel";
            this.backgroundColorLabel.Size = new System.Drawing.Size(121, 13);
            this.backgroundColorLabel.TabIndex = 5;
            this.backgroundColorLabel.Text = "Background Color (hex):";
            //
            // textColorLabel
            //
            this.textColorLabel.AutoSize = true;
            this.textColorLabel.Location = new System.Drawing.Point(5, 27);
            this.textColorLabel.Name = "textColorLabel";
            this.textColorLabel.Size = new System.Drawing.Size(84, 13);
            this.textColorLabel.TabIndex = 6;
            this.textColorLabel.Text = "Text Color (hex):";
            //
            // backgroundColorInputBox
            //
            this.backgroundColorInputBox.Location = new System.Drawing.Point(130, 50);
            this.backgroundColorInputBox.Name = "backgroundColorInputBox";
            this.backgroundColorInputBox.Size = new System.Drawing.Size(121, 20);
            this.backgroundColorInputBox.TabIndex = 0;
            this.backgroundColorInputBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CheckKeys_backgroundColor);
            //
            // textColorInputBox
            //
            this.textColorInputBox.Location = new System.Drawing.Point(130, 24);
            this.textColorInputBox.Name = "textColorInputBox";
            this.textColorInputBox.Size = new System.Drawing.Size(121, 20);
            this.textColorInputBox.TabIndex = 1;
            this.textColorInputBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CheckKeys_textColor);
            //
            // functionalityBox
            //
            this.functionalityBox.Controls.Add(this.hourlyChimeCheckbox);
            this.functionalityBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.functionalityBox.Location = new System.Drawing.Point(12, 116);
            this.functionalityBox.Name = "functionalityBox";
            this.functionalityBox.Size = new System.Drawing.Size(307, 50);
            this.functionalityBox.TabIndex = 1;
            this.functionalityBox.TabStop = false;
            this.functionalityBox.Text = "Functionality";
            //
            // hourlyChimeCheckbox
            //
            this.hourlyChimeCheckbox.AutoSize = true;
            this.hourlyChimeCheckbox.Location = new System.Drawing.Point(6, 22);
            this.hourlyChimeCheckbox.Name = "hourlyChimeCheckbox";
            this.hourlyChimeCheckbox.Size = new System.Drawing.Size(88, 17);
            this.hourlyChimeCheckbox.TabIndex = 0;
            this.hourlyChimeCheckbox.Text = "Hourly Chime";
            this.hourlyChimeCheckbox.UseVisualStyleBackColor = true;
            this.hourlyChimeCheckbox.CheckedChanged += new System.EventHandler(this.HourlyChimeCheckbox_CheckedChanged);
            //
            // toolTipperBox
            //
            this.toolTipperBox.Controls.Add(this.fadeInLabel);
            this.toolTipperBox.Controls.Add(this.fadeInComboBox);
            this.toolTipperBox.Controls.Add(this.fadeOutLabel);
            this.toolTipperBox.Controls.Add(this.fadeOutComboBox);
            this.toolTipperBox.Controls.Add(this.alertColorLabel);
            this.toolTipperBox.Controls.Add(this.alertColorInputBox);
            this.toolTipperBox.Controls.Add(this.AlertColorWheel);
            this.toolTipperBox.Controls.Add(this.AlertColorExample);
            this.toolTipperBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.toolTipperBox.Location = new System.Drawing.Point(12, 172);
            this.toolTipperBox.Name = "toolTipperBox";
            this.toolTipperBox.Size = new System.Drawing.Size(307, 105);
            this.toolTipperBox.TabIndex = 3;
            this.toolTipperBox.TabStop = false;
            this.toolTipperBox.Text = "ToolTipper";
            this.toolTipperBox.Visible = false;
            //
            // fadeInLabel
            //
            this.fadeInLabel.AutoSize = true;
            this.fadeInLabel.Location = new System.Drawing.Point(5, 24);
            this.fadeInLabel.Name = "fadeInLabel";
            this.fadeInLabel.Size = new System.Drawing.Size(75, 13);
            this.fadeInLabel.TabIndex = 0;
            this.fadeInLabel.Text = "Fade In Effect:";
            //
            // fadeInComboBox
            //
            this.fadeInComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.fadeInComboBox.FormattingEnabled = true;
            this.fadeInComboBox.Items.AddRange(new object[] { "none", "opacity", "dissolve" });
            this.fadeInComboBox.Location = new System.Drawing.Point(130, 21);
            this.fadeInComboBox.Name = "fadeInComboBox";
            this.fadeInComboBox.Size = new System.Drawing.Size(121, 21);
            this.fadeInComboBox.TabIndex = 1;
            this.fadeInComboBox.SelectedIndexChanged += new System.EventHandler(this.FadeInComboBox_SelectedIndexChanged);
            //
            // fadeOutLabel
            //
            this.fadeOutLabel.AutoSize = true;
            this.fadeOutLabel.Location = new System.Drawing.Point(5, 51);
            this.fadeOutLabel.Name = "fadeOutLabel";
            this.fadeOutLabel.Size = new System.Drawing.Size(83, 13);
            this.fadeOutLabel.TabIndex = 2;
            this.fadeOutLabel.Text = "Fade Out Effect:";
            //
            // fadeOutComboBox
            //
            this.fadeOutComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.fadeOutComboBox.FormattingEnabled = true;
            this.fadeOutComboBox.Items.AddRange(new object[] { "none", "opacity", "dissolve" });
            this.fadeOutComboBox.Location = new System.Drawing.Point(130, 48);
            this.fadeOutComboBox.Name = "fadeOutComboBox";
            this.fadeOutComboBox.Size = new System.Drawing.Size(121, 21);
            this.fadeOutComboBox.TabIndex = 3;
            this.fadeOutComboBox.SelectedIndexChanged += new System.EventHandler(this.FadeOutComboBox_SelectedIndexChanged);
            //
            // alertColorLabel
            //
            this.alertColorLabel.AutoSize = true;
            this.alertColorLabel.Location = new System.Drawing.Point(5, 78);
            this.alertColorLabel.Name = "alertColorLabel";
            this.alertColorLabel.Size = new System.Drawing.Size(88, 13);
            this.alertColorLabel.TabIndex = 4;
            this.alertColorLabel.Text = "Alert Color (hex):";
            //
            // alertColorInputBox
            //
            this.alertColorInputBox.Location = new System.Drawing.Point(130, 75);
            this.alertColorInputBox.Name = "alertColorInputBox";
            this.alertColorInputBox.Size = new System.Drawing.Size(121, 20);
            this.alertColorInputBox.TabIndex = 5;
            this.alertColorInputBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CheckKeys_alertColor);
            //
            // AlertColorWheel
            //
            this.AlertColorWheel.Image = ((System.Drawing.Image)(resources.GetObject("BackColorWheel.Image")));
            this.AlertColorWheel.Location = new System.Drawing.Point(256, 75);
            this.AlertColorWheel.Name = "AlertColorWheel";
            this.AlertColorWheel.Size = new System.Drawing.Size(20, 20);
            this.AlertColorWheel.TabIndex = 6;
            this.AlertColorWheel.Click += new System.EventHandler(this.AlertColorWheel_Click);
            //
            // AlertColorExample
            //
            this.AlertColorExample.BackColor = System.Drawing.Color.Red;
            this.AlertColorExample.Location = new System.Drawing.Point(281, 75);
            this.AlertColorExample.Name = "AlertColorExample";
            this.AlertColorExample.Size = new System.Drawing.Size(18, 18);
            this.AlertColorExample.TabIndex = 7;
            //
            // ConfigForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(330, 213);
            this.Controls.Add(this.toolTipperBox);
            this.Controls.Add(this.functionalityBox);
            this.Controls.Add(this.interfaceBox);
            this.Name = "ConfigForm";
            this.ShowInTaskbar = false;
            this.Text = "Options";
            this.Load += new System.EventHandler(this.ConfigForm_Load);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ConfigForm_FormClosed);
            this.interfaceBox.ResumeLayout(false);
            this.interfaceBox.PerformLayout();
            this.functionalityBox.ResumeLayout(false);
            this.functionalityBox.PerformLayout();
            this.toolTipperBox.ResumeLayout(false);
            this.toolTipperBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox interfaceBox;
        private System.Windows.Forms.GroupBox functionalityBox;
        private System.Windows.Forms.GroupBox toolTipperBox;
        private System.Windows.Forms.Label backgroundColorLabel;
        private System.Windows.Forms.Label textColorLabel;
        private System.Windows.Forms.TextBox backgroundColorInputBox;
        private System.Windows.Forms.TextBox textColorInputBox;
        private System.Windows.Forms.CheckBox hourlyChimeCheckbox;
        private System.Windows.Forms.Label screenSelectorLabel;
        //private System.Windows.Forms.Button screenSelectorButton;
        private System.Windows.Forms.Label ForeColorWheel;
        private System.Windows.Forms.Label ForeColorExample;
        private System.Windows.Forms.Label BackColorWheel;
        private System.Windows.Forms.Label BackColorExample;
        //private System.Windows.Forms.CheckBox multiScreenCheckbox;
        private System.Windows.Forms.Label fadeInLabel;
        private System.Windows.Forms.ComboBox fadeInComboBox;
        private System.Windows.Forms.Label fadeOutLabel;
        private System.Windows.Forms.ComboBox fadeOutComboBox;
        private System.Windows.Forms.Label alertColorLabel;
        private System.Windows.Forms.TextBox alertColorInputBox;
        private System.Windows.Forms.Label AlertColorWheel;
        private System.Windows.Forms.Label AlertColorExample;
    }
}
