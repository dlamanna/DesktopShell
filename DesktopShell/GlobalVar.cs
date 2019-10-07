using DesktopShell.Forms;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace DesktopShell
{
    [PermissionSetAttribute(SecurityAction.LinkDemand, Name = "FullTrust")]
    public class GlobalVar
    {
        public static System.Windows.Forms.Timer hourlyChime;
        public static Shell shellInstance = null;
        public static ConfigForm configInstance = null;
        public static ChoiceForm choiceInstance = null;
        public static ScreenSelectorForm screenSelectorInstance = null;
        public static ColorWheel colorWheelInstance = null;
        public static ArrayList fileChoices = new ArrayList();
        public static ArrayList dropDownRects = new ArrayList();
        public static Color backColor;
        public static Color fontColor;
        public static Boolean settingFontColor = false;
        public static Boolean settingBackColor = false;
        public static string searchType = "";

        // FilePath Section
        public static string[] deletePaths = { @"C:\automount.bat", @"C:\keyk", @"C:\keye", @"C:\keyd", @"C:\keyx" };
        public static string desktopShellFolderPath = @"D:\Program Files (x86)\DesktopShell";
        public static string desktopShellPath = @"C:\Users\phuzE\Dropbox\Programming\DesktopShell\DesktopShell.sln";
        public static string desktopShellReleasePath = @"C:\Users\phuzE\Dropbox\Programming\DesktopShell\DesktopShell\bin\Release";

        // Form Bounds
        public static int leftBound;
        public static int rightBound;
        public static int bottomBound;
        public static int topBound;

        private static IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static uint SWP_NOSIZE = 0x0001;
        private static uint SWP_NOZORDER = 0x0004;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int W, int H, uint uFlags);

        // Global Functions
        public static string GetSetting(int line)
        {
            using(var sr = new StreamReader("settings.ini")) {
                for(int i = 1; i < line; i++) {
                    sr.ReadLine();
                }

                return sr.ReadLine();
            }
        }
        public static void SetSetting(int line, string settingChange)
        {
            line -= 1;
            string[] tempLines = File.ReadAllLines("settings.ini");
            tempLines[line] = settingChange;

            foreach(string s in tempLines) {
                File.WriteAllLines("settings.ini", tempLines);
            }
        }
        public static void Run(string path, string arguments)
        {
            Process p = new Process();
            try {
                p.StartInfo.Arguments = arguments;
                p.StartInfo.FileName = path;
                p.Start();
            }
            catch { Process.Start("Bin\\ToolTipper.exe", "Error " + path); }


            Point curPos = Cursor.Position;
            Screen curScreen = Screen.FromPoint(curPos);

            // temp hack for now, fix later
            if(curScreen.Bounds.Width <= 1025) {
                int numIncrements = 0;
                int numSecondsUntilTimeout = 10;
                int increment = 50;
                int numMaxIncrements = ((numSecondsUntilTimeout * 1000) / increment);
                bool timeout = false;
                do {
                    p.Refresh();
                    Thread.Sleep(increment);
                    numIncrements++;

                    if(numIncrements == numMaxIncrements) {
                        GlobalVar.log("### Timeout getting process handle to move screens");
                        timeout = true;
                    }
                    else if(p.MainWindowHandle != (IntPtr)0) {
                        GlobalVar.log("&&& Moved process in: " + (increment * numIncrements) + "ms");
                    }
                } while(numIncrements < numMaxIncrements && p.MainWindowHandle == (IntPtr)0);

                if(!timeout) {
                    try {
                        IntPtr hWnd = p.MainWindowHandle;
                        if(!SetWindowPos(hWnd, (IntPtr)null, curScreen.WorkingArea.Left, curScreen.WorkingArea.Top, 0, 0, SWP_NOSIZE | SWP_NOZORDER)) {
                            throw new Win32Exception();
                        }
                    }
                    catch { /*Process.Start("Bin\\ToolTipper.exe", "Error " + path);*/ }
                }
            }
        }
        public static void Run(string path)
        {
            Run(path, "");
            /*try { Process.Start(path); }
            catch { Process.Start("Bin\\ToolTipper.exe", "Error " + path); }*/
        }
        public static void initDropDownRects(object sender)
        {
            dropDownRects.Clear();
            for(int i = 0; i < Screen.AllScreens.Length; i++) {
                if(Properties.Settings.multiscreenEnabled[i]) {
                    Screen s = Screen.AllScreens[i];
                    Size shellSize = ((Shell)sender).ClientSize;
                    int pointX = s.WorkingArea.Left + ((s.WorkingArea.Width / 2) - shellSize.Width / 2);
                    int pointY = s.WorkingArea.Top + shellSize.Height;
                    Point rectPoint = new Point(pointX, pointY);

                    Rectangle tempRect = new Rectangle(rectPoint, shellSize);
                    dropDownRects.Add(tempRect);
                }
                else {
                    continue;
                }
            }
        }
        public static Label[] populateLabels()
        {
            Regex extension = new Regex(".([a-z]|[A-Z]){3,4}$");
            ArrayList tempArray = new ArrayList();
            int fileCount = GlobalVar.fileChoices.Count;
            for(int i = 0; i < fileCount; i++) {
                Label tempLabel = new System.Windows.Forms.Label();
                tempLabel.BackColor = Properties.Settings.backgroundColor;
                tempLabel.ForeColor = Properties.Settings.foregroundColor;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F);
                tempLabel.Location = new System.Drawing.Point(10, (i * 18) + 20);
                tempLabel.Size = new System.Drawing.Size(350, 18);
                if(searchType == "Movie") {
                    tempLabel.Text = "• " + ((System.IO.FileInfo)fileChoices[i]).Name;
                }
                else {
                    tempLabel.Text = "• " + extension.Replace(((System.IO.FileInfo)GlobalVar.fileChoices[i]).Name, "");
                }

                tempArray.Add(tempLabel);
            }
            return (Label[])tempArray.ToArray(typeof(Label));
        }
        public static void setBounds(Form obj)
        {
            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
            int widthAdder = GlobalVar.calculateWidth();
            int heightDiff = screens[Screen.AllScreens.Length - 1].WorkingArea.Top;

            if(obj.Name == "Shell") {
                GlobalVar.topBound = heightDiff;
                GlobalVar.leftBound = widthAdder - (obj.Size.Width / 2);
                GlobalVar.rightBound = widthAdder + (obj.Size.Width / 2);
                GlobalVar.bottomBound = obj.Size.Height + heightDiff;
            }
            else {
                heightDiff += shellInstance.Size.Height;
            }

            obj.Location = new System.Drawing.Point(widthAdder - (obj.Size.Width / 2), 1 + heightDiff);
            //log("!!! Top bound: " + heightDiff);
        }
        public static void setCentered(Screen screen, Form obj)
        {
            int heightDiff = screen.Bounds.Top;
            int widthAdder;
            if(screen.Bounds.Left > 0) {
                widthAdder = screen.Bounds.Left + ((Math.Abs(screen.Bounds.Right) - Math.Abs(screen.Bounds.Left)) / 2) - (obj.Size.Width / 2);
            }
            else {
                widthAdder = ((Math.Abs(screen.Bounds.Right) - Math.Abs(screen.Bounds.Left)) / 2) - (obj.Size.Width / 2);
            }

            //GlobalVar.toolTip("setCentered", "obj.Name: " + obj.Name);
            if(obj.Name == "Shell") {
                GlobalVar.topBound = heightDiff;
                GlobalVar.leftBound = widthAdder;
                GlobalVar.rightBound = widthAdder + obj.Size.Width;
                GlobalVar.bottomBound = heightDiff + obj.Size.Height;
            }
            else {
                heightDiff += shellInstance.Size.Height;
            }

            obj.Location = new System.Drawing.Point(widthAdder, 1 + heightDiff - 20);
            //log("!!! Top bound: " + heightDiff);
            //GlobalVar.toolTip("setCentered", "Setting Location: " + obj.Location + ", Right-Left = " + Math.Abs(screen.Bounds.Right) + " - " + Math.Abs(screen.Bounds.Left) );
        }
        public static void toolTip(String title, String body)
        {
            Process.Start("Bin\\ToolTipper.exe", title + " " + body);
        }
        public static int calculateWidth()
        {
            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
            int widthAdder = 0;

            //Getting/Setting Position on screen
            foreach(Screen s in Screen.AllScreens) {
                if(Screen.AllScreens.Count() == 3) {
                    if(s.Bounds.Width == 1024) {
                        continue;
                    }
                }
                if(s == screens[Screen.AllScreens.Count() - 1]) {
                    widthAdder += (s.Bounds.Width / 2);
                }
                else {
                    widthAdder += s.Bounds.Width;
                }
            }

            log("!!! WidthAdder: " + widthAdder);

            return widthAdder;
        }
        public static void updateColors()
        {
            Properties.Settings.backgroundColor = backColor;
            Properties.Settings.foregroundColor = fontColor;
            shellInstance.changeBackgroundColor();
            shellInstance.changeFontColor();
            log("Changing Colors: (" + backColor.Name + ")\t(" + fontColor.Name + ")");
        }
        public static void log(String logOutput)
        {
            String logPath = "DesktopShell.log";
            using(FileStream fs = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read))
            using(StreamWriter w = new StreamWriter(fs)) {
                w.WriteLine("{0}:\t{1}", DateTime.Now.ToString("HH:mm:ss.fff"), logOutput);
            }
        }
        public static void resetLog()
        {
            using(File.Create("DesktopShell.log")) { };
        }
    }
}
