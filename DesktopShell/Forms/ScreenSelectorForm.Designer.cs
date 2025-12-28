namespace DesktopShell.Forms
{
    partial class ScreenSelectorForm
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
            this.borderLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // borderLabel
            //
            this.borderLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.borderLabel.Click += ScreenSelectorForm_Click;
            this.borderLabel.Location = new System.Drawing.Point(0, 0);
            this.borderLabel.MouseLeave += new System.EventHandler(this.ScreenSelectorForm_MouseLeave);
            this.borderLabel.Name = "borderLabel";
            this.borderLabel.Paint += BorderLabel_Paint;
            this.borderLabel.Size = new System.Drawing.Size(1217, 820);
            this.borderLabel.TabIndex = 0;
            // 
            // ScreenSelector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.AntiqueWhite;
            this.Click += ScreenSelectorForm_Click;
            this.ClientSize = new System.Drawing.Size(1217, 820);
            this.ControlBox = false;
            this.Controls.Add(this.borderLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Load += ScreenSelectorForm_Load;
            this.Opacity = 0.7D;
            //this.MouseLeave += new System.EventHandler(this.ScreenSelectorForm_MouseLeave);
            this.Name = "ScreenSelectorForm";
            this.ShowInTaskbar = false;
            this.Text = "ScreenSelectorForm";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.White;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label borderLabel;
    }
}