using DesktopShell.Properties;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace DesktopShell;

public partial class Shell : Form
{
    #region Declarations

    private readonly List<string> lastCMD = [];
    private readonly System.Windows.Forms.Timer hideTimer;
    private System.Windows.Forms.Timer? fadeTimer;
    private Thread? t = null;
    private readonly List<Combination> shortcutList = [];
    private readonly List<WwwBrowser> browserList = [];
    private readonly List<WebCombo> webSiteList = [];
    private readonly bool[] hourSounded = new bool[GlobalVar.HoursInDay];
    private bool webSiteHit = false;
    private bool isHidden = true;
    private bool hasFaded = false;
    private bool isFading = false;
    // fadeDirection: 0 = idle, 1 = fading in (show), -1 = fading out (hide)
    private int fadeDirection = 0;
    private bool regexHit = false;
    private bool fadeBool = false;
    private float? shellVersionF;
    private float? shellVersionW;
    private int onHour;
    private int fadeTickAmount = 0;
    private int upCounter = 0;

    #endregion Declarations

    #region Screen Monitor WNDProc

    protected override void WndProc(ref Message m)
    {
#pragma warning disable IDE1006 // Naming Styles
        const int WM_DISPLAYCHANGE = 0x007e;
        const int WM_DPICHANGED = 0x02E0;
#pragma warning restore IDE1006 // Naming Styles

        // Listen for operating system messages.
        switch (m.Msg)
        {
            case WM_DISPLAYCHANGE:
                // reset position
                GlobalVar.Log("WM_DISPLAYCHANGE Detected: Position resetting currently disabled");
                //InitializeComponent();
                //GlobalVar.initDropDownRects(this);
                break;
            case WM_DPICHANGED:
                // Handle DPI changes
                GlobalVar.Log("WM_DPICHANGED Detected: Adjusting for new DPI");
                HandleDpiChange();
                break;
        }
        base.WndProc(ref m);
    }

    private void HandleDpiChange()
    {
        // Force a layout update to handle new DPI
        this.PerformLayout();

        // Recalculate positions and sizes if needed
        if (GlobalVar.DropDownRects != null)
        {
            GlobalVar.InitDropDownRects(this);
        }

        // Refresh the form
        this.Invalidate();
    }

    #endregion Screen Monitor WNDProc

    #region Hardcoded regex section

    [GeneratedRegex("^(crosshair|xhair){1}")]
    private static partial Regex Crosshair();

    [GeneratedRegex("(^pass(wd)?){1}|(^password){1}|(^pw){1}")]
    private static partial Regex Passwd();

    [GeneratedRegex("(^rescan$){1}")]
    private static partial Regex Rescan();

    [GeneratedRegex("(^roll$){1}")]
    private static partial Regex Roll();

    [GeneratedRegex("(^randomgame$){1}")]
    private static partial Regex RandomGame();

    [GeneratedRegex("^(timed )?(shutdown){1}$")]
    private static partial Regex Shutdown();

    [GeneratedRegex("(^config$){1}|(^options$){1}")]
    private static partial Regex Options();

    [GeneratedRegex("(^game(s)? ){1}")]
    private static partial Regex Games();

    [GeneratedRegex("(^show){1}(s)?( ){1}(raw )?")]
    private static partial Regex ShowsRaw();

    [GeneratedRegex("(^music ){1}")]
    private static partial Regex MusicSearch();

    [GeneratedRegex("(^movie(s)? ){1}")]
    private static partial Regex MovieSearch();

    [GeneratedRegex("(^[a-zA-Z]+:){1}")]
    private static partial Regex RemoteCommand();

    [GeneratedRegex("^([0-9]){4}$")]
    private static partial Regex FourDigitRegex();

    [GeneratedRegex("^([0-9]){3}$")]
    private static partial Regex ThreeDigitRegex();

    [GeneratedRegex("^(tooltip|note|tip)$")]
    private static partial Regex Tooltip();

