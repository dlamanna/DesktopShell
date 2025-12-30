namespace DesktopShell.Properties;



// This class allows you to handle specific events on the settings class:
//  The SettingChanging event is raised before a setting's value is changed.
//  The PropertyChanged event is raised after a setting's value is changed.
//  The SettingsLoaded event is raised after the setting values are loaded.
//  The SettingsSaving event is raised before the setting values are saved.
internal sealed partial class Settings
{
    public static Color BackgroundColor;
    public static Color ForegroundColor;
    public static string GamesDirectory = "";
    public static string ShowsDirectory = "";
    public static string MoviesDirectory = "";
    public static string MusicDirectory = "";
    public static string VideoPlayer = "";
    public static string TextEditor = "";
    public static bool HourlyChimeChecked = false;
    public static List<bool> MultiscreenEnabled;
    public static bool CheckVersion = false;
    public static bool EnableTCPServer = false;
    public static Regex FontRegex;
    public static Regex BackgroundRegex;
    public static Regex HourlyChimeRegex;
    public static Regex ScreensRegex;
    public static Regex UpdateRegex;
    public static Regex EnableTCPServerRegex;
    public static Regex MusicRegex;
    public static Regex GamesRegex;
    public static Regex MoviesRegex;
    public static Regex ShowsRegex;
    public static Regex VideoPlayerRegex;
    public static Regex TextEditorRegex;
    public static Regex PositionRegex;
    public static System.Drawing.Point PositionSave;

    public Settings()
    {
        // // To add event handlers for saving and changing settings, uncomment the lines below:
        //
        SettingChanging += SettingChangingEventHandler;
        SettingsSaving += SettingsSavingEventHandler;

        FontRegex = new Regex("fontColor");
        BackgroundRegex = new Regex("backgroundColor");
        HourlyChimeRegex = new Regex("hourlyChime");
        ScreensRegex = new Regex("screensEnabled");
        UpdateRegex = new Regex("updateCheck");
        EnableTCPServerRegex = new Regex("enableTCPServer");
        MusicRegex = new Regex("musicDirectory");
        GamesRegex = new Regex("gamesDirectory");
        MoviesRegex = new Regex("moviesDirectory");
        ShowsRegex = new Regex("showsDirectory");
        VideoPlayerRegex = new Regex("videoPlayer");
        TextEditorRegex = new Regex("textEditor");
        PositionRegex = new Regex("positionSave");
    }

    private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e)
    {
        // Add code to handle the SettingChangingEvent event here.
    }

    private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Add code to handle the SettingsSaving event here.
    }

    public static void ScanSettings()
    {
        MultiscreenEnabled = [];
        //change stuff here to read settings into program
        try
        {
            using var sr = new StreamReader("settings.ini");
            while (!sr.EndOfStream)
            {
                string? rawLine = sr.ReadLine();
                if (rawLine == null) return;
                string? curLine = ScanLine(rawLine);
                if (FontRegex.IsMatch(rawLine))
                    ForegroundColor = System.Drawing.ColorTranslator.FromHtml(curLine);
                else if (BackgroundRegex.IsMatch(rawLine))
                    BackgroundColor = System.Drawing.ColorTranslator.FromHtml(curLine);
                else if (HourlyChimeRegex.IsMatch(rawLine))
                    HourlyChimeChecked = System.Convert.ToBoolean(curLine);
                else if (ScreensRegex.IsMatch(rawLine))
                {
                    string[]? screenStrings = curLine.Split(',');
                    for (int i = 0; i < screenStrings.Length; i++)
                    {
                        //GlobalVar.log("!!! Screen" + (i+1) + " enabled: " + screenStrings[i]);
                        MultiscreenEnabled.Add(System.Convert.ToBoolean(screenStrings[i]));
                    }
                }
                else if (UpdateRegex.IsMatch(rawLine))
                    CheckVersion = System.Convert.ToBoolean(curLine);
                else if (EnableTCPServerRegex.IsMatch(rawLine))
                    EnableTCPServer = System.Convert.ToBoolean(curLine);
                else if (MusicRegex.IsMatch(rawLine))
                    MusicDirectory = curLine;
                else if (GamesRegex.IsMatch(rawLine))
                    GamesDirectory = curLine;
                else if (MoviesRegex.IsMatch(rawLine))
                    MoviesDirectory = curLine;
                else if (ShowsRegex.IsMatch(rawLine))
                    ShowsDirectory = curLine;
                else if (VideoPlayerRegex.IsMatch(rawLine))
                    VideoPlayer = curLine;
                else if (TextEditorRegex.IsMatch(rawLine))
                    TextEditor = curLine;
                else if (PositionRegex.IsMatch(rawLine))
                {
                    var positionString = curLine;
                    var g = Regex.Replace(positionString, @"[\{\}a-zA-Z=]", "").Split(',');
                    PositionSave = new System.Drawing.Point(int.Parse(g[0]), int.Parse(g[1]));
                }
            }
            WriteSettings(false);
        }
        catch (IOException e)
        {
            GlobalVar.Log(e.Message);
        }
    }

    public static string? ScanLine(StreamReader sr)
    {
        if (sr != null)
        {
            string? tempLine = sr.ReadLine();
            if (tempLine == null) return null;

            int i = tempLine.IndexOf("=") + 1;
            return tempLine[i..];
        }
        return null;
    }
    public static string ScanLine(string line)
    {
        string tempLine;
        int i = (tempLine = line).IndexOf("=") + 1;
        return tempLine[i..];
    }

    public static void WriteSettings()
    {
        WriteSettings(true);
    }
    public static void WriteSettings(bool toFile)
    {
        string[] tempLines = new string[13];
        tempLines[0] = $"fontColor={System.Drawing.ColorTranslator.ToHtml(ForegroundColor)}";
        tempLines[1] = $"backgroundColor={System.Drawing.ColorTranslator.ToHtml(BackgroundColor)}";
        tempLines[2] = $"hourlyChime={HourlyChimeChecked}";
        tempLines[3] = $"screensEnabled={string.Join(",", MultiscreenEnabled)}";
        tempLines[4] = $"updateCheck={CheckVersion}";
        tempLines[5] = $"enableTCPServer={EnableTCPServer}";
        tempLines[6] = $"musicDirectory={MusicDirectory}";
        tempLines[7] = $"gamesDirectory={GamesDirectory}";
        tempLines[8] = $"moviesDirectory={MoviesDirectory}";
        tempLines[9] = $"showsDirectory={ShowsDirectory}";
        tempLines[10] = $"videoPlayer={VideoPlayer}";
        tempLines[11] = $"textEditor={TextEditor}";
        tempLines[12] = $"positionSave={PositionSave.X},{PositionSave.Y}";

        if (toFile)
        {
            File.WriteAllLines("settings.ini", tempLines);
        }
        else
        {
            foreach (string s in tempLines) GlobalVar.Log($"@@@ Writing setting: {s}");
        }
    }
}
