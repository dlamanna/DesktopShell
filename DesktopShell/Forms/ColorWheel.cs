using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace DesktopShell.Forms;

/// <summary>
/// Summary description for ColorWheel.
/// </summary>
public class ColorWheel : Form
{
    internal Button BtnCancel;
    internal Button BtnOK;
    internal Label Label3;
    internal NumericUpDown NudSaturation;
    internal Label Label7;
    internal NumericUpDown NudBrightness;
    internal NumericUpDown NudRed;
    internal Panel PnlColor;
    internal Label Label6;
    internal Label Label1;
    internal Label Label5;
    internal Panel PnlSelectedColor;
    internal Panel PnlBrightness;
    internal NumericUpDown NudBlue;
    internal Label Label4;
    internal NumericUpDown NudGreen;
    internal Label Label2;
    internal NumericUpDown NudHue;

    private Rectangle colorRectangle;
    private Rectangle brightnessRectangle;
    private Rectangle selectedColorRectangle;
    private Point centerPoint;
    private readonly int radius;
    private readonly int brightnessX;
    private readonly double brightnessScaling;
    private const int colorCount = 6 * 256;
    private const double degreesPerRadian = 180.0 / Math.PI;
    private Point colorPoint;
    private Point brightnessPoint;
    private Graphics g;
    private readonly Region colorRegion;
    private readonly Region brightnessRegion;
    private Bitmap colorImage;
    private int brightness;
    private readonly int brightnessMin;
    private readonly int brightnessMax;
    private Color selectedColor = Color.White;
    private Color fullColor;

    public delegate void ColorChangedEventHandler(object sender, ColorChangedEventArgs e);

    public ColorChangedEventHandler ColorChanged;

    // Keep track of the current mouse state.
    public enum MouseState
    {
        MouseUp,
        ClickOnColor,
        DragInColor,
        ClickOnBrightness,
        DragInBrightness,
        ClickOutsideRegion,
        DragOutsideRegion,
    }

    private MouseState currentState = MouseState.MouseUp;
    private readonly System.ComponentModel.Container? components = null;

    public ColorWheel()
    {
        ///TODO: Make Colorwheel start in the correct RGB/HSV settings
        InitializeComponent();
        GlobalVar.ColorWheelInstance = this;
    }

    public ColorWheel(Rectangle colorRectangle, Rectangle brightnessRectangle, Rectangle selectedColorRectangle)
    {
        using GraphicsPath path = new();
        // Store away locations for later use.
        this.colorRectangle = colorRectangle;
        this.brightnessRectangle = brightnessRectangle;
        this.selectedColorRectangle = selectedColorRectangle;

        // Calculate the center of the circle. Start with the location, then offset the point by the radius. Use the smaller of the width and height of
        // the colorRectangle value.
        radius = (int)Math.Min(colorRectangle.Width, colorRectangle.Height) / 2;
        centerPoint = colorRectangle.Location;
        centerPoint.Offset(radius, radius);

        // Start the pointer in the center.
        colorPoint = centerPoint;

        // Create a region corresponding to the color circle. Code uses this later to determine if a specified point is within the region, using the IsVisible method.
        path.AddEllipse(colorRectangle);
        colorRegion = new Region(path);

        // set the range for the brightness selector.
        brightnessMin = brightnessRectangle.Top;
        brightnessMax = brightnessRectangle.Bottom;

        // Create a region corresponding to the brightness rectangle, with a little extra padding.
        path.AddRectangle(new Rectangle(brightnessRectangle.Left, brightnessRectangle.Top - 10, brightnessRectangle.Width + 10, brightnessRectangle.Height + 20));

        // Create region corresponding to brightness rectangle. Later code uses this to determine if a specified point is within the region, using the IsVisible method.
        brightnessRegion = new Region(path);

        // Set the location for the brightness indicator "marker". Also calculate the scaling factor, scaling the height to be between 0 and 255.
        brightnessX = brightnessRectangle.Left + brightnessRectangle.Width;
        brightnessScaling = (double)255 / (brightnessMax - brightnessMin);

        // Calculate the location of the brightness pointer. Assume it's at the highest position.
        brightnessPoint = new Point(brightnessX, brightnessMax);

        // Create the bitmap that contains the circular gradient.
        CreateGradient();
    }