    #endregion Hardcoded regex section

    #region Startup Constructor Function

    public Shell()
    {
        GlobalVar.ResetLog();
        Settings.ScanSettings();

        // Needed for both the local TCP server port selection and remote command routing.
        GlobalVar.ScanHosts();

        InitializeComponent();

        // Initialize DropDown Rects
        GlobalVar.InitDropDownRects(this);

        // Timer Instantiations
        GlobalVar.HourlyChime = new System.Windows.Forms.Timer
        {
            Interval = GlobalVar.HourlyChimeIntervalMs
        };
        GlobalVar.HourlyChime.Tick += delegate { TimerTick(); };
        GlobalVar.HourlyChime.Enabled = Settings.HourlyChimeChecked;
        for (int i = 0; i < GlobalVar.HoursInDay; i++)
        {
            hourSounded[i] = false;
        }

        hideTimer = new System.Windows.Forms.Timer
        {
            Interval = GlobalVar.HideTimerIntervalMs
        };
        hideTimer.Tick += delegate { HideTimerTick(hideTimer, EventArgs.Empty); };
        hideTimer.Enabled = true;

        if (Settings.EnableTCPServer)
        {
            GlobalVar.ServerInstance = new TCPServer();
        }

        PopulateCombos();
        PopulateWebSites();

        // Store-and-forward: pull pending queued messages once at startup.
        this.Shown += async (_, _) =>
        {
            try
            {
                await MessageQueueClient.ProcessPendingOnceOnStartupAsync(this, CancellationToken.None);
            }
            catch (Exception e)
            {
                GlobalVar.Log($"### Startup queue processing failed: {e.GetType()}: {e.Message}");
            }
        };
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    #endregion Startup Constructor Function

    #region Populating combos and websites

    private bool PopulateCombos()
    {
        shortcutList.Clear();
        browserList.Clear();

        //Populate Combinations
        using (StreamReader? sr = new("shortcuts.txt"))
        {
            while (!sr.EndOfStream)
            {
                List<string> tempFilePaths = [];
                List<string> tempArguments = [];
                string? tempKeyword = sr.ReadLine();
                string? tempLine;
                while (((tempLine = sr.ReadLine())
                    != "") && (!sr.EndOfStream))
                {
                    if (tempLine != null)
                    {
                        tempFilePaths.Add(tempLine);
                    }
                    tempLine = sr.ReadLine();
                    if (tempLine == "-")
                    {
                        tempArguments.Add("");
                    }
                    else if (tempLine != null)
                    {
                        tempArguments.Add(tempLine);
                    }
                }
                shortcutList.Add(new Combination(tempKeyword, tempFilePaths, tempArguments));
            }
        }

        //Populate WebBrowsers
        using (StreamReader? sr = new("webbrowsers.txt"))
        {
            bool isDefault = true;
            while (!sr.EndOfStream)
            {
                string? tempRegex = sr.ReadLine();
                string? tempFilePath = sr.ReadLine();
                sr.ReadLine();

                browserList.Add(new WwwBrowser(tempRegex, tempFilePath, isDefault));
                isDefault = false;
            }
        }
        return true;
    }

    private void PopulateWebSites()
    {
        webSiteList.Clear();
        using var sr = new StreamReader("websites.txt");
        while (!sr.EndOfStream)
        {
            List<string> tempWebsiteBase = [];
            string? tempLine;
            string? tempKeyword = sr.ReadLine();
            string? tempSearchableString = sr.ReadLine();
            bool? tempSearchable = Convert.ToBoolean(tempSearchableString);
            while (((tempLine = sr.ReadLine())
                    != "") && (!sr.EndOfStream))
            {
                if (tempLine != null)
                {
                    tempWebsiteBase.Add(tempLine);
                }
            }

            webSiteList.Add(new WebCombo(tempKeyword, tempWebsiteBase, tempSearchable));
        }
    }

    #endregion Populating combos and websites

    #region KeyPressed Handler

    private void CheckKeys(object sender, KeyEventArgs e)
    {
        //Last command - Up-Key
        if (e.KeyCode == Keys.Up)
        {
            if (lastCMD.Count != 0)
            {
                upCounter++;
                if (lastCMD.Count < upCounter)
                {
                    upCounter = 1;
                }

                textBox1.Text = lastCMD[^upCounter]?.ToString();
                if (textBox1.Text != null)
                {
                    textBox1.SelectionStart = textBox1.Text.Length;
                    textBox1.SelectionLength = 0;
                }
            }
        }
        //If {Enter} is pressed
        else if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;                  //Prevents Beep
            ProcessCommand(textBox1.Text.ToLower());
        }
    }

