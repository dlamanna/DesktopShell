using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DesktopShell
{
    public partial class Tooltip : Form
    {
        private Timer tooltipTimer;

        public Tooltip()
        {
            InitializeComponent();
        }

        private void Tooltip_Click(object sender, EventArgs e)
        {
            //create timer to fade out
            tooltipTimer = new Timer();
            tooltipTimer.Interval = 100;
            tooltipTimer.Tick += delegate { TimerTick(tooltipTimer, EventArgs.Empty); };
            tooltipTimer.Enabled = true;
        }

        public void TimerTick(object sender, EventArgs e)
        {
            if (this.Opacity > .1)
            {
                this.Opacity -= 0.065D;
            }
            else
            {
                this.Enabled = false;
                this.Close();
                this.Opacity = 0.75D;
            }
        }

        private void Tooltip_Load(object sender, EventArgs e)
        {
            this.Location = new Point(2705, 139);
        }
    }
}
