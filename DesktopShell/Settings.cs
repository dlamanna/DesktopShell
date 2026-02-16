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
    public static string TextEditor = "";
    public static bool HourlyChimeChecked = false;
    public static List<bool> MultiscreenEnabled;
    public static bool EnableTCPServer = false;
    public static Regex FontRegex => FontColorRegex();
    public static Regex BackgroundRegex => BackgroundColorRegex();
    public static Regex HourlyChimeRegex => HourlyChimeSettingRegex();
    public static Regex ScreensRegex => ScreensEnabledRegex();
    public static Regex UpdateRegex => UpdateCheckRegex();
    public static Regex EnableTCPServerRegex => EnableTCPServerSettingRegex();
    public static Regex GamesRegex => GamesDirectoryRegex();
    public static Regex TextEditorRegex => TextEditorSettingRegex();
    public static Regex PositionRegex => PositionSaveRegex();
    public static Point PositionSave;

    [GeneratedRegex("fontColor")]
    private static partial Regex FontColorRegex();

    [GeneratedRegex("backgroundColor")]
    private static partial Regex BackgroundColorRegex();

    [GeneratedRegex("hourlyChime")]
    private static partial Regex HourlyChimeSettingRegex();

    [GeneratedRegex("screensEnabled")]
    private static partial Regex ScreensEnabledRegex();

    [GeneratedRegex("updateCheck")]
    private static partial Regex UpdateCheckRegex();

    [GeneratedRegex("enableTCPServer")]
    private static partial Regex EnableTCPServerSettingRegex();

    [GeneratedRegex("gamesDirectory")]
    private static partial Regex GamesDirectoryRegex();

    [GeneratedRegex("textEditor")]
    private static partial Regex TextEditorSettingRegex();

    [GeneratedRegex("positionSave")]
    private static partial Regex PositionSaveRegex();

    public Settings()
    {
        // // To add event handlers for saving and changing settings, uncomment the lines below:
        //
        SettingChanging += SettingChangingEventHandler;
        SettingsSaving += SettingsSavingEventHandler;
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
        bool sawEnableTcpServer = false;
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
                {
                    ForegroundColor = ColorTranslator.FromHtml(curLine);
                }
                else if (BackgroundRegex.IsMatch(rawLine))
                {
                    BackgroundColor = ColorTranslator.FromHtml(curLine);
                }
                else if (HourlyChimeRegex.IsMatch(rawLine))
                {
                    HourlyChimeChecked = Convert.ToBoolean(curLine);
                }
                else if (ScreensRegex.IsMatch(rawLine))
                {
                    string[]? screenStrings = curLine.Split(',');
                    for (int i = 0; i < screenStrings.Length; i++)
                    {
                        MultiscreenEnabled.Add(Convert.ToBoolean(screenStrings[i]));
                    }
                }
                else if (EnableTCPServerRegex.IsMatch(rawLine))
                {
                    EnableTCPServer = Convert.ToBoolean(curLine);
                    sawEnableTcpServer = true;
                }
                else if (GamesRegex.IsMatch(rawLine))
                {
                    GamesDirectory = curLine;
                }
                else if (TextEditorRegex.IsMatch(rawLine))
                {
                    TextEditor = curLine;
                }
                else if (PositionRegex.IsMatch(rawLine))
                {
                    var positionString = curLine;
                    var g = Regex.Replace(positionString, @"[\{\}a-zA-Z=]", "").Split(',');
                    PositionSave = new Point(int.Parse(g[0]), int.Parse(g[1]));
                }
            }

            if (!sawEnableTcpServer)
            {
                EnableTCPServer = true;
                GlobalVar.Log("^^^ Settings::ScanSettings() - enableTCPServer missing; defaulting to true");
            }
            WriteSettings(false);
        }
        catch (IOException e)
        {
            GlobalVar.Log(e.Message);
        }
    }

    public static string ScanLine(string line)
    {
        int eqIndex = line.IndexOf('=');
        if (eqIndex < 0) return "";
        return line[(eqIndex + 1)..];
    }

    public static void WriteSettings()
    {
        WriteSettings(true);
    }
    public static void WriteSettings(bool toFile)
    {
        string[] tempLines =
        [
            $"fontColor={ColorTranslator.ToHtml(ForegroundColor)}",
            $"backgroundColor={ColorTranslator.ToHtml(BackgroundColor)}",
            $"hourlyChime={HourlyChimeChecked}",
            $"screensEnabled={string.Join(",", MultiscreenEnabled)}",
            $"enableTCPServer={EnableTCPServer}",
            $"gamesDirectory={GamesDirectory}",
            $"textEditor={TextEditor}",
            $"positionSave={PositionSave.X},{PositionSave.Y}",
        ];
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