    public void ProcessCommand(string command)
    {
        //Command Formatting
        string? originalCMD = command;
        lastCMD.Add(originalCMD);
        string[] splitWords = SplitWords(originalCMD);
        GlobalVar.Log($"!!! Processing Command: {originalCMD}");

        //Initial Data Resets
        textBox1.Text = "";
        upCounter = 0;
        regexHit = false;
        webSiteHit = false;

        //Generic combo running (shortcuts.txt)
        foreach (Combination combo in shortcutList)
        {
            if (combo is not { Keyword: not null })
            {
                GlobalVar.Log($"### ShellForm::ProcessCommand() - combo.keyword = null\ncombo:{combo}");
                continue;
            }

            if (Regex.IsMatch(originalCMD, combo.Keyword, RegexOptions.IgnoreCase))
            {
                GlobalVar.Log($"!!! Command found in shortcuts.txt: {originalCMD}");
                for (int i = 0; i < combo.FilePath.Count; i++)
                {
                    string tempFilePath = combo.FilePath[i];
                    string tempArguments = combo.Arguments[i];
                    if (string.IsNullOrEmpty(tempArguments))
                    {
                        GlobalVar.Run(path: tempFilePath);
                    }
                    else
                    {
                        GlobalVar.Run(path: tempFilePath, arguments: tempArguments);
                    }
                }
                regexHit = true;
            }
        }

        //Hardcoded Functions
        if (!regexHit)
        {
            HardCodedCombos(originalCMD, splitWords);
        }
    }

