using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace DesktopShell.Forms;

/// <summary>
/// Summary description for ColorWheel.
/// </summary>
public class ColorWheel : Form
{
    internal Button btnCancel;
    internal Button btnOK;
    internal Label Label3;
    internal NumericUpDown nudSaturation;
    internal Label Label7;
    internal NumericUpDown nudBrightness;
    internal NumericUpDown nudRed;
    internal Panel pnlColor;
    internal Label Label6;
    internal Label Label1;
    internal Label Label5;
    internal Panel pnlSelectedColor;
    internal Panel pnlBrightness;
    internal NumericUpDown nudBlue;
    internal Label Label4;
    internal NumericUpDown nudGreen;
    internal Label Label2;
    internal NumericUpDown nudHue;

    private Rectangle colorRectangle;
    private Rectangle brightnessRectangle;
    private Rectangle selectedColorRectangle;
    private Point centerPoint;
    private readonly int radius;
    private readonly int brightnessX;
    private readonly double brightnessScaling;
    private const int COLOR_COUNT = 6 * 256;
    private const double DEGREES_PER_RADIAN = 180.0 / Math.PI;
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

    public ColorWheel(Rectangle _colorRectangle, Rectangle _brightnessRectangle, Rectangle _selectedColorRectangle)
    {
        using GraphicsPath path = new();
        // Store away locations for later use.
        colorRectangle = _colorRectangle;
        brightnessRectangle = _brightnessRectangle;
        selectedColorRectangle = _selectedColorRectangle;

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
        btnCancel = new Button();
        btnOK = new Button();
        Label3 = new Label();
        nudSaturation = new NumericUpDown();
        Label7 = new Label();
        nudBrightness = new NumericUpDown();
        nudRed = new NumericUpDown();
        pnlColor = new Panel();
        Label6 = new Label();
        Label1 = new Label();
        Label5 = new Label();
        pnlSelectedColor = new Panel();
        pnlBrightness = new Panel();
        nudBlue = new NumericUpDown();
        Label4 = new Label();
        nudGreen = new NumericUpDown();
        Label2 = new Label();
        nudHue = new NumericUpDown();
        ((System.ComponentModel.ISupportInitialize)(nudSaturation)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(nudBrightness)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(nudRed)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(nudBlue)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(nudGreen)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(nudHue)).BeginInit();
        SuspendLayout();
        //
        // btnCancel
        //
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnCancel.Location = new Point(192, 320);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(64, 24);
        btnCancel.TabIndex = 55;
        btnCancel.Text = "Cancel";
        btnCancel.Click += new EventHandler(BtnCancel_Click);
        //
        // btnOK
        //
        btnOK.DialogResult = DialogResult.OK;
        btnOK.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnOK.Location = new Point(120, 320);
        btnOK.Name = "btnOK";
        btnOK.Size = new Size(64, 24);
        btnOK.TabIndex = 54;
        btnOK.Text = "OK";
        btnOK.Click += new EventHandler(BtnOK_Click);
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
        // nudSaturation
        //
        nudSaturation.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        nudSaturation.Location = new Point(96, 256);
        nudSaturation.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
        nudSaturation.Name = "nudSaturation";
        nudSaturation.Size = new Size(48, 22);
        nudSaturation.TabIndex = 42;
        nudSaturation.TextChanged += new EventHandler(HandleTextChanged);
        nudSaturation.ValueChanged += new EventHandler(HandleHSVChange);
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
        // nudBrightness
        //
        nudBrightness.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        nudBrightness.Location = new Point(96, 280);
        nudBrightness.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
        nudBrightness.Name = "nudBrightness";
        nudBrightness.Size = new Size(48, 22);
        nudBrightness.TabIndex = 47;
        nudBrightness.TextChanged += new EventHandler(HandleTextChanged);
        nudBrightness.ValueChanged += new EventHandler(HandleHSVChange);
        //
        // nudRed
        //
        nudRed.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        nudRed.Location = new Point(208, 232);
        nudRed.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
        nudRed.Name = "nudRed";
        nudRed.Size = new Size(48, 22);
        nudRed.TabIndex = 38;
        nudRed.TextChanged += new EventHandler(HandleTextChanged);
        nudRed.ValueChanged += new EventHandler(HandleRGBChange);
        //
        // pnlColor
        //
        pnlColor.Location = new Point(8, 8);
        pnlColor.Name = "pnlColor";
        pnlColor.Size = new Size(176, 176);
        pnlColor.TabIndex = 51;
        pnlColor.Visible = false;
        pnlColor.MouseUp += new MouseEventHandler(FrmMain_MouseUp);
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
        // pnlSelectedColor
        //
        pnlSelectedColor.Location = new Point(208, 200);
        pnlSelectedColor.Name = "pnlSelectedColor";
        pnlSelectedColor.Size = new Size(48, 24);
        pnlSelectedColor.TabIndex = 53;
        pnlSelectedColor.Visible = false;
        //
        // pnlBrightness
        //
        pnlBrightness.Location = new Point(208, 8);
        pnlBrightness.Name = "pnlBrightness";
        pnlBrightness.Size = new Size(16, 176);
        pnlBrightness.TabIndex = 52;
        pnlBrightness.Visible = false;
        //
        // nudBlue
        //
        nudBlue.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        nudBlue.Location = new Point(208, 280);
        nudBlue.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
        nudBlue.Name = "nudBlue";
        nudBlue.Size = new Size(48, 22);
        nudBlue.TabIndex = 40;
        nudBlue.TextChanged += new EventHandler(HandleTextChanged);
        nudBlue.ValueChanged += new EventHandler(HandleRGBChange);
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
        // nudGreen
        //
        nudGreen.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        nudGreen.Location = new Point(208, 256);
        nudGreen.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
        nudGreen.Name = "nudGreen";
        nudGreen.Size = new Size(48, 22);
        nudGreen.TabIndex = 39;
        nudGreen.TextChanged += new EventHandler(HandleTextChanged);
        nudGreen.ValueChanged += new EventHandler(HandleRGBChange);
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
        // nudHue
        //
        nudHue.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        nudHue.Location = new Point(96, 232);
        nudHue.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
        nudHue.Name = "nudHue";
        nudHue.Size = new Size(48, 22);
        nudHue.TabIndex = 41;
        nudHue.TextChanged += new EventHandler(HandleTextChanged);
        nudHue.ValueChanged += new EventHandler(HandleHSVChange);
        //
        // ColorWheel
        //
        AutoScaleBaseSize = new Size(5, 13);
        ClientSize = new Size(264, 349);
        Controls.Add(btnCancel);
        Controls.Add(btnOK);
        Controls.Add(Label3);
        Controls.Add(nudSaturation);
        Controls.Add(Label7);
        Controls.Add(nudBrightness);
        Controls.Add(nudRed);
        Controls.Add(pnlColor);
        Controls.Add(Label6);
        Controls.Add(Label1);
        Controls.Add(Label5);
        Controls.Add(pnlSelectedColor);
        Controls.Add(pnlBrightness);
        Controls.Add(nudBlue);
        Controls.Add(Label4);
        Controls.Add(nudGreen);
        Controls.Add(Label2);
        Controls.Add(nudHue);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ColorWheel";
        ShowInTaskbar = false;
        Text = "Select Color";
        Load += new EventHandler(ColorWheel_Load);
        Paint += new PaintEventHandler(ColorWheel_Paint);
        MouseDown += new MouseEventHandler(HandleMouse);
        MouseMove += new MouseEventHandler(HandleMouse);
        MouseUp += new MouseEventHandler(FrmMain_MouseUp);
        ((System.ComponentModel.ISupportInitialize)(nudSaturation)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(nudBrightness)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(nudRed)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(nudBlue)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(nudGreen)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(nudHue)).EndInit();
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
    private ColorHandler.RGB RGB;
    private ColorHandler.HSV HSV;
    private bool isInUpdate = false;

    private void ColorWheel_Load(object sender, EventArgs e)
    {
        // Turn on double-buffering, so the form looks better.
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.DoubleBuffer, true);

        // These properties are set in design view, as well, but they have to be set to false in order for the Paint event to be able to display their contents.
        pnlSelectedColor.Visible = false;
        pnlBrightness.Visible = false;
        pnlColor.Visible = false;

        // Calculate the coordinates of the three required regions on the form.
        Rectangle SelectedColorRectangle = new(pnlSelectedColor.Location, pnlSelectedColor.Size);
        Rectangle BrightnessRectangle = new(pnlBrightness.Location, pnlBrightness.Size);
        Rectangle ColorRectangle = new(pnlColor.Location, pnlColor.Size);

        // Create the new ColorWheel class, indicating the locations of the color wheel itself, the brightness area, and the position of the selected color.
        myColorWheel = new ColorWheel(ColorRectangle, BrightnessRectangle, SelectedColorRectangle);
        myColorWheel.ColorChanged += new ColorChangedEventHandler(MyColorWheel_ColorChanged);

        /// TODO: I think this is where I need to do the conversion from hash color code to RGB HSV

        // Set the RGB and HSV values of the NumericUpDown controls.
        SetRGB(RGB);
        SetHSV(HSV);

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
            RGB = new ColorHandler.RGB((int)nudRed.Value, (int)nudGreen.Value, (int)nudBlue.Value);
            SetHSV(ColorHandler.RGBtoHSV(RGB));
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
            HSV = new ColorHandler.HSV((int)nudHue.Value, (int)nudSaturation.Value, (int)nudBrightness.Value);
            SetRGB(ColorHandler.HSVtoRGB(HSV));
            Invalidate();
        }
    }

    private void SetRGB(ColorHandler.RGB RGB)
    {
        // Update the RGB values on the form, but don't trigger the ValueChanged event of the form. The isInUpdate variable ensures that the event procedures exit without doing anything.
        isInUpdate = true;
        RefreshValue(nudRed, RGB.Red);
        RefreshValue(nudBlue, RGB.Blue);
        RefreshValue(nudGreen, RGB.Green);
        isInUpdate = false;
    }

    private void SetHSV(ColorHandler.HSV HSV)
    {
        // Update the HSV values on the form, but don't trigger the ValueChanged event of the form. The isInUpdate variable ensures that the event procedures exit without doing anything.
        isInUpdate = true;
        RefreshValue(nudHue, HSV.Hue);
        RefreshValue(nudSaturation, HSV.Saturation);
        RefreshValue(nudBrightness, HSV.value);
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
            RGB = new ColorHandler.RGB(value.R, value.G, value.B);
            HSV = ColorHandler.RGBtoHSV(RGB);
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
                myColorWheel.Draw(e.Graphics, HSV);
                break;

            case ChangeStyle.MouseMove:
            case ChangeStyle.None:
                myColorWheel.Draw(e.Graphics, selectedPoint);
                break;

            case ChangeStyle.RGB:
                myColorWheel.Draw(e.Graphics, RGB);
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
        // Create an array of COLOR_COUNT colors, looping through all the hues between 0 and 255, broken into COLOR_COUNT intervals. HSV is particularly well-suited for this,
        // because the only value that changes as you create colors is the Hue.
        Color[] Colors = new Color[COLOR_COUNT];

        for (int i = 0; i <= COLOR_COUNT - 1; i++)
        {
            Colors[i] = ColorHandler.HSVtoColor((int)((double)(i * 255) / COLOR_COUNT), 255, 255);
        }

        return Colors;
    }

    private static Point[] GetPoints(double radius, Point centerPoint)
    {
        // Generate the array of points that describe the locations of the COLOR_COUNT colors to be displayed on the color wheel.
        Point[] Points = new Point[COLOR_COUNT];

        for (int i = 0; i <= COLOR_COUNT - 1; i++)
        {
            Points[i] = GetPoint((double)(i * 360) / COLOR_COUNT, radius, centerPoint);
        }

        return Points;
    }

    private static Point GetPoint(double degrees, double radius, Point centerPoint)
    {
        // Given the center of a circle and its radius, along with the angle corresponding to the point, find the coordinates. In other words, conver  t from polar to rectangular coordinates.
        double radians = degrees / DEGREES_PER_RADIAN;

        return new Point((int)(centerPoint.X + Math.Floor(radius * Math.Cos(radians))),
            (int)(centerPoint.Y - Math.Floor(radius * Math.Sin(radians))));
    }

    protected void OnColorChanged(ColorHandler.RGB RGB, ColorHandler.HSV HSV)
    {
        ColorChangedEventArgs e = new() { RGB = RGB, HSV = HSV };
        ColorChanged(this, e);
    }

    public void Draw(Graphics g, ColorHandler.HSV HSV)
    {
        // Given HSV values, update the screen.
        this.g = g;
        this.HSV = HSV;
        CalcCoordsAndUpdate(this.HSV);
        UpdateDisplay();
    }

    public void Draw(Graphics g, ColorHandler.RGB RGB)
    {
        // Given RGB values, calculate HSV and then update the screen.
        this.g = g;
        HSV = ColorHandler.RGBtoHSV(RGB);
        CalcCoordsAndUpdate(HSV);
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
                HSV.value = brightness;
                RGB = ColorHandler.HSVtoRGB(HSV);
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
                HSV.Hue = degrees * 255 / 360;
                HSV.Saturation = (int)(distance * 255);
                HSV.value = brightness;
                RGB = ColorHandler.HSVtoRGB(HSV);
                fullColor = ColorHandler.HSVtoColor(HSV.Hue, HSV.Saturation, 255);
                break;
        }
        selectedColor = ColorHandler.HSVtoColor(HSV);

        // Raise an event back to the parent form, so the form can update any UI it's using to display selected color values.
        OnColorChanged(RGB, HSV);

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

    private void CalcCoordsAndUpdate(ColorHandler.HSV HSV)
    {
        // Convert color to real-world coordinates and then calculate the various points. HSV.Hue represents the degrees (0 to 360), HSV.Saturation represents the radius.
        // This procedure doesn't draw anything--it simply updates class-level variables. The UpdateDisplay procedure uses these values to update the screen.

        // Given the angle (HSV.Hue), and distance from the center (HSV.Saturation), and the center, calculate the point corresponding to the selected color, on the color wheel.
        colorPoint = GetPoint((double)HSV.Hue / 255 * 360,
            (double)HSV.Saturation / 255 * radius,
            centerPoint);

        // Given the brightness (HSV.value), calculate the point corresponding to the brightness indicator.
        brightnessPoint = CalcBrightnessPoint(HSV.value);

        // Store information about the selected color.
        brightness = HSV.value;
        selectedColor = ColorHandler.HSVtoColor(HSV);
        RGB = ColorHandler.HSVtoRGB(HSV);

        // The full color is the same as HSV, except that the brightness is set to full (255). This is the top-most color in the brightness gradient.
        fullColor = ColorHandler.HSVtoColor(HSV.Hue, HSV.Saturation, 255);
    }

    private void DrawLinearGradient(Color TopColor)
    {
        // Given the top color, draw a linear gradient ranging from black to the top color. Use the brightness rectangle as the area to fill.
        using LinearGradientBrush lgb =
                         new(rect: brightnessRectangle, color1: TopColor,
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
            degrees = (int)(-Math.Atan((double)pt.Y / pt.X) * DEGREES_PER_RADIAN);

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
        // Given a point, draw the color selector. The constant SIZE represents half the width -- the square will be twice this value in width and height.
        const int SIZE = 3;
        g.DrawRectangle(Pens.Black, pt.X - SIZE, pt.Y - SIZE, SIZE * 2, SIZE * 2);
    }

    private void DrawBrightnessPointer(Point pt)
    {
        // Draw a triangle for the brightness indicator that "points" at the provided point.
        const int HEIGHT = 10;
        const int WIDTH = 7;

        Point[] Points = new Point[3];
        Points[0] = pt;
        Points[1] = new Point(pt.X + WIDTH, pt.Y + HEIGHT / 2);
        Points[2] = new Point(pt.X + WIDTH, pt.Y - HEIGHT / 2);
        g.FillPolygon(Brushes.Black, Points);
    }

    private void BtnOK_Click(object sender, EventArgs e)
    {
        if (GlobalVar.SettingBackColor)
        {
            GlobalVar.BackColor = Color.FromArgb((int)nudRed.Value, (int)nudGreen.Value, (int)nudBlue.Value);
        }
        else
        {
            GlobalVar.FontColor = Color.FromArgb((int)nudRed.Value, (int)nudGreen.Value, (int)nudBlue.Value);
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