    protected override void Dispose(bool disposing)
    {
        GlobalVar.Log($"!!! Disposing ColorWheel");
        if (disposing)
        {
            // Dispose of graphic resources
            if (colorImage != null)
            {
                colorImage.Dispose();
            }

            if (colorRegion != null)
            {
                colorRegion.Dispose();
            }

            if (brightnessRegion != null)
            {
                brightnessRegion.Dispose();
            }

            if (g != null)
            {
                g.Dispose();
            }

            if (components != null)
            {
                components.Dispose();
            }
        }

        GlobalVar.ColorWheelInstance = null;
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        BtnCancel = new Button();
        BtnOK = new Button();
        Label3 = new Label();
        NudSaturation = new NumericUpDown();
        Label7 = new Label();
        NudBrightness = new NumericUpDown();
        NudRed = new NumericUpDown();
        PnlColor = new Panel();
        Label6 = new Label();
        Label1 = new Label();
        Label5 = new Label();
        PnlSelectedColor = new Panel();
        PnlBrightness = new Panel();
        NudBlue = new NumericUpDown();
        Label4 = new Label();
        NudGreen = new NumericUpDown();
        Label2 = new Label();
        NudHue = new NumericUpDown();
        NudSaturation.BeginInit();
        NudBrightness.BeginInit();
        NudRed.BeginInit();
        NudBlue.BeginInit();
        NudGreen.BeginInit();
        NudHue.BeginInit();
        SuspendLayout();
        //
        // BtnCancel
        //
        BtnCancel.DialogResult = DialogResult.Cancel;
        BtnCancel.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        BtnCancel.Location = new Point(192, 320);
        BtnCancel.Name = "btnCancel";
        BtnCancel.Size = new Size(64, 24);
        BtnCancel.TabIndex = 55;
        BtnCancel.Text = "Cancel";
        BtnCancel.Click += BtnCancel_Click;
        //
        // BtnOK
        //
        BtnOK.DialogResult = DialogResult.OK;
        BtnOK.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        BtnOK.Location = new Point(120, 320);
        BtnOK.Name = "btnOK";
        BtnOK.Size = new Size(64, 24);
        BtnOK.TabIndex = 54;
        BtnOK.Text = "OK";
        BtnOK.Click += BtnOK_Click;
        //
        // Label3
        //
        Label3.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Label3.Location = new Point(152, 280);
        Label3.Name = "Label3";
        Label3.Size = new Size(48, 23);
        Label3.TabIndex = 45;
        Label3.Text = "Blue:";
        Label3.TextAlign = ContentAlignment.MiddleLeft;
        //
        // NudSaturation
        //
        NudSaturation.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        NudSaturation.Location = new Point(96, 256);
        NudSaturation.Maximum = new decimal([
            255,
            0,
            0,
            0]);
        NudSaturation.Name = "nudSaturation";
        NudSaturation.Size = new Size(48, 22);
        NudSaturation.TabIndex = 42;
        NudSaturation.TextChanged += HandleTextChanged;
        NudSaturation.ValueChanged += HandleHSVChange;
        //
        // Label7
        //
        Label7.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Label7.Location = new Point(16, 280);
        Label7.Name = "Label7";
        Label7.Size = new Size(72, 23);
        Label7.TabIndex = 50;
        Label7.Text = "Brightness:";
        Label7.TextAlign = ContentAlignment.MiddleLeft;
        //
        // NudBrightness
        //
        NudBrightness.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        NudBrightness.Location = new Point(96, 280);
        NudBrightness.Maximum = new decimal([
            255,
            0,
            0,
            0]);
        NudBrightness.Name = "nudBrightness";
        NudBrightness.Size = new Size(48, 22);
        NudBrightness.TabIndex = 47;
        NudBrightness.TextChanged += HandleTextChanged;
        NudBrightness.ValueChanged += HandleHSVChange;
        //
        // NudRed
        //
        NudRed.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        NudRed.Location = new Point(208, 232);
        NudRed.Maximum = new decimal([
            255,
            0,
            0,
            0]);
        NudRed.Name = "nudRed";
        NudRed.Size = new Size(48, 22);
        NudRed.TabIndex = 38;
        NudRed.TextChanged += HandleTextChanged;
        NudRed.ValueChanged += HandleRGBChange;
        //
        // PnlColor
        //
        PnlColor.Location = new Point(8, 8);
        PnlColor.Name = "pnlColor";
        PnlColor.Size = new Size(176, 176);
        PnlColor.TabIndex = 51;
        PnlColor.Visible = false;
        PnlColor.MouseUp += FrmMain_MouseUp;
        //
        // Label6
        //
        Label6.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Label6.Location = new Point(16, 256);
        Label6.Name = "Label6";
        Label6.Size = new Size(72, 23);
        Label6.TabIndex = 49;
        Label6.Text = "Saturation:";
        Label6.TextAlign = ContentAlignment.MiddleLeft;
        //
        // Label1
        //
        Label1.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Label1.Location = new Point(152, 232);
        Label1.Name = "Label1";
        Label1.Size = new Size(48, 23);
        Label1.TabIndex = 43;
        Label1.Text = "Red:";
        Label1.TextAlign = ContentAlignment.MiddleLeft;
        //
        // Label5
        //
        Label5.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Label5.Location = new Point(16, 232);
        Label5.Name = "Label5";
        Label5.Size = new Size(72, 23);
        Label5.TabIndex = 48;
        Label5.Text = "Hue:";
        Label5.TextAlign = ContentAlignment.MiddleLeft;
        //
        // PnlSelectedColor
        //
        PnlSelectedColor.Location = new Point(208, 200);
        PnlSelectedColor.Name = "pnlSelectedColor";
        PnlSelectedColor.Size = new Size(48, 24);
        PnlSelectedColor.TabIndex = 53;
        PnlSelectedColor.Visible = false;
        //
        // PnlBrightness
        //
        PnlBrightness.Location = new Point(208, 8);
        PnlBrightness.Name = "pnlBrightness";
        PnlBrightness.Size = new Size(16, 176);
        PnlBrightness.TabIndex = 52;
        PnlBrightness.Visible = false;
        //
        // NudBlue
        //
        NudBlue.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        NudBlue.Location = new Point(208, 280);
        NudBlue.Maximum = new decimal([
            255,
            0,
            0,
            0]);
        NudBlue.Name = "nudBlue";
        NudBlue.Size = new Size(48, 22);
        NudBlue.TabIndex = 40;
        NudBlue.TextChanged += HandleTextChanged;
        NudBlue.ValueChanged += HandleRGBChange;
        //
        // Label4
        //
        Label4.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Label4.Location = new Point(152, 200);
        Label4.Name = "Label4";
        Label4.Size = new Size(48, 24);
        Label4.TabIndex = 46;
        Label4.Text = "Color:";
        Label4.TextAlign = ContentAlignment.MiddleLeft;
        //
        // NudGreen
        //
        NudGreen.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        NudGreen.Location = new Point(208, 256);
        NudGreen.Maximum = new decimal([
            255,
            0,
            0,
            0]);
        NudGreen.Name = "nudGreen";
        NudGreen.Size = new Size(48, 22);
        NudGreen.TabIndex = 39;
        NudGreen.TextChanged += HandleTextChanged;
        NudGreen.ValueChanged += HandleRGBChange;
        //
        // Label2
        //
        Label2.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Label2.Location = new Point(152, 256);
        Label2.Name = "Label2";
        Label2.Size = new Size(48, 23);
        Label2.TabIndex = 44;
        Label2.Text = "Green:";
        Label2.TextAlign = ContentAlignment.MiddleLeft;
        //
        // NudHue
        //
        NudHue.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        NudHue.Location = new Point(96, 232);
        NudHue.Maximum = new decimal([
            255,
            0,
            0,
            0]);
        NudHue.Name = "nudHue";
        NudHue.Size = new Size(48, 22);
        NudHue.TabIndex = 41;
        NudHue.TextChanged += HandleTextChanged;
        NudHue.ValueChanged += HandleHSVChange;
        //
        // ColorWheel
        //
        AutoScaleBaseSize = new Size(5, 13);
        ClientSize = new Size(264, 349);
        Controls.Add(BtnCancel);
        Controls.Add(BtnOK);
        Controls.Add(Label3);
        Controls.Add(NudSaturation);
        Controls.Add(Label7);
        Controls.Add(NudBrightness);
        Controls.Add(NudRed);
        Controls.Add(PnlColor);
        Controls.Add(Label6);
        Controls.Add(Label1);
        Controls.Add(Label5);
        Controls.Add(PnlSelectedColor);
        Controls.Add(PnlBrightness);
        Controls.Add(NudBlue);
        Controls.Add(Label4);
        Controls.Add(NudGreen);
        Controls.Add(Label2);
        Controls.Add(NudHue);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ColorWheel";
        ShowInTaskbar = false;
        Text = "Select Color";
        Load += ColorWheel_Load;
        Paint += ColorWheel_Paint;
        MouseDown += HandleMouse;
        MouseMove += HandleMouse;
        MouseUp += FrmMain_MouseUp;
        NudSaturation.EndInit();
        NudBrightness.EndInit();
        NudRed.EndInit();
        NudBlue.EndInit();
        NudGreen.EndInit();
        NudHue.EndInit();
        ResumeLayout(false);
    }

