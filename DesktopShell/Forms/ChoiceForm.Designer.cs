namespace DesktopShell
{
    partial class ChoiceForm
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
            this.filesGroup = new System.Windows.Forms.GroupBox();
            this.titleLabel = new System.Windows.Forms.Label();
            this.RandomLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // filesGroup
            // 
            this.filesGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.filesGroup.AutoSize = true;
            this.filesGroup.Location = new System.Drawing.Point(5, 30);
            this.filesGroup.Name = "filesGroup";
            this.filesGroup.Size = new System.Drawing.Size(341, 127);
            this.filesGroup.TabIndex = 0;
            this.filesGroup.TabStop = false;
            // 
            // titleLabel
            // 
            this.titleLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.25F);
            this.titleLabel.Location = new System.Drawing.Point(0, 0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(53, 17);
            this.titleLabel.TabIndex = 1;
            this.titleLabel.Text = "Games";
            // 
            // RandomLabel
            // 
            this.RandomLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.RandomLabel.AutoSize = true;
            this.RandomLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.RandomLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F);
            this.RandomLabel.Location = new System.Drawing.Point(277, 3);
            this.RandomLabel.Name = "RandomLabel";
            this.RandomLabel.Size = new System.Drawing.Size(62, 18);
            this.RandomLabel.TabIndex = 2;
            this.RandomLabel.Text = "Random";
            this.RandomLabel.Click += new System.EventHandler(this.RandomLabel_Click);
            // 
            // ChoiceForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(350, 162);
            this.Controls.Add(this.RandomLabel);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.filesGroup);
            this.ForeColor = System.Drawing.Color.LawnGreen;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ChoiceForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "ChoiceForm";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.ChoiceForm_Load);
            this.DoubleClick += new System.EventHandler(this.ChoiceForm_DoubleClick);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion


        private System.Windows.Forms.GroupBox filesGroup;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label RandomLabel;
    }
}