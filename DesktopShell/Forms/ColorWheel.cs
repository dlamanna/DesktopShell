using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace DesktopShell.Forms
{
    /// <summary>
    /// Summary description for ColorWheel.
    /// </summary>
    public class ColorWheel : System.Windows.Forms.Form
    {
        internal System.Windows.Forms.Button btnCancel;
        internal System.Windows.Forms.Button btnOK;
        internal System.Windows.Forms.Label Label3;
        internal System.Windows.Forms.NumericUpDown nudSaturation;
        internal System.Windows.Forms.Label Label7;
        internal System.Windows.Forms.NumericUpDown nudBrightness;
        internal System.Windows.Forms.NumericUpDown nudRed;
        internal System.Windows.Forms.Panel pnlColor;
        internal System.Windows.Forms.Label Label6;
        internal System.Windows.Forms.Label Label1;
        internal System.Windows.Forms.Label Label5;
        internal System.Windows.Forms.Panel pnlSelectedColor;
        internal System.Windows.Forms.Panel pnlBrightness;
        internal System.Windows.Forms.NumericUpDown nudBlue;
        internal System.Windows.Forms.Label Label4;
        internal System.Windows.Forms.NumericUpDown nudGreen;
        internal System.Windows.Forms.Label Label2;
        internal System.Windows.Forms.NumericUpDown nudHue;

        private Rectangle colorRectangle;
        private Rectangle brightnessRectangle;
        private Rectangle selectedColorRectangle;
        private Point centerPoint;
        private int radius;
        private int brightnessX;
        private double brightnessScaling;
        private const int COLOR_COUNT = 6 * 256;
        private const double DEGREES_PER_RADIAN = 180.0 / Math.PI;
        private Point colorPoint;
        private Point brightnessPoint;
        private Graphics g;
        private Region colorRegion;
        private Region brightnessRegion;
        private Bitmap colorImage;
        private int brightness;
        private int brightnessMin;
        private int brightnessMax;
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

        private System.ComponentModel.Container components = null;

        public ColorWheel()
        {
            InitializeComponent();
            GlobalVar.colorWheelInstance = this;
        }

        public ColorWheel(Rectangle colorRectangle, Rectangle brightnessRectangle, Rectangle selectedColorRectangle)
        {
            using(GraphicsPath path = new GraphicsPath()) {
                // Store away locations for later use.
                this.colorRectangle = colorRectangle;
                this.brightnessRectangle = brightnessRectangle;
                this.selectedColorRectangle = selectedColorRectangle;

                // Calculate the center of the circle. Start with the location, then offset the point by the radius. Use the smaller of the width and height of
                // the colorRectangle value.
                this.radius = (int)Math.Min(colorRectangle.Width, colorRectangle.Height) / 2;
                this.centerPoint = colorRectangle.Location;
                this.centerPoint.Offset(radius, radius);

                // Start the pointer in the center.
                this.colorPoint = this.centerPoint;

                // Create a region corresponding to the color circle. Code uses this later to determine if a specified point is within the region, using the IsVisible method.
                path.AddEllipse(colorRectangle);
                colorRegion = new Region(path);

                // set the range for the brightness selector.
                this.brightnessMin = this.brightnessRectangle.Top;
                this.brightnessMax = this.brightnessRectangle.Bottom;

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
        }

        protected override void Dispose(bool disposing)
        {
            Console.WriteLine("!!! Disposing ColorWheel");
            if(disposing) {
                // Dispose of graphic resources
                if(colorImage != null) {
                    colorImage.Dispose();
                }

                if(colorRegion != null) {
                    colorRegion.Dispose();
                }

                if(brightnessRegion != null) {
                    brightnessRegion.Dispose();
                }

                if(g != null) {
                    g.Dispose();
                }

                if(components != null) {
                    components.Dispose();
                }
            }

            GlobalVar.colorWheelInstance = null;
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.Label3 = new System.Windows.Forms.Label();
            this.nudSaturation = new System.Windows.Forms.NumericUpDown();
            this.Label7 = new System.Windows.Forms.Label();
            this.nudBrightness = new System.Windows.Forms.NumericUpDown();
            this.nudRed = new System.Windows.Forms.NumericUpDown();
            this.pnlColor = new System.Windows.Forms.Panel();
            this.Label6 = new System.Windows.Forms.Label();
            this.Label1 = new System.Windows.Forms.Label();
            this.Label5 = new System.Windows.Forms.Label();
            this.pnlSelectedColor = new System.Windows.Forms.Panel();
            this.pnlBrightness = new System.Windows.Forms.Panel();
            this.nudBlue = new System.Windows.Forms.NumericUpDown();
            this.Label4 = new System.Windows.Forms.Label();
            this.nudGreen = new System.Windows.Forms.NumericUpDown();
            this.Label2 = new System.Windows.Forms.Label();
            this.nudHue = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.nudSaturation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBrightness)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRed)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBlue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudGreen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHue)).BeginInit();
            this.SuspendLayout();
            //
            // btnCancel
            //
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancel.Location = new System.Drawing.Point(192, 320);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(64, 24);
            this.btnCancel.TabIndex = 55;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // btnOK
            //
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOK.Location = new System.Drawing.Point(120, 320);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(64, 24);
            this.btnOK.TabIndex = 54;
            this.btnOK.Text = "OK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            //
            // Label3
            //
            this.Label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label3.Location = new System.Drawing.Point(152, 280);
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(48, 23);
            this.Label3.TabIndex = 45;
            this.Label3.Text = "Blue:";
            this.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // nudSaturation
            //
            this.nudSaturation.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nudSaturation.Location = new System.Drawing.Point(96, 256);
            this.nudSaturation.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nudSaturation.Name = "nudSaturation";
            this.nudSaturation.Size = new System.Drawing.Size(48, 22);
            this.nudSaturation.TabIndex = 42;
            this.nudSaturation.TextChanged += new System.EventHandler(this.HandleTextChanged);
            this.nudSaturation.ValueChanged += new System.EventHandler(this.HandleHSVChange);
            //
            // Label7
            //
            this.Label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label7.Location = new System.Drawing.Point(16, 280);
            this.Label7.Name = "Label7";
            this.Label7.Size = new System.Drawing.Size(72, 23);
            this.Label7.TabIndex = 50;
            this.Label7.Text = "Brightness:";
            this.Label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // nudBrightness
            //
            this.nudBrightness.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nudBrightness.Location = new System.Drawing.Point(96, 280);
            this.nudBrightness.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nudBrightness.Name = "nudBrightness";
            this.nudBrightness.Size = new System.Drawing.Size(48, 22);
            this.nudBrightness.TabIndex = 47;
            this.nudBrightness.TextChanged += new System.EventHandler(this.HandleTextChanged);
            this.nudBrightness.ValueChanged += new System.EventHandler(this.HandleHSVChange);
            //
            // nudRed
            //
            this.nudRed.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nudRed.Location = new System.Drawing.Point(208, 232);
            this.nudRed.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nudRed.Name = "nudRed";
            this.nudRed.Size = new System.Drawing.Size(48, 22);
            this.nudRed.TabIndex = 38;
            this.nudRed.TextChanged += new System.EventHandler(this.HandleTextChanged);
            this.nudRed.ValueChanged += new System.EventHandler(this.HandleRGBChange);
            //
            // pnlColor
            //
            this.pnlColor.Location = new System.Drawing.Point(8, 8);
            this.pnlColor.Name = "pnlColor";
            this.pnlColor.Size = new System.Drawing.Size(176, 176);
            this.pnlColor.TabIndex = 51;
            this.pnlColor.Visible = false;
            this.pnlColor.MouseUp += new System.Windows.Forms.MouseEventHandler(this.frmMain_MouseUp);
            //
            // Label6
            //
            this.Label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label6.Location = new System.Drawing.Point(16, 256);
            this.Label6.Name = "Label6";
            this.Label6.Size = new System.Drawing.Size(72, 23);
            this.Label6.TabIndex = 49;
            this.Label6.Text = "Saturation:";
            this.Label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // Label1
            //
            this.Label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label1.Location = new System.Drawing.Point(152, 232);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(48, 23);
            this.Label1.TabIndex = 43;
            this.Label1.Text = "Red:";
            this.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // Label5
            //
            this.Label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label5.Location = new System.Drawing.Point(16, 232);
            this.Label5.Name = "Label5";
            this.Label5.Size = new System.Drawing.Size(72, 23);
            this.Label5.TabIndex = 48;
            this.Label5.Text = "Hue:";
            this.Label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // pnlSelectedColor
            //
            this.pnlSelectedColor.Location = new System.Drawing.Point(208, 200);
            this.pnlSelectedColor.Name = "pnlSelectedColor";
            this.pnlSelectedColor.Size = new System.Drawing.Size(48, 24);
            this.pnlSelectedColor.TabIndex = 53;
            this.pnlSelectedColor.Visible = false;
            //
            // pnlBrightness
            //
            this.pnlBrightness.Location = new System.Drawing.Point(208, 8);
            this.pnlBrightness.Name = "pnlBrightness";
            this.pnlBrightness.Size = new System.Drawing.Size(16, 176);
            this.pnlBrightness.TabIndex = 52;
            this.pnlBrightness.Visible = false;
            //
            // nudBlue
            //
            this.nudBlue.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nudBlue.Location = new System.Drawing.Point(208, 280);
            this.nudBlue.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nudBlue.Name = "nudBlue";
            this.nudBlue.Size = new System.Drawing.Size(48, 22);
            this.nudBlue.TabIndex = 40;
            this.nudBlue.TextChanged += new System.EventHandler(this.HandleTextChanged);
            this.nudBlue.ValueChanged += new System.EventHandler(this.HandleRGBChange);
            //
            // Label4
            //
            this.Label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label4.Location = new System.Drawing.Point(152, 200);
            this.Label4.Name = "Label4";
            this.Label4.Size = new System.Drawing.Size(48, 24);
            this.Label4.TabIndex = 46;
            this.Label4.Text = "Color:";
            this.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // nudGreen
            //
            this.nudGreen.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nudGreen.Location = new System.Drawing.Point(208, 256);
            this.nudGreen.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nudGreen.Name = "nudGreen";
            this.nudGreen.Size = new System.Drawing.Size(48, 22);
            this.nudGreen.TabIndex = 39;
            this.nudGreen.TextChanged += new System.EventHandler(this.HandleTextChanged);
            this.nudGreen.ValueChanged += new System.EventHandler(this.HandleRGBChange);
            //
            // Label2
            //
            this.Label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label2.Location = new System.Drawing.Point(152, 256);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(48, 23);
            this.Label2.TabIndex = 44;
            this.Label2.Text = "Green:";
            this.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // nudHue
            //
            this.nudHue.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nudHue.Location = new System.Drawing.Point(96, 232);
            this.nudHue.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nudHue.Name = "nudHue";
            this.nudHue.Size = new System.Drawing.Size(48, 22);
            this.nudHue.TabIndex = 41;
            this.nudHue.TextChanged += new System.EventHandler(this.HandleTextChanged);
            this.nudHue.ValueChanged += new System.EventHandler(this.HandleHSVChange);
            //
            // ColorWheel
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(264, 349);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.Label3);
            this.Controls.Add(this.nudSaturation);
            this.Controls.Add(this.Label7);
            this.Controls.Add(this.nudBrightness);
            this.Controls.Add(this.nudRed);
            this.Controls.Add(this.pnlColor);
            this.Controls.Add(this.Label6);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.Label5);
            this.Controls.Add(this.pnlSelectedColor);
            this.Controls.Add(this.pnlBrightness);
            this.Controls.Add(this.nudBlue);
            this.Controls.Add(this.Label4);
            this.Controls.Add(this.nudGreen);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.nudHue);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ColorWheel";
            this.ShowInTaskbar = false;
            this.Text = "Select Color";
            this.Load += new System.EventHandler(this.ColorWheel_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.ColorWheel_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.HandleMouse);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.HandleMouse);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.frmMain_MouseUp);
            ((System.ComponentModel.ISupportInitialize)(this.nudSaturation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBrightness)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRed)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBlue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudGreen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHue)).EndInit();
            this.ResumeLayout(false);
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

        private void ColorWheel_Load(object sender, System.EventArgs e)
        {
            // Turn on double-buffering, so the form looks better.
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.DoubleBuffer, true);

            // These properties are set in design view, as well, but they have to be set to false in order for the Paint event to be able to display their contents.
            pnlSelectedColor.Visible = false;
            pnlBrightness.Visible = false;
            pnlColor.Visible = false;

            // Calculate the coordinates of the three required regions on the form.
            Rectangle SelectedColorRectangle = new Rectangle(pnlSelectedColor.Location, pnlSelectedColor.Size);
            Rectangle BrightnessRectangle = new Rectangle(pnlBrightness.Location, pnlBrightness.Size);
            Rectangle ColorRectangle = new Rectangle(pnlColor.Location, pnlColor.Size);

            // Create the new ColorWheel class, indicating the locations of the color wheel itself, the brightness area, and the position of the selected color.
            myColorWheel = new ColorWheel(ColorRectangle, BrightnessRectangle, SelectedColorRectangle);
            myColorWheel.ColorChanged += new ColorWheel.ColorChangedEventHandler(this.myColorWheel_ColorChanged);

            // Set the RGB and HSV values of the NumericUpDown controls.
            SetRGB(RGB);
            SetHSV(HSV);

            this.Location = Cursor.Position;
        }

        private void HandleMouse(object sender, MouseEventArgs e)
        {
            // If you have the left mouse button down, then update the selectedPoint value and force a repaint of the color wheel.
            if(e.Button == MouseButtons.Left) {
                changeType = ChangeStyle.MouseMove;
                selectedPoint = new Point(e.X, e.Y);
                this.Invalidate();
            }
        }

        private void frmMain_MouseUp(object sender, MouseEventArgs e)
        {
            myColorWheel.SetMouseUp();
            changeType = ChangeStyle.None;
        }

        public void SetMouseUp()
        {
            // Indicate that the user has released the mouse.
            currentState = MouseState.MouseUp;
        }

        private void HandleRGBChange(object sender, System.EventArgs e)
        {
            // If the R, G, or B values change, use this code to update the HSV values and invalidate the color wheel (so it updates the pointers). Check the isInUpdate flag to avoid recursive events
            // when you update the NumericUpdownControls.
            if(!isInUpdate) {
                changeType = ChangeStyle.RGB;
                RGB = new ColorHandler.RGB((int)nudRed.Value, (int)nudGreen.Value, (int)nudBlue.Value);
                SetHSV(ColorHandler.RGBtoHSV(RGB));
                this.Invalidate();
            }
        }

        private void HandleHSVChange(object sender, EventArgs e)
        {
            // If the H, S, or V values change, use this code to update the RGB values and invalidate the color wheel (so it updates the pointers). Check the isInUpdate flag to avoid recursive events
            // when you update the NumericUpdownControls.
            if(!isInUpdate) {
                changeType = ChangeStyle.HSV;
                HSV = new ColorHandler.HSV((int)(nudHue.Value), (int)(nudSaturation.Value), (int)(nudBrightness.Value));
                SetRGB(ColorHandler.HSVtoRGB(HSV));
                this.Invalidate();
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

        private void HandleTextChanged(object sender, System.EventArgs e)
        {
            // This step works around a bug -- unless you actively retrieve the value, the min and max settings for the control aren't honored when you type text. This may be fixed in the 1.1 version, but in VS.NET 1.0, this
            // step is required.
            Decimal x = ((NumericUpDown)sender).Value;
        }

        private void RefreshValue(NumericUpDown nud, int value)
        {
            // Update the value of the NumericUpDown control, if the value is different than the current value. Refresh the control, causing an immediate repaint.
            if(nud.Value != value) {
                nud.Value = value;
                nud.Refresh();
            }
        }

        public Color Color
        {
            // Get or set the color to be displayed in the color wheel.
            get => myColorWheel.Color;

            set {
                // Indicate the color change type. Either RGB or HSV will cause the color wheel to update the position of the pointer.
                changeType = ChangeStyle.RGB;
                RGB = new ColorHandler.RGB(value.R, value.G, value.B);
                HSV = ColorHandler.RGBtoHSV(RGB);
            }
        }

        private void myColorWheel_ColorChanged(object sender, ColorChangedEventArgs e)
        {
            SetRGB(e.RGB);
            SetHSV(e.HSV);
        }

        private void ColorWheel_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            // Depending on the circumstances, force a repaint of the color wheel passing different information.
            switch(changeType) {
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
            using(PathGradientBrush pgb = new PathGradientBrush(GetPoints(radius, new Point(radius, radius)))) {
                // Set the various properties. Note the SurroundColors property, which contains an array of points, in a one-to-one relationship with the points that created the gradient.
                pgb.CenterColor = Color.White;
                pgb.CenterPoint = new PointF(radius, radius);
                pgb.SurroundColors = GetColors();

                // Create a new bitmap containing the color wheel gradient, so the code only needs to do all this work once. Later code uses the bitmap rather than recreating the gradient.
                colorImage = new Bitmap(
                    colorRectangle.Width, colorRectangle.Height,
                    PixelFormat.Format32bppArgb);

                using(Graphics newGraphics =
                                 Graphics.FromImage(colorImage)) {
                    newGraphics.FillEllipse(pgb, 0, 0,
                        colorRectangle.Width, colorRectangle.Height);
                }
            }
        }

        private Color[] GetColors()
        {
            // Create an array of COLOR_COUNT colors, looping through all the hues between 0 and 255, broken into COLOR_COUNT intervals. HSV is particularly well-suited for this,
            // because the only value that changes as you create colors is the Hue.
            Color[] Colors = new Color[COLOR_COUNT];

            for(int i = 0; i <= COLOR_COUNT - 1; i++) {
                Colors[i] = ColorHandler.HSVtoColor((int)((double)(i * 255) / COLOR_COUNT), 255, 255);
            }

            return Colors;
        }

        private Point[] GetPoints(double radius, Point centerPoint)
        {
            // Generate the array of points that describe the locations of the COLOR_COUNT colors to be displayed on the color wheel.
            Point[] Points = new Point[COLOR_COUNT];

            for(int i = 0; i <= COLOR_COUNT - 1; i++) {
                Points[i] = GetPoint((double)(i * 360) / COLOR_COUNT, radius, centerPoint);
            }

            return Points;
        }

        private Point GetPoint(double degrees, double radius, Point centerPoint)
        {
            // Given the center of a circle and its radius, along with the angle corresponding to the point, find the coordinates. In other words, conver  t from polar to rectangular coordinates.
            double radians = degrees / DEGREES_PER_RADIAN;

            return new Point((int)(centerPoint.X + Math.Floor(radius * Math.Cos(radians))),
                (int)(centerPoint.Y - Math.Floor(radius * Math.Sin(radians))));
        }

        protected void OnColorChanged(ColorHandler.RGB RGB, ColorHandler.HSV HSV)
        {
            ColorChangedEventArgs e = new ColorChangedEventArgs(RGB, HSV);
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
            this.HSV = ColorHandler.RGBtoHSV(RGB);
            CalcCoordsAndUpdate(this.HSV);
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

            if(currentState == MouseState.MouseUp) {
                if(!mousePoint.IsEmpty) {
                    if(colorRegion.IsVisible(mousePoint)) {
                        // Is the mouse point within the color circle? If so, you just clicked on the color wheel.
                        currentState = MouseState.ClickOnColor;
                    }
                    else if(brightnessRegion.IsVisible(mousePoint)) {
                        // Is the mouse point within the brightness area? You clicked on the brightness area.
                        currentState = MouseState.ClickOnBrightness;
                    }
                    else {
                        // Clicked outside the color and the brightness regions. In that case, just put the pointers back where they were.
                        currentState = MouseState.ClickOutsideRegion;
                    }
                }
            }

            switch(currentState) {
                case MouseState.ClickOnBrightness:
                case MouseState.DragInBrightness:
                    // Calculate new color information based on the brightness, which may have changed.
                    newPoint = mousePoint;
                    if(newPoint.Y < brightnessMin) {
                        newPoint.Y = brightnessMin;
                    }
                    else if(newPoint.Y > brightnessMax) {
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

                    if(currentState == MouseState.DragInColor) {
                        if(distance > 1) {
                            // Mouse is down, and outside the circle, but you were previously dragging in the color circle. In that case, move the point to the edge of the
                            // circle at the correct angle.
                            distance = 1;
                            newColorPoint = GetPoint(degrees, radius, centerPoint);
                        }
                    }

                    // Calculate the new HSV and RGB values.
                    HSV.Hue = (int)(degrees * 255 / 360);
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
            switch(currentState) {
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
            using(LinearGradientBrush lgb =
                             new LinearGradientBrush(brightnessRectangle, TopColor,
                             Color.Black, LinearGradientMode.Vertical)) {
                g.FillRectangle(lgb, brightnessRectangle);
            }
        }

        private int CalcDegrees(Point pt)
        {
            int degrees;

            if(pt.X == 0) {
                // The point is on the y-axis. Determine whether it's above or below the x-axis, and return the corresponding angle. Note that the orientation of the
                // y-coordinate is backwards. That is, A positive Y value indicates a point BELOW the x-axis.
                if(pt.Y > 0) {
                    degrees = 270;
                }
                else {
                    degrees = 90;
                }
            }
            else {
                // This value needs to be multiplied by -1 because the y-coordinate is opposite from the normal direction here. That is, a y-coordinate that's "higher" on
                // the form has a lower y-value, in this coordinate system. So everything's off by a factor of -1 when performing the ratio calculations.
                degrees = (int)(-Math.Atan((double)pt.Y / pt.X) * DEGREES_PER_RADIAN);

                // If the x-coordinate of the selected point is to the left of the center of the circle, you need to add 180 degrees to the angle. ArcTan only
                // gives you a value on the right-hand side of the circle.
                if(pt.X < 0) {
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

            using(Brush selectedBrush = new SolidBrush(selectedColor)) {
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
        }

        private void DrawColorPointer(Point pt)
        {
            // Given a point, draw the color selector. The constant SIZE represents half the width -- the square will be twice this value in width and height.
            const int SIZE = 3;
            g.DrawRectangle(Pens.Black,
                pt.X - SIZE, pt.Y - SIZE, SIZE * 2, SIZE * 2);
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

        private void btnOK_Click(object sender, EventArgs e)
        {
            if(GlobalVar.settingBackColor) {
                GlobalVar.backColor = Color.FromArgb((int)nudRed.Value, (int)nudGreen.Value, (int)nudBlue.Value);
            }
            else {
                GlobalVar.fontColor = Color.FromArgb((int)nudRed.Value, (int)nudGreen.Value, (int)nudBlue.Value);
            }
            GlobalVar.settingFontColor = false;
            GlobalVar.settingBackColor = false;
            GlobalVar.updateColors();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
            GlobalVar.settingBackColor = false;
            GlobalVar.settingFontColor = false;
        }
    }
}