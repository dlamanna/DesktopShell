using System.Collections.Generic;
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
        public static string videoPlayer = "";
        public static bool hourlyChimeChecked = false;
        public static List<bool> multiscreenEnabled;
        public static bool checkVersion = false;
        public static Regex fontRegex;
        public static Regex backgroundRegex;
        public static Regex hourlyChimeRegex;
        public static Regex screensRegex;
        public static Regex updateRegex;
        public static Regex musicRegex;
        public static Regex gamesRegex;
        public static Regex moviesRegex;
        public static Regex showsRegex;
        public static Regex videoPlayerRegex;
        public static Regex positionRegex;
        
        /**/
        public static System.Drawing.Point positionSave;
        
        public Settings() {
            // // To add event handlers for saving and changing settings, uncomment the lines below:
            //
            this.SettingChanging += this.SettingChangingEventHandler;
            this.SettingsSaving += this.SettingsSavingEventHandler;

            fontRegex = new Regex("fontColor");
            backgroundRegex = new Regex("backgroundColor");
            hourlyChimeRegex = new Regex("hourlyChime");
            screensRegex = new Regex("screensEnabled");
            updateRegex = new Regex("updateCheck");
            musicRegex = new Regex("musicDirectory");
            gamesRegex = new Regex("gamesDirectory");
            moviesRegex = new Regex("moviesDirectory");
            showsRegex = new Regex("showsDirectory");
            videoPlayerRegex = new Regex("videoPlayer");
            positionRegex = new Regex("positionSave");
        }
        
        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) {
            // Add code to handle the SettingChangingEvent event here.
        }
        
        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) {
            // Add code to handle the SettingsSaving event here.
        }

        public static void scanSettings() {
            multiscreenEnabled = new List<bool>();
            //change stuff here to read settings into program
            try
            {
                using (var sr = new StreamReader("settings.ini"))
                {
                    while (!sr.EndOfStream)
                    {
                        string rawLine = sr.ReadLine();
                        string curLine = scanLine(rawLine);
                        if (fontRegex.IsMatch(rawLine))
                            foregroundColor = System.Drawing.ColorTranslator.FromHtml(curLine);
                        else if (backgroundRegex.IsMatch(rawLine))
                            backgroundColor = System.Drawing.ColorTranslator.FromHtml(curLine);
                        else if (hourlyChimeRegex.IsMatch(rawLine))
                            hourlyChimeChecked = System.Convert.ToBoolean(curLine);
                        else if (screensRegex.IsMatch(rawLine))
                        {
                            string[] screenStrings = curLine.Split(',');
                            for (int i = 0; i < screenStrings.Length; i++)
                            {
                                //GlobalVar.log("!!! Screen" + (i+1) + " enabled: " + screenStrings[i]);
                                multiscreenEnabled.Add(System.Convert.ToBoolean(screenStrings[i]));
                            }
                        }
                        else if (updateRegex.IsMatch(rawLine))
                            checkVersion = System.Convert.ToBoolean(curLine);
                        else if (musicRegex.IsMatch(rawLine))
                            musicDirectory = curLine;
                        else if (gamesRegex.IsMatch(rawLine))
                            gamesDirectory = curLine;
                        else if (moviesRegex.IsMatch(rawLine))
                            moviesDirectory = curLine;
                        else if (showsRegex.IsMatch(rawLine))
                            showsDirectory = curLine;
                        else if (videoPlayerRegex.IsMatch(rawLine))
                            videoPlayer = curLine;
                        else if (positionRegex.IsMatch(rawLine))
                        {
                            var positionString = curLine;
                            var g = Regex.Replace(positionString, @"[\{\}a-zA-Z=]", "").Split(',');
                            positionSave = new System.Drawing.Point(int.Parse(g[0]), int.Parse(g[1]));
                        }
                    }
                    writeSettings(false);
                }
            }
            catch (IOException e)
            {
                GlobalVar.log(e.Message);
            }
        }
        public static string scanLine(StreamReader sr) {
            string tempLine = "";
            int i = ((tempLine = sr.ReadLine()).IndexOf("=") + 1);
            return tempLine.Substring(i);
        }
        public static string scanLine(string line)
        {
            string tempLine = "";
            int i = ((tempLine = line).IndexOf("=") + 1);
            return tempLine.Substring(i);
        }
        public static void writeSettings() {
            writeSettings(true);
        }
        public static void writeSettings(bool toFile) {
            string[] tempLines = new string[11];
            tempLines[0] = "fontColor=" + System.Drawing.ColorTranslator.ToHtml(foregroundColor);
            tempLines[1] = "backgroundColor=" + System.Drawing.ColorTranslator.ToHtml(backgroundColor);
            tempLines[2] = "hourlyChime=" + hourlyChimeChecked.ToString();
            tempLines[3] = "screensEnabled=" + string.Join(",", multiscreenEnabled.ToArray()); ;
            tempLines[4] = "updateCheck=" + checkVersion.ToString();
            tempLines[5] = "musicDirectory=" + musicDirectory;
            tempLines[6] = "gamesDirectory=" + gamesDirectory;
            tempLines[7] = "moviesDirectory=" + moviesDirectory;
            tempLines[8] = "showsDirectory=" + showsDirectory;
            tempLines[9] = "videoPlayer=" + videoPlayer;
            tempLines[10] = "positionSave=" + positionSave.X + "," + positionSave.Y;

            if (toFile) {
                foreach (string s in tempLines) File.WriteAllLines("settings.ini", tempLines);
            }
            else {
                foreach (string s in tempLines) GlobalVar.log(s);
            }
        }
    }
}
