using DesktopShell.Forms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
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
        public static ArrayList fileChoices = new ArrayList();
        public static string searchType = "";

        // FilePath Section
        public static string[] deletePaths = { @"C:\automount.bat", @"C:\keyk", @"C:\keye", @"C:\keyd", @"C:\keyx" };
        public static string desktopShellFolderPath = @"D:\Program Files (x86)\DesktopShell";
        public static string desktopShellPath = @"C:\Users\phuzE\Dropbox\Programming\DesktopShell\DesktopShell.sln";
        public static string desktopShellReleasePath = @"C:\Users\phuzE\Dropbox\Programming\DesktopShell\DesktopShell\bin\Release";
        public static string[] pronPaths = { @"K:\Blackangel" };
        public static string vcplusplusPath = @"D:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\WDExpress.exe";
        public static string vcsPath = @"D:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\WDExpress.exe";

        // Form Bounds
        public static int leftBound;
        public static int rightBound;
        public static int bottomBound;
        public static int topBound;

        // Global Functions
        public static string GetSetting(int line)
        {
            using (var sr = new StreamReader("settings.ini"))
            {
                for (int i = 1; i < line; i++) sr.ReadLine();
                return sr.ReadLine();
            }
        }
        public static void SetSetting(int line, string settingChange)
        {
            line -= 1;
            string[] tempLines = File.ReadAllLines("settings.ini");
            tempLines[line] = settingChange;

            foreach (string s in tempLines)
            {
                File.WriteAllLines("settings.ini", tempLines);
            }
        }
        public static void Run(string path, string arguments)
        {
            try { Process.Start(path, arguments); }
            catch { Process.Start("Bin\\ToolTipper.exe","Error " + path); }          
        }
        public static void Run(string path)
        {
            try { Process.Start(path); }
            catch { Process.Start("Bin\\ToolTipper.exe", "Error " + path); }
        }
        public static Label[] populateLabels()
        {
            Regex extension = new Regex(".([a-z]|[A-Z]){3,4}$");
            ArrayList tempArray = new ArrayList();
            int fileCount = GlobalVar.fileChoices.Count;
            for (int i = 0; i < fileCount; i++)
            {
                Label tempLabel = new System.Windows.Forms.Label();
                tempLabel.BackColor = Properties.Settings.backgroundColor;
                tempLabel.ForeColor = Properties.Settings.foregroundColor;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F);
                tempLabel.Location = new System.Drawing.Point(10, (i * 18) + 20);
                tempLabel.Size = new System.Drawing.Size(350, 18);
                if (searchType == "Movie") tempLabel.Text = "• " + ((System.IO.FileInfo)fileChoices[i]).Name;
                else tempLabel.Text = "• " + extension.Replace(((System.IO.FileInfo)GlobalVar.fileChoices[i]).Name, "");
                tempArray.Add(tempLabel);
            }
            return (Label[])tempArray.ToArray(typeof(Label));
        }
        public static void setBounds(Form obj)
        {
            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
            int widthAdder = GlobalVar.calculateWidth();
            int heightDiff = screens[Screen.AllScreens.Length - 1].WorkingArea.Top;

            if (obj.Name == "Shell")
            {
                GlobalVar.topBound = heightDiff;
                GlobalVar.leftBound = widthAdder - (obj.Size.Width / 2);
                GlobalVar.rightBound = widthAdder + (obj.Size.Width / 2);
                GlobalVar.bottomBound = obj.Size.Height + heightDiff;
            }
            else
            {
                heightDiff += shellInstance.Size.Height;
            }

            obj.Location = new System.Drawing.Point(widthAdder - (obj.Size.Width / 2), 1 + heightDiff);
            Console.WriteLine("Top bound: " + heightDiff);
        }
        public static void setCentered(Screen screen, Form obj)
        {
            int heightDiff = screen.Bounds.Top;
            int widthAdder;
            if(screen.Bounds.Left > 0)
                widthAdder = screen.Bounds.Left + ((Math.Abs(screen.Bounds.Right) - Math.Abs(screen.Bounds.Left)) / 2) - (obj.Size.Width / 2);
            else
                widthAdder = ((Math.Abs(screen.Bounds.Right) - Math.Abs(screen.Bounds.Left)) / 2) - (obj.Size.Width / 2);

            //GlobalVar.toolTip("setCentered", "obj.Name: " + obj.Name);
            if (obj.Name == "Shell")
            {
                GlobalVar.topBound = heightDiff;
                GlobalVar.leftBound = widthAdder;
                GlobalVar.rightBound = widthAdder+obj.Size.Width;
                GlobalVar.bottomBound = heightDiff+obj.Size.Height;
            }
            else {
                heightDiff += shellInstance.Size.Height;
            }

            obj.Location = new System.Drawing.Point(widthAdder, 1 + heightDiff - 20);
            Console.WriteLine("Top bound: " + heightDiff);
            //GlobalVar.toolTip("setCentered", "Setting Location: " + obj.Location + ", Right-Left = " + Math.Abs(screen.Bounds.Right) + " - " + Math.Abs(screen.Bounds.Left) );
        }
        public static void toolTip(String title, String body)
        {
            Process.Start("Bin\\ToolTipper.exe", title+" " + body);
        }
        public static int calculateWidth()
        {           
            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
            int widthAdder = 0;

            //Getting/Setting Position on screen
            foreach (Screen s in Screen.AllScreens)
            {
                if (Screen.AllScreens.Count() == 3)
                {
                    if (s.Bounds.Width == 1024)
                    {
                        continue;
                    }
                }
                if (s == screens[Screen.AllScreens.Count() - 1])
                {
                    widthAdder += (s.Bounds.Width / 2);
                }
                else widthAdder += s.Bounds.Width;
            }

            Console.WriteLine("WidthAdder: " + widthAdder);

            return widthAdder;
        }
    }
}
