using DesktopShell.Properties;
using System.Runtime.InteropServices;
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
    private readonly bool[] hourSounded = new bool[24];
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
    private int? shortcutCounter = 0;

    #endregion Declarations

    #region Screen Monitor WNDProc

    protected override void WndProc(ref Message m)
    {
        const int WM_DISPLAYCHANGE = 0x007e;
        const int WM_DPICHANGED = 0x02E0;

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
        if (GlobalVar.dropDownRects != null)
        {
            GlobalVar.InitDropDownRects(this);
        }

        // Refresh the form
        this.Invalidate();
    }

    #endregion Screen Monitor WNDProc

    #region Hardcoded regex section

    private readonly Regex crosshair = new("^(crosshair|xhair){1}");
    private readonly Regex passwd = new("(^pass(wd)?){1}|(^password){1}|(^pw){1}");
    private readonly Regex rescan = new("(^rescan$){1}");
    private readonly Regex roll = new("(^roll$){1}");
    private readonly Regex randomGame = new("(^randomgame$){1}");
    private readonly Regex shutdown = new("^(timed )?(shutdown){1}$");
    private readonly Regex options = new("(^config$){1}|(^options$){1}");
    private readonly Regex games = new("(^game(s)? ){1}");
    private readonly Regex showsRaw = new("(^show){1}(s)?( ){1}(raw )?");
    private readonly Regex musicSearch = new("(^music ){1}");
    private readonly Regex movieSearch = new("(^movie(s)? ){1}");
    private readonly Regex positionFix = new("(^positionfix$){1}");
    private readonly Regex remoteCommand = new("(^[a-zA-Z]+:){1}");
    private readonly Regex version = new("(^ver(sion)?$){1}");

    #endregion Hardcoded regex section

    #region Startup Constructor Function

    public Shell()
    {
        GlobalVar.ResetLog();
        Settings.ScanSettings();

        InitializeComponent();

        // Initialize DropDown Rects
        GlobalVar.InitDropDownRects(this);

        // Timer Instantiations
        GlobalVar.hourlyChime = new System.Windows.Forms.Timer
        {
            Interval = 1000
        };
        GlobalVar.hourlyChime.Tick += delegate { TimerTick(); };
        GlobalVar.hourlyChime.Enabled = Settings.hourlyChimeChecked;
        for (int i = 0; i < 24; i++)
        {
            hourSounded[i] = false;
        }

        hideTimer = new System.Windows.Forms.Timer
        {
            Interval = 50
        };
        hideTimer.Tick += delegate { HideTimerTick(hideTimer, EventArgs.Empty); };
        hideTimer.Enabled = true;

        if (Settings.checkVersion)
        {
            CheckVersions();
        }

        if (Settings.enableTCPServer)
        {
            GlobalVar.ScanHosts();
            GlobalVar.serverInstance = new TCPServer();
        }

        PopulateCombos();
        PopulateWebSites();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    #endregion Startup Constructor Function

    #region Versioning Functions

    private void CheckVersions()
    {
        using (var sr = new StreamReader("version.txt")) { shellVersionF = Convert.ToSingle(sr.ReadLine()); }
        if (!File.Exists("http://phuze.is-leet.com/version.txt"))
        {
            GlobalVar.ToolTip("Version Website", "version can't be obtained");
            return;
        }
        else
        {
            using var sr = new StreamReader("http://phuze.is-leet.com/version.txt");
            shellVersionW = Convert.ToSingle(sr.ReadLine());
        }

        if (shellVersionF != shellVersionW)
        {
            GlobalVar.ToolTip("Update", $"Update available: {shellVersionW}");
            GlobalVar.Run("Updater\\download.cmd");
            Application.Exit();
        }
    }

    #endregion Versioning Functions

    #region Populating combos and websites

    private bool PopulateCombos()
    {
        shortcutList.Clear();
        browserList.Clear();
        shortcutCounter = 0;

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
                shortcutCounter++;
            }
        }

        //Populate WebBrowsers
        using (StreamReader? sr = new("webbrowsers.txt"))
        {
            bool _default = true;
            while (!sr.EndOfStream)
            {
                string? tempRegex = sr.ReadLine();
                string? tempFilePath = sr.ReadLine();
                sr.ReadLine();

                browserList.Add(new WwwBrowser(tempRegex, tempFilePath, _default));
                _default = false;
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
                if (((lastCMD.Count) - upCounter) < 0)
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
            ProcessCommand((textBox1.Text).ToLower());
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
            if (combo is not { keyword: not null })
            {
                GlobalVar.Log($"### ShellForm::ProcessCommand() - combo.keyword = null\ncombo:{combo}");
                continue;
            }

            Match match = Regex.Match(originalCMD, combo.keyword, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                GlobalVar.Log($"!!! Command found in shortcuts.txt: {originalCMD}");
                for (int i = 0; i < combo.filePath.Count; i++)
                {
                    string tempFilePath = combo.filePath[i];
                    string tempArguments = combo.arguments[i];
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
        if (crosshair.IsMatch(splitWords[0]))
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
        else if (passwd.IsMatch(splitWords[0]))
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
        else if (rescan.IsMatch(splitWords[0]))
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
        //Attempted Position Fix
        else if (positionFix.IsMatch(splitWords[0]))
        {
            GlobalVar.InitDropDownRects(this);
            GlobalVar.Log("!!! Attempting Positioning Fix");
        }
        //RandomGame function
        else if (randomGame.IsMatch(splitWords[0]))
        {
            DirectoryInfo dir = new(@"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Games");
            FileInfo[] fileEntries = dir.GetFiles();
            int? numRandoms = 10;
            Random random = new();
            int randomNumber;
            GlobalVar.fileChoices.Clear();

            for (int? i = 0; i < numRandoms; i++)
            {
                randomNumber = random.Next(0, fileEntries.Length);
                GlobalVar.fileChoices.Add(fileEntries[randomNumber]);
            }

            Thread t = new(new ThreadStart(ChoiceProc));
            t.Start();
            GlobalVar.searchType = "Game";
        }
        //Timed shutdown function
        else if (shutdown.IsMatch(splitWords[0]))
        {
            TimedShutdown(splitWords);
        }
        //Roll function
        else if (roll.IsMatch(splitWords[0]))
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
        else if (options.IsMatch(splitWords[0]))
        {
            t = new Thread(new ThreadStart(ConfigProc))
            {
                IsBackground = true
            };
            t.Start();
        }
        //Shows Raw Input Function
        else if (showsRaw.IsMatch(originalCMD))
        {
            string? rawSearch = showsRaw.Replace(originalCMD, "");
            GlobalVar.Run("Bin\\showListCreator.exe", rawSearch);
        }
        //Game Shortcut Searcher
        else if (games.IsMatch(originalCMD))
        {
            OpenRandomGame(originalCMD);
        }
        //Music Searcher
        else if (musicSearch.IsMatch(originalCMD))
        {
            MusicSearcher(originalCMD);
        }
        //Movie Searcher
        else if (movieSearch.IsMatch(originalCMD))
        {
            MovieSearcher(originalCMD);
        }
        //Sending Remote Command
        else if (remoteCommand.IsMatch(originalCMD))
        {
            string[] splitString = originalCMD.Split(':');
            if (splitString is { Length: 2 })
            {
                string? remoteName = splitString[0];
                string? remoteCommand = splitString[^1];       //^1 = Length-1
                bool foundHost = false;
                foreach (var hostPair in GlobalVar.hostList)
                {
                    string? hostName = hostPair.Key.Trim().ToLower();
                    if (hostName.Equals(remoteName))
                    {
                        foundHost = true;
                        GlobalVar.Log($"!!! ShellForm::hardCodedCombos() Sending command: {remoteCommand} // to: {hostPair.Value}");
                        SendRemoteCommand(hostPair.Value, remoteCommand, hostPair.Key);
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
        //Outputting DesktopShell version
        else if (version.IsMatch(originalCMD))
        {
            Regex desktopShellRegex = new("DesktopShell");
            try
            {
                using var sr = new StreamReader("version.txt");
                while (!sr.EndOfStream)
                {
                    string? rawLine = sr.ReadLine();
                    if (rawLine == null) return;

                    if (desktopShellRegex.IsMatch(rawLine))
                    {
                        string? curLine = Settings.ScanLine(rawLine);   //Getting the setting value
                        GlobalVar.ToolTip("Version", $"DesktopShell Version: {curLine}");
                    }
                }
            }
            catch (IOException e)
            {
                GlobalVar.Log($"### ShellForm::HardCodedCombos version.txt - {e.Message}");
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

    public static void SendRemoteCommand(string port, string command, string host = "phuze.is-leet.com")
    {
        if (!int.TryParse(port, out int portNum))
        {
            GlobalVar.Log($"### Error trying to parse port number as int: {port}");
            return;
        }

        GlobalVar.SendRemoteCommand(portNum, command, host);
    }

    private static void TimedShutdown(string[] splitWords)
    {
        switch (splitWords)
        {
            case { Length: 1 }:
                GlobalVar.Run("Bin\\Timed Shutdown.exe", "-trigger now");
                break;
            case { Length: > 1 } when splitWords[1] is string timeArg:
                Regex fourdigit = new("^([0-9]){4}$");
                Regex threedigit = new("^([0-9]){3}$");
                string timedShutdownArgs = "-trigger clock ";

                if (fourdigit.IsMatch(timeArg))
                {
                    timedShutdownArgs += $"{timeArg}00";
                }
                else if (threedigit.IsMatch(timeArg))
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

    private void OpenRandomGame(string originalCMD)
    {
        string? rawSearch = games.Replace(originalCMD, "");
        GlobalVar.fileChoices.Clear();
        DirectoryInfo dir = new(Settings.gamesDirectory);
        foreach (var f in dir.GetFiles())
        {
            if (f.Name.ToLower().Contains(rawSearch, StringComparison.CurrentCulture))
            {
                GlobalVar.fileChoices.Add(f);
            }
        }

        if (GlobalVar.fileChoices.Count > 0)
        {
            Thread t = new(new ThreadStart(ChoiceProc));
            t.Start();
            GlobalVar.searchType = "Game";
        }
        else
        {
            GlobalVar.ToolTip("Search", $"Error Couldn't find game: {rawSearch}");
        }
    }

    private void MusicSearcher(string originalCMD)
    {
        string? rawSearch = musicSearch.Replace(originalCMD, "");
        GlobalVar.fileChoices.Clear();
        DirectoryInfo dir = new(Properties.Settings.musicDirectory);
        foreach (var f in dir.GetFiles())
        {
            if ((f.Name.Contains(".mp3", StringComparison.CurrentCulture)) || (f.Name.Contains(".wav", StringComparison.CurrentCulture)) || (f.Name.Contains(".mp4", StringComparison.CurrentCulture)) || (f.Name.Contains(".flac", StringComparison.CurrentCulture)))
            {
                if (f.Name.ToLower().Contains(rawSearch, StringComparison.CurrentCulture))
                {
                    GlobalVar.fileChoices.Add(f);
                }
            }
        }

        if (GlobalVar.fileChoices.Count > 0)
        {
            Thread t = new(new ThreadStart(ChoiceProc));
            t.Start();
            GlobalVar.searchType = "Music";
        }
        else
        {
            GlobalVar.ToolTip("Search", $"Error Couldn't find music: {rawSearch}");
        }
    }

    private void MovieSearcher(string originalCMD)
    {
        string? rawSearch = movieSearch.Replace(originalCMD, "");
        GlobalVar.fileChoices.Clear();
        List<FileInfo> tempList2 = [];
        string[] tempList = Directory.GetFiles(Settings.moviesDirectory, "*.*", SearchOption.AllDirectories);
        foreach (string s in tempList)
        {
            tempList2.Add(new FileInfo(s));
        }

        foreach (var f in tempList2)
        {
            if ((f.Name.Contains(".avi") || f.Name.Contains(".mkv") || f.Name.Contains(".rar")) && f.Name.Contains("sample"))
            {
                if (f.Name.ToLower().Contains(rawSearch))
                {
                    GlobalVar.fileChoices.Add(f);
                }
            }
        }

        if (GlobalVar.fileChoices.Count > 0)
        {
            Thread t = new(new ThreadStart(ChoiceProc));
            t.Start();
            GlobalVar.searchType = "Movie";
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

        if (combo is not { keyword: not null })
        {
            GlobalVar.Log($"### ShellForm::WebSiteOpener() - combo.keyword = null\ncombo:{combo}");
            return;
        }

        Match webSiteMatch = Regex.Match(originalCMD, combo.keyword, RegexOptions.IgnoreCase);
        if (webSiteMatch.Success)
        {
            //Choose browser
            foreach (var b in browserList)
            {
                if (b is not { keyword: not null, filePath: not null })
                {
                    GlobalVar.Log($"### ShellForm::WebSiteOpener() - browser.keyword or filePath is null\ncombo:{b}");
                    continue;
                }

                Match browserMatch = Regex.Match(originalCMD, b.keyword, RegexOptions.IgnoreCase);
                if (browserMatch.Success)
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

            if (combo.searchable != null)
            {
                //Remove keyword terms, turn spaces into +
                searchTerms = Regex.Replace(searchTerms, combo.keyword, "");
                searchTerms = Regex.Replace(searchTerms, @"\s+", "+");
                foreach (var s in combo.websiteBase)
                {
                    GlobalVar.Run(path: browserPath, arguments: $"{s}{searchTerms}");
                    Thread.Sleep(500);
                }
            }
            else
            {
                foreach (var s in combo.websiteBase)
                {
                    GlobalVar.Run(path: browserPath, arguments: s);
                    Thread.Sleep(500);
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
        GlobalVar.serverInstance?.CloseServer();
        Close();
    }

    #endregion MouseClicked Handlers

    #region ConfigForm/ChoiceForm startup

    public static void ConfigProc()
    {
        Application.Run(GlobalVar.configInstance = new ConfigForm());
    }

    public static void ChoiceProc()
    {
        Application.Run(GlobalVar.choiceInstance = new ChoiceForm());
    }

    #endregion ConfigForm/ChoiceForm startup

    #region Timer Functions

    public void FadeTimerTick(int direction)
    {
        int yAmt = direction switch
        {
            1 => GlobalVar.topBound - GlobalVar.fadeAnimationStartOffset,
            _ => GlobalVar.topBound - 1
        };

        if (fadeTickAmount <= GlobalVar.fadeTickMaxAmount && !hasFaded)
        {
            SetDesktopLocation(GlobalVar.leftBound, yAmt += (direction * fadeTickAmount));
            fadeTickAmount++;
        }
        else if (fadeTickAmount > GlobalVar.fadeTickMaxAmount && !hasFaded)
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
        // If an animation is already running, ignore subsequent requests
        if (fadeDirection != 0 || isFading)
        {
            return;
        }

        hasFaded = false;
        fadeDirection = direction;
        isFading = true; // Set this immediately to prevent race condition
        fadeTickAmount = 0;

        fadeTimer = new System.Windows.Forms.Timer
        {
            Interval = 15
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
        if (GlobalVar.hourlyChime != null && GlobalVar.hourlyChime.Enabled)
        {
            System.Media.SoundPlayer myPlayer = new();
            string[] splitTime;

            splitTime = SplitWords(DateTime.Now.ToShortTimeString());
            onHour = (int)Convert.ToDecimal(splitTime[0]);
            if ((splitTime[1] == "00") && (hourSounded[onHour] == false))
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

        foreach (var r in GlobalVar.dropDownRects)
        {
            if (IsInField(r))
            {
                GlobalVar.Log($"^^^ Activating window now - Cursor: X={Cursor.Position.X}, Y={Cursor.Position.Y}, Rect: L={r.Left}, T={r.Top}, R={r.Right}, B={r.Bottom}");
                TopMost = true;                                                                                     //make window foreground
                                                                                                                    // The trigger rect is extended by horizontal padding on each side, but form bounds should use actual form size
                int formLeft = r.Left + GlobalVar.dropDownRectHorizontalPadding;  // Offset to get back to center of extended rect
                // Form animates down from screen top, ending with bottom at r.Bottom
                GlobalVar.bottomBound = r.Bottom;
                GlobalVar.topBound = r.Bottom - ClientSize.Height;
                GlobalVar.leftBound = formLeft;
                GlobalVar.rightBound = formLeft + ClientSize.Width;
                GlobalVar.width = ClientSize.Width;
                FadeAway(1);
                break; // Important: exit after starting animation
            }
        }
    }

    public void DecideToHide()
    {
        // Check if mouse is within the form's actual bounds OR the trigger area
        // Form bounds after animation
        Rectangle formBounds = new Rectangle(
            GlobalVar.leftBound,
            GlobalVar.topBound,
            GlobalVar.width,
            GlobalVar.bottomBound - GlobalVar.topBound
        );

        Point cursorPos = Cursor.Position;
        bool mouseInForm = cursorPos.X >= formBounds.Left &&
                          cursorPos.X <= formBounds.Right &&
                          cursorPos.Y >= formBounds.Top &&
                          cursorPos.Y <= formBounds.Bottom;

        // Also check trigger rects
        bool mouseInTriggerArea = false;
        foreach (var r in GlobalVar.dropDownRects)
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
            isHidden = true;                                                                                        //toggle hidden status
            FadeAway(-1);                                                                                           //move window position up 20 pixels
            TopMost = false;                                                                                        //make window not foreground
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
        GlobalVar.SetCentered(Screen.FromPoint(Settings.positionSave), this);

        // Getting colors from settings.ini
        BackColor = textBox1.BackColor = label1.BackColor = button1.BackColor = GlobalVar.backColor = Settings.backgroundColor;
        button1.ForeColor = textBox1.ForeColor = label1.ForeColor = GlobalVar.fontColor = Settings.foregroundColor;

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



        if (GlobalVar.configInstance != null && t != null)
        {
            //t.Abort();
            ///TODO: Change this from thread to task and abort through cancellation tokens
        }
        else
        {
            GlobalVar.Log($"### ShellForm::Shell_FormClosed() - configInstance or main thread is null");
        }
        GlobalVar.serverInstance?.CloseServer();
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
        textBox1.ForeColor = label1.ForeColor = Settings.foregroundColor;
    }

    public void ChangeBackgroundColor()
    {
        textBox1.BackColor = label1.BackColor = BackColor = Settings.backgroundColor;
    }

    #endregion Change font/background Colors
}