using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace DesktopShell.Forms;

public partial class ScreenSelectorForm : Form
{
    public ScreenSelectorForm()
    {
        InitializeComponent();
    }

    private void ScreenSelectorForm_Load(object sender, System.EventArgs e)
    {
        Screen currentScreen = Screen.FromPoint(Cursor.Position);
        this.borderLabel.Size = currentScreen.WorkingArea.Size;
        this.Size = currentScreen.WorkingArea.Size;
        this.Location = new Point(currentScreen.Bounds.Left, currentScreen.Bounds.Top);
        //GlobalVar.toolTip("ScreenSelector_Load", "Location: " + this.Location + ", Size:" + this.Size);
    }
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
    }
    private void ScreenSelectorForm_MouseLeave(object sender, System.EventArgs e)
    {
        Point alteredPoint = new Point(Cursor.Position.X, Cursor.Position.Y);
        if (this.Bounds.Left >= Cursor.Position.X) alteredPoint.X = Cursor.Position.X - 50;
        else if (this.Bounds.Right <= Cursor.Position.X) alteredPoint.X = Cursor.Position.X + 50;

        Screen currentScreen = Screen.FromPoint(alteredPoint);
        this.borderLabel.Size = currentScreen.WorkingArea.Size;
        this.Size = currentScreen.WorkingArea.Size;
        this.Location = new Point(currentScreen.Bounds.Left, currentScreen.Bounds.Top);
        //GlobalVar.toolTip("MouseLeave", "CursorPos: " + Cursor.Position);
    }
    private void ScreenSelectorForm_Click(object sender, System.EventArgs e)
    {
        Properties.Settings.positionSave = Cursor.Position;
        Properties.Settings.WriteSettings();
        if (GlobalVar.shellInstance != null)
            GlobalVar.SetCentered(Screen.FromPoint(Cursor.Position), obj: GlobalVar.shellInstance);

        Application.ExitThread();
    }
    private void borderLabel_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
    {
        int borderWidth = 20;
        ControlPaint.DrawBorder(e.Graphics, borderLabel.DisplayRectangle,
            Color.Blue, borderWidth, ButtonBorderStyle.Solid,
            Color.Blue, borderWidth, ButtonBorderStyle.Solid,
            Color.Blue, borderWidth, ButtonBorderStyle.Solid,
            Color.Blue, borderWidth, ButtonBorderStyle.Solid);
    }
}