    #endregion Windows Form Designer generated code

    private enum ChangeStyle
    {
        MouseMove,
        RGB,
        HSV,
        None
    }

    private ChangeStyle changeType = ChangeStyle.None;
    private Point selectedPoint;

    private ColorWheel myColorWheel;
    private ColorHandler.RGB rgb;
    private ColorHandler.HSV hsv;
    private bool isInUpdate = false;

    private void ColorWheel_Load(object sender, EventArgs e)
    {
        // Turn on double-buffering, so the form looks better.
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.DoubleBuffer, true);

        // These properties are set in design view, as well, but they have to be set to false in order for the Paint event to be able to display their contents.
        PnlSelectedColor.Visible = false;
        PnlBrightness.Visible = false;
        PnlColor.Visible = false;

        // Calculate the coordinates of the three required regions on the form.
        Rectangle selectedColorRectangle = new(PnlSelectedColor.Location, PnlSelectedColor.Size);
        Rectangle brightnessRectangle = new(PnlBrightness.Location, PnlBrightness.Size);
        Rectangle colorRectangle = new(PnlColor.Location, PnlColor.Size);

        // Create the new ColorWheel class, indicating the locations of the color wheel itself, the brightness area, and the position of the selected color.
        myColorWheel = new ColorWheel(colorRectangle, brightnessRectangle, selectedColorRectangle);
        myColorWheel.ColorChanged += new ColorChangedEventHandler(MyColorWheel_ColorChanged);

