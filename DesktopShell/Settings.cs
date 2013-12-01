using System.IO;
using System.Text.RegularExpressions;

namespace DesktopShell.Properties {
    
    
    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings {
        public static System.Drawing.Color backgroundColor;
        public static System.Drawing.Color foregroundColor;
        public static string gamesDirectory = "";
        public static string showsDirectory = "";
        public static string moviesDirectory = "";
        public static string musicDirectory = "";
        public static bool hourlyChimeChecked = false;
        public static bool checkVersion = false;
        public static System.Drawing.Point positionSave;
        
        public Settings() {
            // // To add event handlers for saving and changing settings, uncomment the lines below:
            //
            this.SettingChanging += this.SettingChangingEventHandler;
            //
            this.SettingsSaving += this.SettingsSavingEventHandler;
            //
        }
        
        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) {
            // Add code to handle the SettingChangingEvent event here.
        }
        
        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) {
            // Add code to handle the SettingsSaving event here.
        }

        public static void scanSettings()
        {
            //change stuff here to read settings into program
            using (var sr = new StreamReader("settings.ini"))
            {
                foregroundColor = System.Drawing.ColorTranslator.FromHtml(scanLine(sr));
                backgroundColor = System.Drawing.ColorTranslator.FromHtml(scanLine(sr));
                hourlyChimeChecked = System.Convert.ToBoolean(scanLine(sr));
                checkVersion = System.Convert.ToBoolean(scanLine(sr));
                musicDirectory = scanLine(sr);
                gamesDirectory = scanLine(sr);
                moviesDirectory = scanLine(sr);
                showsDirectory = scanLine(sr);
                
                var positionString = scanLine(sr);
                var g = Regex.Replace(positionString, @"[\{\}a-zA-Z=]", "").Split(',');
                positionSave = new System.Drawing.Point(int.Parse (g[0]),int.Parse( g[1]));
                //GlobalVar.toolTip("Settings.scanSettings", "Position: " + int.Parse(g[0]) + ", " + int.Parse(g[1]));
            }    
        }
        public static string scanLine(StreamReader sr) 
        {
            string tempLine = "";
            int i = ((tempLine = sr.ReadLine()).IndexOf("=") + 1);
            return tempLine.Substring(i);
        }
        public static void writeSettings()
        {
            string[] tempLines = new string[9];
            tempLines[0] = "fontColor=" + System.Drawing.ColorTranslator.ToHtml(foregroundColor);
            tempLines[1] = "backgroundColor=" + System.Drawing.ColorTranslator.ToHtml(backgroundColor);
            tempLines[2] = "hourlyChime=" + hourlyChimeChecked.ToString();
            tempLines[3] = "updateCheck=" + checkVersion.ToString();
            tempLines[4] = "musicDirectory=" + musicDirectory;
            tempLines[5] = "gamesDirectory=" + gamesDirectory;
            tempLines[6] = "moviesDirectory=" + moviesDirectory;
            tempLines[7] = "showsDirectory=" + showsDirectory;
            tempLines[8] = "positionSave=" + positionSave.X + "," + positionSave.Y;

            foreach (string s in tempLines) File.WriteAllLines("settings.ini", tempLines);
        }
    }
}
