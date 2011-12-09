using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DesktopShell
{
    public class GlobalVar
    {
        public static System.Windows.Forms.Timer hourlyChime;
        public static Shell shellInstance = null;

        // FilePath Section
        public static string[] deletePaths = { @"C:\automount.bat", @"C:\keyk", @"C:\keye", @"C:\keyd" };
        public static string desktopShellFolderPath = @"D:\Program Files (x86)\DesktopShell";
        public static string desktopShellPath = @"C:\Users\phuzE\Dropbox\DesktopShell\DesktopShell.sln";
        public static string desktopShellReleasePath = @"C:\Users\phuzE\Dropbox\DesktopShell\DesktopShell\bin\Release";
        public static string[] pronPaths = { @"D:\Users\phuzE\Documents\Downloads\Blackangel_2", @"K:\Transfer" };
        public static string vcplusplusPath = @"D:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe";
        public static string vcsPath = @"D:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe";

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
            catch { Process.Start("Bin\\ToolTipper.exe", "Error " + path); }          
        }
        public static void Run(string path)
        {
            try { Process.Start(path); }
            catch { Process.Start("Bin\\ToolTipper.exe", "Error " + path); }
        }
    }
}