    private void HardCodedCombos(string originalCMD, string[] splitWords)
    {
        //Crosshair
        if (Crosshair().IsMatch(splitWords[0]))
        {
            if (splitWords.Length == 1)
            {
                //Scan through running processes to see if there's already an instance
                //  Eventually this will change to toggle options/start, exit will have to be explicitly called
                bool isRunning = GlobalVar.KillProcess("Crosshair");

                if (!isRunning)
                {
                    GlobalVar.Run("Bin\\Crosshair.exe");
                }
            }
            else
            {
                if (splitWords[1].Equals("stop", StringComparison.OrdinalIgnoreCase) || splitWords[1].Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    GlobalVar.KillProcess("Crosshair");
                }
            }
        }
        //PasswordTabula
        else if (Passwd().IsMatch(splitWords[0]))
        {
            if (splitWords.Length == 1)
            {
                GlobalVar.Run("Bin\\PasswordTabula.exe");
            }
            else
            {
                GlobalVar.Run("Bin\\PasswordTabula.exe", splitWords[1]);
            }
        }
        //RescanRegex function
        else if (Rescan().IsMatch(splitWords[0]))
        {
            if (PopulateCombos())
            {
                GlobalVar.ToolTip("Rescan", "Regex Rescan Successful");
            }
            else
            {
                GlobalVar.ToolTip("Rescan", "Regex Rescan Failure");
            }

            PopulateWebSites();
        }
        else if (Tooltip().IsMatch(splitWords[0]))
        {
            string? title = "ToolTip";
            string? message = splitWords[^1];
            GlobalVar.ToolTip(title, message);
        }
        //RandomGame function
        else if (RandomGame().IsMatch(splitWords[0]))
        {
            DirectoryInfo dir = new(@"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Games");
            FileInfo[] fileEntries = dir.GetFiles();
            int? numRandoms = 10;
            Random random = new();
            int randomNumber;
            GlobalVar.FileChoices.Clear();

            for (int? i = 0; i < numRandoms; i++)
            {
                randomNumber = random.Next(0, fileEntries.Length);
                GlobalVar.FileChoices.Add(fileEntries[randomNumber]);
            }

            Thread t = new(new ThreadStart(ChoiceProc));
            t.Start();
            GlobalVar.SearchType = "Game";
        }
        //Timed shutdown function
        else if (Shutdown().IsMatch(splitWords[0]))
        {
            TimedShutdown(splitWords);
        }
        //Roll function
        else if (Roll().IsMatch(splitWords[0]))
        {
            Random randNum = new();
            int? num = 0;
            if (splitWords.Length == 2)
            {
                num = randNum.Next(1, (int)Convert.ToDecimal(splitWords[1]));
            }
            else if (splitWords.Length == 3)
            {
                num = randNum.Next((int)Convert.ToDecimal(splitWords[1]), (int)Convert.ToDecimal(splitWords[2]));
            }
            else
            {
                num = randNum.Next(1, 100);
            }

            GlobalVar.ToolTip("Roll", $"Randomed: {Convert.ToString(num)}");
        }
        //Options
        else if (Options().IsMatch(splitWords[0]))
        {
            t = new Thread(new ThreadStart(ConfigProc))
            {
                IsBackground = true
            };
            t.Start();
        }
        //Shows Raw Input Function
        else if (ShowsRaw().IsMatch(originalCMD))
        {
            string? rawSearch = ShowsRaw().Replace(originalCMD, "");
            GlobalVar.Run("Bin\\showListCreator.exe", rawSearch);
        }
        //Game Shortcut Searcher
        else if (Games().IsMatch(originalCMD))
        {
            OpenRandomGame(originalCMD);
        }
        //Music Searcher
        else if (MusicSearch().IsMatch(originalCMD))
        {
            MusicSearcher(originalCMD);
        }
        //Movie Searcher
        else if (MovieSearch().IsMatch(originalCMD))
        {
            MovieSearcher(originalCMD);
        }
        //Sending Remote Command
        else if (RemoteCommand().IsMatch(originalCMD))
        {
            string[] splitString = originalCMD.Split(':');
            if (splitString is { Length: 2 })
            {
                string remoteName = GlobalVar.NormalizeHostName(splitString[0]);
                string remoteCommand = splitString[^1].Trim();       //^1 = Length-1
                bool foundHost = false;
                foreach (var hostPair in GlobalVar.HostList)
                {
                    string hostName = GlobalVar.NormalizeHostName(hostPair.Key);
                    if (string.Equals(hostName, remoteName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundHost = true;
                        GlobalVar.Log($"!!! ShellForm::hardCodedCombos() Sending command: {remoteCommand} // to: {hostPair.Value}");
                        SendRemoteCommand(hostName, hostPair.Value, remoteCommand);
                    }
                }
                if (!foundHost)
                {
                    GlobalVar.Log($"### ShellForm::hardCodedCombos() Couldn't find hostName '{remoteName}' in hostlist.txt");
                }
            }
            else
            {
                GlobalVar.Log($"### ShellForm::hardCodedCombos() Invalid remote command, needs to have exactly 1 ':', command has {splitString.Length - 1}");
            }
        }
        //Website section
        foreach (var combo in webSiteList)
        {
            WebSiteOpener(combo, originalCMD);
        }
        //WWW Browser Section
        if (!webSiteHit)
        {
            foreach (var browser in browserList)
            {
                if (browser is not { keyword: not null, filePath: not null }) continue;

                Match match = Regex.Match(originalCMD, browser.keyword, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    GlobalVar.Run(browser.filePath);
                }
            }
        }
    }

    public static void SendRemoteCommand(string targetName, string port, string command)
    {
        if (!int.TryParse(port, out int portNum))
        {
            GlobalVar.Log($"### Error trying to parse port number as int: {port}");
            return;
        }

        GlobalVar.SendRemoteCommandWithQueueFallback(targetName, portNum, command);
    }

    private static void TimedShutdown(string[] splitWords)
    {
        switch (splitWords)
        {
            case { Length: 1 }:
                GlobalVar.Run("Bin\\Timed Shutdown.exe", "-trigger now");
                break;
            case { Length: > 1 } when splitWords[1] is string timeArg:
                string timedShutdownArgs = "-trigger clock ";

                if (FourDigitRegex().IsMatch(timeArg))
                {
                    timedShutdownArgs += $"{timeArg}00";
                }
                else if (ThreeDigitRegex().IsMatch(timeArg))
                {
                    timedShutdownArgs += $"0{timeArg}00";
                }
                else
                {
                    timedShutdownArgs += timeArg;
                }
                GlobalVar.Run("Bin\\Timed Shutdown.exe", timedShutdownArgs);
                GlobalVar.ToolTip("Shutdown", $"Shutdown Scheduled: {timeArg}");
                break;
        }
    }

    private static void OpenRandomGame(string originalCMD)
    {
        string? rawSearch = Games().Replace(originalCMD, "");
        GlobalVar.FileChoices.Clear();
        DirectoryInfo dir = new(Settings.GamesDirectory);
        foreach (var f in dir.GetFiles())
        {
            if (f.Name.ToLower().Contains(rawSearch, StringComparison.CurrentCulture))
            {
                GlobalVar.FileChoices.Add(f);
            }
        }

        if (GlobalVar.FileChoices.Count > 0)
        {
            Thread t = new(new ThreadStart(ChoiceProc));
            t.Start();
            GlobalVar.SearchType = "Game";
        }
        else
        {
            GlobalVar.ToolTip("Search", $"Error Couldn't find game: {rawSearch}");
        }
    }

    private static void MusicSearcher(string originalCMD)
    {
        string? rawSearch = MusicSearch().Replace(originalCMD, "");
        GlobalVar.FileChoices.Clear();
        DirectoryInfo dir = new(Settings.MusicDirectory);
        foreach (var f in dir.GetFiles())
        {
            if (f.Name.Contains(".mp3", StringComparison.CurrentCulture) || f.Name.Contains(".wav", StringComparison.CurrentCulture) ||
            f.Name.Contains(".mp4", StringComparison.CurrentCulture) || f.Name.Contains(".flac", StringComparison.CurrentCulture))
            {
                if (f.Name.Contains(rawSearch, StringComparison.CurrentCultureIgnoreCase))
                {
                    GlobalVar.FileChoices.Add(f);
                }
            }
        }

        if (GlobalVar.FileChoices.Count > 0)
        {
            Thread t = new(new ThreadStart(ChoiceProc));
            t.Start();
            GlobalVar.SearchType = "Music";
        }
        else
        {
            GlobalVar.ToolTip("Search", $"Error Couldn't find music: {rawSearch}");
        }
    }

    private void MovieSearcher(string originalCMD)
    {
        string? rawSearch = MovieSearch().Replace(originalCMD, "");
        GlobalVar.FileChoices.Clear();
        List<FileInfo> tempList2 = [];
        foreach (string s in Directory.GetFiles(Settings.MoviesDirectory, "*.*", SearchOption.AllDirectories))
        {
            tempList2.Add(new FileInfo(s));
        }

        foreach (var f in tempList2)
        {
            if ((f.Name.Contains(".avi") || f.Name.Contains(".mkv") || f.Name.Contains(".rar")) && f.Name.Contains("sample") && f.Name.Contains(rawSearch, StringComparison.CurrentCultureIgnoreCase))
            {
                GlobalVar.FileChoices.Add(f);
            }
        }

        if (GlobalVar.FileChoices.Count > 0)
        {
            Thread t = new(new ThreadStart(ChoiceProc));
            t.Start();
            GlobalVar.SearchType = "Movie";
        }
        else
        {
            GlobalVar.ToolTip("Search", $"Error Couldn't find movie: {rawSearch}");
        }
    }

    private void WebSiteOpener(WebCombo combo, string originalCMD)
    {
        string? browserPath = null;
        var searchTerms = originalCMD;
        webSiteHit = true;

        if (combo is not { Keyword: not null })
        {
            GlobalVar.Log($"### ShellForm::WebSiteOpener() - combo.Keyword = null\ncombo:{combo}");
            return;
        }

        if (Regex.IsMatch(originalCMD, combo.Keyword, RegexOptions.IgnoreCase))
        {
            //Choose browser
            foreach (var b in browserList)
            {
                if (b is not { keyword: not null, filePath: not null })
                {
                    GlobalVar.Log($"### ShellForm::WebSiteOpener() - browser.keyword or filePath is null\ncombo:{b}");
                    continue;
                }

                if (Regex.IsMatch(originalCMD, b.keyword, RegexOptions.IgnoreCase))
                {
                    browserPath = b.filePath;
                    searchTerms = b.keyword.Replace(originalCMD, "");                                   //Remove browser terms from search
                }
                else if (b.defaultBrowser)
                {
                    browserPath = b.filePath;
                }
            }
            //Searching here
            if (string.IsNullOrEmpty(browserPath))
            {
                GlobalVar.Log("No browser path found for website opening");
                return;
            }

            if (combo.Searchable != null)
            {
                //Remove keyword terms, turn spaces into +
                searchTerms = Regex.Replace(searchTerms, combo.Keyword, "");
                searchTerms = Regex.Replace(searchTerms, @"\s+", "+");
                foreach (var s in combo.WebsiteBase)
                {
                    GlobalVar.Run(path: browserPath, arguments: $"{s}{searchTerms}");
                    Thread.Sleep(GlobalVar.WebBrowserLaunchDelayMs);
                }
            }
            else
            {
                foreach (var s in combo.WebsiteBase)
                {
                    GlobalVar.Run(path: browserPath, arguments: s);
                    Thread.Sleep(GlobalVar.WebBrowserLaunchDelayMs);
                }
            }
        }
    }

    #endregion KeyPressed Handler

    #region MouseClicked Handlers

    private void Button1_Click(object sender, EventArgs e)
    {
        fadeBool = !fadeBool;
    }

    private void TextBox1_DoubleClick(object sender, EventArgs e)
    {
        GlobalVar.ServerInstance?.CloseServer();
        Close();
    }

    #endregion MouseClicked Handlers

    #region ConfigForm/ChoiceForm startup

    public static void ConfigProc()
    {
        Application.Run(GlobalVar.ConfigInstance = new ConfigForm());
    }

    public static void ChoiceProc()
    {
        Application.Run(GlobalVar.ChoiceInstance = new ChoiceForm());
    }

    #endregion ConfigForm/ChoiceForm startup

    #region Timer Functions

    public void FadeTimerTick(int direction)
    {
        int yAmt = direction switch
        {
            1 => GlobalVar.TopBound - GlobalVar.FadeAnimationStartOffset,
            _ => GlobalVar.TopBound - 1
        };

        if (fadeTickAmount <= GlobalVar.FadeTickMaxAmount && !hasFaded)
        {
            SetDesktopLocation(GlobalVar.LeftBound, yAmt += (direction * fadeTickAmount));
            fadeTickAmount++;
        }
        else if (fadeTickAmount > GlobalVar.FadeTickMaxAmount && !hasFaded)
        {
            fadeTickAmount = 0;
            hasFaded = true;
            isFading = false;
            // Animation finished, clear the direction and set final hidden state
            fadeDirection = 0;

            // Clean up the timer
            fadeTimer?.Stop();
            fadeTimer?.Dispose();
            fadeTimer = null;

            isHidden = direction switch
            {
                1 => false,
                -1 => true,
                _ => isHidden
            };
        }
    }

    public void FadeAway(int direction)
    {
        // If an animation is already running in the same direction, ignore
        if (fadeDirection == direction)
        {
            return;
        }

        // If animating in opposite direction, allow reversal
        if (fadeDirection != 0 && fadeDirection != direction)
        {
            GlobalVar.Log($"$$$ Reversing animation direction from {fadeDirection} to {direction}");
            fadeTimer?.Stop();
            fadeTimer?.Dispose();
            fadeTimer = null;
            isFading = false;
            hasFaded = false;
            fadeDirection = 0;
        }

        hasFaded = false;
        fadeDirection = direction;
        isFading = true; // Set this immediately to prevent race condition
        fadeTickAmount = 0;

        fadeTimer = new System.Windows.Forms.Timer
        {
            Interval = GlobalVar.FadeTimerIntervalMs
        };
        fadeTimer.Tick += delegate { FadeTimerTick(direction); };
        fadeTimer.Enabled = true;
    }

    public void HideTimerTick(object sender, EventArgs e)
    {
        // If an animation is in progress, don't start another one
        if (isFading || fadeDirection != 0)
        {
            return;
        }

        if (isHidden)
        {
            DecideToShow();
        }
        else
        {
            DecideToHide();
        }
    }

    public void TimerTick()
    {
        if (GlobalVar.HourlyChime?.Enabled == true)
        {
            System.Media.SoundPlayer myPlayer = new();
            string[] splitTime = SplitWords(DateTime.Now.ToShortTimeString());
            onHour = (int)Convert.ToDecimal(splitTime[0]);
            if ((splitTime[1] == "00") && (!hourSounded[onHour]))
            {
                myPlayer.SoundLocation = $"Sounds\\{splitTime[0]}.wav";
                myPlayer.PlaySync();
                myPlayer.SoundLocation = $"Sounds\\{splitTime[2]}.wav";
                myPlayer.PlaySync();
                hourSounded[onHour] = true;
                hourSounded[onHour - 1] = false;
            }
        }
    }

    public void DecideToShow()
    {
        // Check if we're currently hiding - if so, reverse the animation
        if (fadeDirection == -1 && isFading)
        {
            GlobalVar.Log(">>> Reversing hide animation - mouse re-entered trigger area");
            // Stop the current hide animation
            fadeTimer?.Stop();
            fadeTimer?.Dispose();
            fadeTimer = null;
            // Reset state and start show animation
            hasFaded = false;
            isFading = false;
            fadeDirection = 0;
            isHidden = false;
            TopMost = true;
            FadeAway(1);
            return;
        }

        // Don't start a new show animation if one is already running
        if (fadeDirection == 1 && isFading)
        {
            return;
        }

        // Only start show animation if we're currently hidden
        if (!isHidden)
        {
            return;
        }

        foreach (var r in GlobalVar.DropDownRects)
        {
            if (IsInField(r))
            {
                GlobalVar.Log($"^^^ Activating window now - Cursor: X={Cursor.Position.X}, Y={Cursor.Position.Y}, Rect: L={r.Left}, T={r.Top}, R={r.Right}, B={r.Bottom}");
                TopMost = true;                                                                                     //make window foreground
                                                                                                                    // The trigger rect is extended by padding on each side, but form bounds should use actual form size
                int formLeft = r.Left + GlobalVar.DropDownRectHorizontalPadding;  // Offset to get back to center of extended rect
                // Form animates down from screen top, the trigger rect is extended but form position is natural
                GlobalVar.BottomBound = r.Bottom - GlobalVar.DropDownRectVerticalPadding;  // Remove padding from bottom
                GlobalVar.TopBound = GlobalVar.BottomBound - ClientSize.Height;
                GlobalVar.LeftBound = formLeft;
                GlobalVar.RightBound = formLeft + ClientSize.Width;
                GlobalVar.Width = ClientSize.Width;
                FadeAway(1);
                break; // Important: exit after starting animation
            }
        }
    }

    public void DecideToHide()
    {
        // Check if mouse is within the form's actual bounds OR the trigger area
        // Form bounds after animation
        Rectangle formBounds = new(
            GlobalVar.LeftBound,
            GlobalVar.TopBound,
            GlobalVar.Width,
            GlobalVar.BottomBound - GlobalVar.TopBound
        );

        Point cursorPos = Cursor.Position;
        bool mouseInForm = cursorPos.X >= formBounds.Left &&
                          cursorPos.X <= formBounds.Right &&
                          cursorPos.Y >= formBounds.Top &&
                          cursorPos.Y <= formBounds.Bottom;

        // Also check trigger rects
        bool mouseInTriggerArea = false;
        foreach (var r in GlobalVar.DropDownRects)
        {
            if (IsInField(r))
            {
                mouseInTriggerArea = true;
                break;
            }
        }

        if (!mouseInForm && !mouseInTriggerArea && !isFading)
        {
            GlobalVar.Log($"!!! Hiding main window now - mouse left active area. Cursor: X={cursorPos.X}, Y={cursorPos.Y}");
            FadeAway(-1);  // Start fade out animation
            TopMost = false;  // Make window not foreground
        }
    }



    public static bool IsInField(Rectangle r)
    {
        Point cursorPos = Cursor.Position;
        // Require the cursor to be within both X and Y bounds of the bounding rect.
        // Previous implementation only checked the bottom Y bound which could make
        // the method return true for many unintended cursor positions.
        bool withinX = cursorPos.X >= r.Left && cursorPos.X <= r.Right;
        bool withinY = cursorPos.Y >= r.Top && cursorPos.Y <= r.Bottom;
        return withinX && withinY;
    }

    #endregion Timer Functions

    #region Shell Event Handlers

    public void Shell_Load(object sender, EventArgs e)
    {
        GlobalVar.SetCentered(Screen.FromPoint(Settings.PositionSave), this);

        // Getting colors from settings.ini
        BackColor = textBox1.BackColor = label1.BackColor = button1.BackColor = GlobalVar.BackColor = Settings.BackgroundColor;
        button1.ForeColor = textBox1.ForeColor = label1.ForeColor = GlobalVar.FontColor = Settings.ForegroundColor;

        // SetForegroundWindow here to fix hung shutdown error: https://stackoverflow.com/questions/23638290/gdi-window-preventing-shutdown
        SetForegroundWindow(this.Handle);
    }



    private void Shell_FormClosed(object sender, FormClosedEventArgs e)
    {
        // Clean up timers
        hideTimer?.Stop();
        hideTimer?.Dispose();

        fadeTimer?.Stop();
        fadeTimer?.Dispose();



        if (GlobalVar.ConfigInstance != null && t != null)
        {
            //t.Abort();
            ///TODO: Change this from thread to task and abort through cancellation tokens
        }
        else
        {
            GlobalVar.Log("### ShellForm::Shell_FormClosed() - configInstance or main thread is null");
        }
        GlobalVar.ServerInstance?.CloseServer();
    }

    #endregion Shell Event Handlers

    #region Splitwords

    public static string[] SplitWords(string splitMe)
    {
        return Regex.Split(splitMe, @"\W+");
    }

    #endregion Splitwords

    #region Change font/background Colors

    public void ChangeFontColor()
    {
        textBox1.ForeColor = label1.ForeColor = Settings.ForegroundColor;
    }

    public void ChangeBackgroundColor()
    {
        textBox1.BackColor = label1.BackColor = BackColor = Settings.BackgroundColor;
    }

    #endregion Change font/background Colors
}