        /// TODO: I think this is where I need to do the conversion from hash color code to RGB HSV

        // Set the RGB and HSV values of the NumericUpDown controls.
        SetRGB(rgb);
        SetHSV(hsv);

        Location = Cursor.Position;
    }

    private void HandleMouse(object sender, MouseEventArgs e)
    {
        // If you have the left mouse button down, then update the selectedPoint value and force a repaint of the color wheel.
        if (e.Button == MouseButtons.Left)
        {
            changeType = ChangeStyle.MouseMove;
            selectedPoint = new Point(e.X, e.Y);
            Invalidate();
        }
    }

    private void FrmMain_MouseUp(object sender, MouseEventArgs e)
    {
        myColorWheel.SetMouseUp();
        changeType = ChangeStyle.None;
    }

    public void SetMouseUp()
    {
        // Indicate that the user has released the mouse.
        currentState = MouseState.MouseUp;
    }

    private void HandleRGBChange(object sender, EventArgs e)
    {
        // If the R, G, or B values change, use this code to update the HSV values and invalidate the color wheel (so it updates the pointers). Check the isInUpdate flag to avoid recursive events
        // when you update the NumericUpdownControls.
        if (!isInUpdate)
        {
            changeType = ChangeStyle.RGB;
            rgb = new ColorHandler.RGB((int)NudRed.Value, (int)NudGreen.Value, (int)NudBlue.Value);
            SetHSV(ColorHandler.RGBtoHSV(rgb));
            Invalidate();
        }
    }

    private void HandleHSVChange(object sender, EventArgs e)
    {
        // If the H, S, or V values change, use this code to update the RGB values and invalidate the color wheel (so it updates the pointers). Check the isInUpdate flag to avoid recursive events
        // when you update the NumericUpdownControls.
        if (!isInUpdate)
        {
            changeType = ChangeStyle.HSV;
            hsv = new ColorHandler.HSV((int)NudHue.Value, (int)NudSaturation.Value, (int)NudBrightness.Value);
            SetRGB(ColorHandler.HSVtoRGB(hsv));
            Invalidate();
        }
    }

    private void SetRGB(ColorHandler.RGB rgb)
    {
        // Update the RGB values on the form, but don't trigger the ValueChanged event of the form. The isInUpdate variable ensures that the event procedures exit without doing anything.
        isInUpdate = true;
        RefreshValue(NudRed, rgb.Red);
        RefreshValue(NudBlue, rgb.Blue);
        RefreshValue(NudGreen, rgb.Green);
        isInUpdate = false;
    }

    private void SetHSV(ColorHandler.HSV hsv)
    {
        // Update the HSV values on the form, but don't trigger the ValueChanged event of the form. The isInUpdate variable ensures that the event procedures exit without doing anything.
        isInUpdate = true;
        RefreshValue(NudHue, hsv.Hue);
        RefreshValue(NudSaturation, hsv.Saturation);
        RefreshValue(NudBrightness, hsv.Value);
        isInUpdate = false;
    }

    private void HandleTextChanged(object sender, EventArgs e)
    {
        // This step works around a bug -- unless you actively retrieve the value, the min and max settings for the control aren't honored when you type text. This may be fixed in the 1.1 version, but in VS.NET 1.0, this
        // step is required.
        _ = ((NumericUpDown)sender).Value;
    }

    private static void RefreshValue(NumericUpDown nud, int value)
    {
        // Update the value of the NumericUpDown control, if the value is different than the current value. Refresh the control, causing an immediate repaint.
        if (nud.Value != value)
        {
            nud.Value = value;
            nud.Refresh();
        }
    }

    public Color Color
    {
        // Get or set the color to be displayed in the color wheel.
        get => myColorWheel.Color;

        set
        {
            // Indicate the color change type. Either RGB or HSV will cause the color wheel to update the position of the pointer.
            changeType = ChangeStyle.RGB;
            rgb = new ColorHandler.RGB(value.R, value.G, value.B);
            hsv = ColorHandler.RGBtoHSV(rgb);
        }
    }

    private void MyColorWheel_ColorChanged(object sender, ColorChangedEventArgs e)
    {
        SetRGB(e.RGB);
        SetHSV(e.HSV);
    }

    private void ColorWheel_Paint(object sender, PaintEventArgs e)
    {
        // Depending on the circumstances, force a repaint of the color wheel passing different information.
        switch (changeType)
        {
            case ChangeStyle.HSV:
                myColorWheel.Draw(e.Graphics, hsv);
                break;

            case ChangeStyle.MouseMove:
            case ChangeStyle.None:
                myColorWheel.Draw(e.Graphics, selectedPoint);
                break;

            case ChangeStyle.RGB:
                myColorWheel.Draw(e.Graphics, rgb);
                break;
        }
    }

    private void CreateGradient()
    {
        // Create a new PathGradientBrush, supplying an array of points created by calling the GetPoints method.
        using PathGradientBrush pgb = new(GetPoints(radius: radius, new Point(radius, radius)));
        // Set the various properties. Note the SurroundColors property, which contains an array of points, in a one-to-one relationship with the points that created the gradient.
        pgb.CenterColor = Color.White;
        pgb.CenterPoint = new PointF(radius, radius);
        pgb.SurroundColors = GetColors();

        // Create a new bitmap containing the color wheel gradient, so the code only needs to do all this work once. Later code uses the bitmap rather than recreating the gradient.
        colorImage = new Bitmap(colorRectangle.Width, colorRectangle.Height, PixelFormat.Format32bppArgb);

        using Graphics newGraphics = Graphics.FromImage(colorImage);
        newGraphics.FillEllipse(pgb, 0, 0, colorRectangle.Width, colorRectangle.Height);
    }

    private static Color[] GetColors()
    {
        // Create an array of colorCount colors, looping through all the hues between 0 and 255, broken into colorCount intervals. HSV is particularly well-suited for this,
        // because the only value that changes as you create colors is the Hue.
        Color[] colors = new Color[colorCount];

        for (int i = 0; i <= colorCount - 1; i++)
        {
            colors[i] = ColorHandler.HSVtoColor((int)((double)(i * 255) / colorCount), 255, 255);
        }

        return colors;
    }

    private static Point[] GetPoints(double radius, Point centerPoint)
    {
        // Generate the array of points that describe the locations of the colorCount colors to be displayed on the color wheel.
        Point[] points = new Point[colorCount];

        for (int i = 0; i <= colorCount - 1; i++)
        {
            points[i] = GetPoint((double)(i * 360) / colorCount, radius, centerPoint);
        }

        return points;
    }

    private static Point GetPoint(double degrees, double radius, Point centerPoint)
    {
        // Given the center of a circle and its radius, along with the angle corresponding to the point, find the coordinates. In other words, conver  t from polar to rectangular coordinates.
        double radians = degrees / degreesPerRadian;

        return new Point((int)(centerPoint.X + Math.Floor(radius * Math.Cos(radians))),
            (int)(centerPoint.Y - Math.Floor(radius * Math.Sin(radians))));
    }

    protected void OnColorChanged(ColorHandler.RGB rgb, ColorHandler.HSV hsv)
    {
        ColorChangedEventArgs e = new() { RGB = rgb, HSV = hsv };
        ColorChanged?.Invoke(this, e);
    }

    public void Draw(Graphics g, ColorHandler.HSV hsv)
    {
        // Given HSV values, update the screen.
        this.g = g;
        this.hsv = hsv;
        CalcCoordsAndUpdate(this.hsv);
        UpdateDisplay();
    }

    public void Draw(Graphics g, ColorHandler.RGB rgb)
    {
        // Given RGB values, calculate HSV and then update the screen.
        this.g = g;
        hsv = ColorHandler.RGBtoHSV(rgb);
        CalcCoordsAndUpdate(hsv);
        UpdateDisplay();
    }

    public void Draw(Graphics g, Point mousePoint)
    {
        // You've moved the mouse. Now update the screen to match.
        double distance;
        int degrees;
        Point delta;
        Point newColorPoint;
        Point newBrightnessPoint;
        Point newPoint;

        // Keep track of the previous color pointer point, so you can put the mouse there in case the user has clicked outside the circle.
        newColorPoint = colorPoint;
        newBrightnessPoint = brightnessPoint;

        // Store this away for later use.
        this.g = g;

        if (currentState == MouseState.MouseUp)
        {
            if (!mousePoint.IsEmpty)
            {
                if (colorRegion.IsVisible(mousePoint))
                {
                    // Is the mouse point within the color circle? If so, you just clicked on the color wheel.
                    currentState = MouseState.ClickOnColor;
                }
                else if (brightnessRegion.IsVisible(mousePoint))
                {
                    // Is the mouse point within the brightness area? You clicked on the brightness area.
                    currentState = MouseState.ClickOnBrightness;
                }
                else
                {
                    // Clicked outside the color and the brightness regions. In that case, just put the pointers back where they were.
                    currentState = MouseState.ClickOutsideRegion;
                }
            }
        }

        switch (currentState)
        {
            case MouseState.ClickOnBrightness:
            case MouseState.DragInBrightness:
                // Calculate new color information based on the brightness, which may have changed.
                newPoint = mousePoint;
                if (newPoint.Y < brightnessMin)
                {
                    newPoint.Y = brightnessMin;
                }
                else if (newPoint.Y > brightnessMax)
                {
                    newPoint.Y = brightnessMax;
                }
                newBrightnessPoint = new Point(brightnessX, newPoint.Y);
                brightness = (int)((brightnessMax - newPoint.Y) * brightnessScaling);
                hsv = new ColorHandler.HSV(hsv.Hue, hsv.Saturation, brightness);
                rgb = ColorHandler.HSVtoRGB(hsv);
                break;

            case MouseState.ClickOnColor:
            case MouseState.DragInColor:
                // Calculate new color information based on selected color, which may have changed.
                newColorPoint = mousePoint;

                // Calculate x and y distance from the center, and then calculate the angle corresponding to the new location.
                delta = new Point(mousePoint.X - centerPoint.X, mousePoint.Y - centerPoint.Y);
                degrees = CalcDegrees(delta);

                // Calculate distance from the center to the new point as a fraction of the radius. Use your old friend, the Pythagorean theorem, to calculate this value.
                distance = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) / radius;

                if (currentState == MouseState.DragInColor)
                {
                    if (distance > 1)
                    {
                        // Mouse is down, and outside the circle, but you were previously dragging in the color circle. In that case, move the point to the edge of the
                        // circle at the correct angle.
                        distance = 1;
                        newColorPoint = GetPoint(degrees, radius, centerPoint);
                    }
                }

                // Calculate the new HSV and RGB values.
                hsv = new ColorHandler.HSV(degrees * 255 / 360, (int)(distance * 255), brightness);
                rgb = ColorHandler.HSVtoRGB(hsv);
                fullColor = ColorHandler.HSVtoColor(hsv.Hue, hsv.Saturation, 255);
                break;
        }
        selectedColor = ColorHandler.HSVtoColor(hsv);

        // Raise an event back to the parent form, so the form can update any UI it's using to display selected color values.
        OnColorChanged(rgb, hsv);

        // On the way out, set the new state.
        switch (currentState)
        {
            case MouseState.ClickOnBrightness:
                currentState = MouseState.DragInBrightness;
                break;

            case MouseState.ClickOnColor:
                currentState = MouseState.DragInColor;
                break;

            case MouseState.ClickOutsideRegion:
                currentState = MouseState.DragOutsideRegion;
                break;
        }

        // Store away the current points for next time.
        colorPoint = newColorPoint;
        brightnessPoint = newBrightnessPoint;

        // Draw the gradients and points.
        UpdateDisplay();
    }

    private Point CalcBrightnessPoint(int brightness)
    {
        // Take the value for brightness (0 to 255), scale to the scaling used in the brightness bar, then add the value to the bottom of the bar. return the correct point at which
        // to display the brightness pointer.
        return new Point(brightnessX,
            (int)(brightnessMax - brightness / brightnessScaling));
    }

    private void CalcCoordsAndUpdate(ColorHandler.HSV hsv)
    {
        // Convert color to real-world coordinates and then calculate the various points. hsv.Hue represents the degrees (0 to 360), hsv.Saturation represents the radius.
        // This procedure doesn't draw anything--it simply updates class-level variables. The UpdateDisplay procedure uses these values to update the screen.

        // Given the angle (hsv.Hue), and distance from the center (hsv.Saturation), and the center, calculate the point corresponding to the selected color, on the color wheel.
        colorPoint = GetPoint((double)hsv.Hue / 255 * 360,
            (double)hsv.Saturation / 255 * radius,
            centerPoint);

        // Given the brightness (hsv.Value), calculate the point corresponding to the brightness indicator.
        brightnessPoint = CalcBrightnessPoint(hsv.Value);

        // Store information about the selected color.
        brightness = hsv.Value;
        selectedColor = ColorHandler.HSVtoColor(hsv);
        rgb = ColorHandler.HSVtoRGB(hsv);

        // The full color is the same as hsv, except that the brightness is set to full (255). This is the top-most color in the brightness gradient.
        fullColor = ColorHandler.HSVtoColor(hsv.Hue, hsv.Saturation, 255);
    }

    private void DrawLinearGradient(Color topColor)
    {
        // Given the top color, draw a linear gradient ranging from black to the top color. Use the brightness rectangle as the area to fill.
        using LinearGradientBrush lgb =
                         new(rect: brightnessRectangle, color1: topColor,
                         color2: Color.Black, linearGradientMode: LinearGradientMode.Vertical);
        g.FillRectangle(lgb, brightnessRectangle);
    }

    private static int CalcDegrees(Point pt)
    {
        int degrees;

        if (pt.X == 0)
        {
            // The point is on the y-axis. Determine whether it's above or below the x-axis, and return the corresponding angle. Note that the orientation of the
            // y-coordinate is backwards. That is, A positive Y value indicates a point BELOW the x-axis.
            if (pt.Y > 0)
            {
                degrees = 270;
            }
            else
            {
                degrees = 90;
            }
        }
        else
        {
            // This value needs to be multiplied by -1 because the y-coordinate is opposite from the normal direction here. That is, a y-coordinate that's "higher" on
            // the form has a lower y-value, in this coordinate system. So everything's off by a factor of -1 when performing the ratio calculations.
            degrees = (int)(-Math.Atan((double)pt.Y / pt.X) * degreesPerRadian);

            // If the x-coordinate of the selected point is to the left of the center of the circle, you need to add 180 degrees to the angle. ArcTan only
            // gives you a value on the right-hand side of the circle.
            if (pt.X < 0)
            {
                degrees += 180;
            }

            // Ensure that the return value is between 0 and 360.
            degrees = (degrees + 360) % 360;
        }
        return degrees;
    }

    private void UpdateDisplay()
    {
        // Update the gradients, and place the pointers correctly based on colors and brightness.
        using Brush selectedBrush = new SolidBrush(selectedColor);

        // Draw the saved color wheel image.
        g.DrawImage(colorImage, colorRectangle);

        // Draw the "selected color" rectangle.
        g.FillRectangle(selectedBrush, selectedColorRectangle);

        // Draw the "brightness" rectangle.
        DrawLinearGradient(fullColor);

        // Draw the two pointers.
        DrawColorPointer(colorPoint);
        DrawBrightnessPointer(brightnessPoint);
    }

    private void DrawColorPointer(Point pt)
    {
        // Given a point, draw the color selector. The constant size represents half the width -- the square will be twice this value in width and height.
        const int size = 3;
        g.DrawRectangle(Pens.Black, pt.X - size, pt.Y - size, size * 2, size * 2);
    }

    private void DrawBrightnessPointer(Point pt)
    {
        // Draw a triangle for the brightness indicator that "points" at the provided point.
        const int height = 10;
        const int width = 7;

        Point[] points = new Point[3];
        points[0] = pt;
        points[1] = new Point(pt.X + width, pt.Y + height / 2);
        points[2] = new Point(pt.X + width, pt.Y - height / 2);
        g.FillPolygon(Brushes.Black, points);
    }

    private void BtnOK_Click(object sender, EventArgs e)
    {
        if (GlobalVar.SettingBackColor)
        {
            GlobalVar.BackColor = Color.FromArgb((int)NudRed.Value, (int)NudGreen.Value, (int)NudBlue.Value);
        }
        else
        {
            GlobalVar.FontColor = Color.FromArgb((int)NudRed.Value, (int)NudGreen.Value, (int)NudBlue.Value);
        }
        GlobalVar.SettingFontColor = false;
        GlobalVar.SettingBackColor = false;
        GlobalVar.UpdateColors();
        Close();
    }

    private void BtnCancel_Click(object sender, EventArgs e)
    {
        Close();
        GlobalVar.SettingBackColor = false;
        GlobalVar.SettingFontColor = false;
    }
}
