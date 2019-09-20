using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace DesktopShell
{
    public partial class Shell : Form
    {
        #region Declarations
        private ArrayList lastCMD = new ArrayList();
        private System.Windows.Forms.Timer hideTimer;
        private System.Windows.Forms.Timer fadeTimer;
        private Process proc = new Process();
        private ArrayList shortcutList = new ArrayList();
        private ArrayList browserList = new ArrayList();
        private ArrayList webSiteList = new ArrayList();
        private bool[] hourSounded = new bool[24];
        private bool webSiteHit = false;
        private bool isHidden = false;
        private bool hasFaded = false;
        private bool isFading = false;
        private bool regexHit = false;
        private bool fadeBool = false;
        private float shellVersionF;
        private float shellVersionW;
        private int onHour;
        private int fadeTickAmount = 0;
        private int upCounter = 0;
        private int shortcutCounter = 0;
        #endregion

        #region Screen Monitor WNDProc
        protected override void WndProc(ref Message m) {
            const int WM_DISPLAYCHANGE = 0x007e;

            // Listen for operating system messages. 
            switch (m.Msg) {
                case WM_DISPLAYCHANGE:
                    // reset position
                    GlobalVar.log("WM_DISPLAYCHANGE Detected: Should be resetting position now");
                    //InitializeComponent();
                    //GlobalVar.initDropDownRects(this);
                    break;
            }
            base.WndProc(ref m);
        }
        #endregion

        #region Hardcoded regex section
        private Regex passwd = new Regex("(^pass(wd)?){1}|(^password){1}|(^pw){1}");
        private Regex rescan = new Regex("(^rescan$){1}");
        private Regex roll = new Regex("(^roll$){1}");
        private Regex randomGame = new Regex("(^randomgame$){1}");
        private Regex shutdown = new Regex("^(timed )?(shutdown){1}$");
        private Regex disable = new Regex("(^disable$){1}|(^cancel$){1}|(^stop$){1}");
        private Regex options = new Regex("(^config$){1}|(^options$){1}");
        private Regex games = new Regex("(^game(s)? ){1}");
        private Regex showsRaw = new Regex("(^show){1}(s)?( ){1}(raw )?");
        private Regex musicSearch = new Regex("(^music ){1}");
        private Regex movieSearch = new Regex("(^movie(s)? ){1}");
        #endregion

        #region Startup Constructor Function
        public Shell()
        {
            GlobalVar.resetLog();
            Properties.Settings.scanSettings();
            InitializeComponent();

            // Initialize DropDown Rects
            GlobalVar.initDropDownRects(this);

            // Timer Instantiations
            GlobalVar.hourlyChime = new System.Windows.Forms.Timer();
            GlobalVar.hourlyChime.Interval = 1000;
            GlobalVar.hourlyChime.Tick += delegate { TimerTick(EventArgs.Empty); };
            GlobalVar.hourlyChime.Enabled = Properties.Settings.hourlyChimeChecked;
            for (int i = 0; i < 24; i++) hourSounded[i] = false;

            hideTimer = new System.Windows.Forms.Timer();
            hideTimer.Interval = 50;
            hideTimer.Tick += delegate { hideTimerTick(hideTimer, EventArgs.Empty); };
            hideTimer.Enabled = true;

            if(Properties.Settings.checkVersion) checkVersions();

            populateCombos();
            populateWebSites();
        }
        #endregion

        #region Versioning Functions
        private void checkVersions()
        {
            using (var sr = new StreamReader("version.txt")) { shellVersionF = System.Convert.ToSingle(sr.ReadLine()); }

            if (!File.Exists("http://phuze.dyndns.info/version.txt")) { notify("Version Website version can't be obtained"); return; }
            else { using (var sr = new StreamReader("http://phuze.dyndns.info/version.txt")) { shellVersionW = System.Convert.ToSingle(sr.ReadLine()); } }
            
            if (shellVersionF != shellVersionW)
            {
                notify("Update Update available: " + shellVersionW);
                GlobalVar.Run("Updater\\download.cmd");
                Application.Exit();
            }
        }
        #endregion

        #region Populating combos and websites
        private bool populateCombos()
        {
            shortcutList.Clear(); browserList.Clear();
            shortcutCounter = 0;
            //Populate Combinations
            using (var sr = new StreamReader("shortcuts.txt"))
            {
                while (!sr.EndOfStream)
                {
                    string tempLine = "";
                    ArrayList tempFilePaths = new ArrayList();
                    ArrayList tempArguments = new ArrayList();
                    string tempKeyword = sr.ReadLine();

                    while (((tempLine = sr.ReadLine()) != "") && (!sr.EndOfStream)) 
                    {
                        tempFilePaths.Add(tempLine);
                        if ((tempLine = sr.ReadLine()) == "-") tempArguments.Add("");
                        else tempArguments.Add(tempLine);
                    }
                    shortcutList.Add(new Combination(tempKeyword,tempFilePaths,tempArguments));
                    shortcutCounter++;
                }
            }

            //Populate WebBrowsers
            using (var sr = new StreamReader("webbrowsers.txt"))
            {
                bool _default = true;
                while (!sr.EndOfStream)
                {
                    string tempRegex = sr.ReadLine();
                    string tempFilePath = sr.ReadLine();
                    sr.ReadLine();

                    browserList.Add(new wwwBrowser(tempRegex, tempFilePath, _default));
                    _default = false;
                }
            }
            return true;
        }
        private void populateWebSites()
        {
            webSiteList.Clear();
            //Populate websiteList Combinations
            using (var sr = new StreamReader("websites.txt"))
            {
                while (!sr.EndOfStream)
                {
                    ArrayList tempWebsiteBase = new ArrayList();
                    string tempLine;

                    string tempKeyword = sr.ReadLine();
                    bool tempSearchable = Convert.ToBoolean(sr.ReadLine());
                    while (((tempLine = sr.ReadLine()) != "") && (!sr.EndOfStream)) tempWebsiteBase.Add(tempLine);   
                    webSiteList.Add(new webCombo(tempKeyword,tempWebsiteBase,tempSearchable));
                }
            }
        }
        #endregion

        #region KeyPressed Handler + Hardcoded Combos
        private void CheckKeys(object sender, KeyEventArgs e)
        {
            //Last command - Up-Key
            if (e.KeyCode == Keys.Up)
            {
                if (lastCMD.Count != 0) {
                    upCounter++;
                    if (((lastCMD.Count) - upCounter) < 0) upCounter = 1;

                    textBox1.Text = lastCMD[(lastCMD.Count) - upCounter].ToString();
                    textBox1.SelectionStart = textBox1.Text.Length;
                    textBox1.SelectionLength = 0;
                }
            }
            //Next command - Down-Key
            /*else if (e.KeyCode == Keys.Down)
            {
                upCounter--;
                //if (((lastCMD.Count) - upCounter) < 0) upCounter = 1;

                textBox1.Text = lastCMD[(lastCMD.Count) - upCounter].ToString();
                textBox1.SelectionStart = textBox1.Text.Length;
            }*/
            //If {Enter} is pressed
            else if (e.KeyCode == Keys.Enter)
            {
                //Prevents Beep
                e.SuppressKeyPress = true;

                //Command Formatting
                string originalCMD = (textBox1.Text).ToLower();
                lastCMD.Add(originalCMD);
                string[] splitWords = SplitWords(originalCMD);

                //Initial Data Resets
                textBox1.Text = "";
                upCounter = 0;
                regexHit = false;
                webSiteHit = false;
                
                //Generic combo running (shortcuts.txt)
                foreach (Combination combo in shortcutList) {
                    Match match = Regex.Match(originalCMD, combo.keyword, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        for (int i = 0; i < combo.filePath.Count; i++)
                        {
                            GlobalVar.Run((string)combo.filePath[i], (string)combo.arguments[i]);
                        }
                        regexHit = true;
                    }
                }

                //Hardcoded Functions
                if (!regexHit)
                {
                    hardCodedCombos(originalCMD,splitWords);
                }
            }
        }

        private void hardCodedCombos(string originalCMD, string[] splitWords)
        {
            //PasswordTabula
            if (passwd.IsMatch(splitWords[0]))
            {
                if (splitWords.Length == 1) GlobalVar.Run("Bin\\PasswordTabula.exe");
                else GlobalVar.Run("Bin\\PasswordTabula.exe", splitWords[1]);
            }
            //RescanRegex function
            else if (rescan.IsMatch(splitWords[0]))
            {
                if (populateCombos()) notify("Rescan Regex Rescan Successful");
                populateWebSites();
            }
            //RandomGame function
            else if (randomGame.IsMatch(splitWords[0]))
            {
                string gameShortcutPath = @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Games\";
                string[] fileEntries = Directory.GetFiles(gameShortcutPath);

                Random random = new Random();
                int randomNumber = random.Next(0, fileEntries.Length);

                GlobalVar.toolTip("RandomGame", Path.GetFileNameWithoutExtension(fileEntries[randomNumber]));
                GlobalVar.Run(fileEntries[randomNumber]);
            }
            //Timed shutdown function
            else if (shutdown.IsMatch(splitWords[0]))
            {
                timedShutdown(splitWords);
            }
            //Roll function
            else if (roll.IsMatch(splitWords[0]))
            {
                Random randNum = new Random();
                int num = 0;
                if (splitWords.Length == 2) num = randNum.Next(1, (int)Convert.ToDecimal(splitWords[1]));
                else if (splitWords.Length == 3) num = randNum.Next((int)Convert.ToDecimal(splitWords[1]), (int)Convert.ToDecimal(splitWords[2]));
                else num = randNum.Next(1, 100);

                notify("Roll Randomed: " + Convert.ToString(num));
            }
            //Options
            else if (options.IsMatch(splitWords[0]))
            {
                System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(ConfigProc));
                t.Start();
            }
            //Shows Raw Input Function
            else if (showsRaw.IsMatch(originalCMD))
            {
                string rawSearch = showsRaw.Replace(originalCMD, "");
                GlobalVar.Run("Bin\\showListCreator.exe", rawSearch);
            }
            //Game Shortcut Searcher
            else if (games.IsMatch(originalCMD))
            {
                openRandomGame(originalCMD);
            }
            //Music Searcher
            else if (musicSearch.IsMatch(originalCMD))
            {
                musicSearcher(originalCMD);
            }
            //Movie Searcher
            else if (movieSearch.IsMatch(originalCMD))
            {
                movieSearcher(originalCMD);
            }
            //Website section
            foreach (webCombo combo in webSiteList)
            {
                webSiteOpener(combo,originalCMD);
            }
            //WWW Browser Section
            if (!webSiteHit)
            {
                foreach (wwwBrowser browser in browserList)
                {
                    Match match = Regex.Match(originalCMD, browser.keyword, RegexOptions.IgnoreCase);
                    if (match.Success) GlobalVar.Run(browser.filePath);
                }
            }
        }

        private void timedShutdown(string[] splitWords)
        {
            if (splitWords.Length == 1)
            {
                GlobalVar.Run("Bin\\Timed Shutdown.exe");
            }
            else if (splitWords.Length > 1)
            {
                Regex fourdigit = new Regex("^([0-9]){4}$");//|^([0-9]){1-2}(:)?([0-9]){1-2}");
                Regex threedigit = new Regex("^([0-9]){3}$");
                string timedShutdownArgs = "-trigger clock ";

                if (fourdigit.IsMatch(splitWords[1]))
                {
                    timedShutdownArgs += splitWords[1];
                    timedShutdownArgs += "00";
                }
                else if (threedigit.IsMatch(splitWords[1]))
                {
                    timedShutdownArgs += "0";
                    timedShutdownArgs += splitWords[1];
                    timedShutdownArgs += "00";
                }
                else
                {
                    timedShutdownArgs += splitWords[1];
                }
                GlobalVar.Run("Bin\\Timed Shutdown.exe", timedShutdownArgs);
                notify("Shutdown Shutdown Scheduled: " + splitWords[1]);
            }
        }

        private void openRandomGame(string originalCMD)
        {
            string rawSearch = games.Replace(originalCMD, "");
            GlobalVar.fileChoices.Clear();
            DirectoryInfo dir = new DirectoryInfo(Properties.Settings.gamesDirectory);
            foreach (FileInfo f in dir.GetFiles())
            {
                if ((f.Name.ToLower()).IndexOf(rawSearch) != -1)
                {
                    GlobalVar.fileChoices.Add(f);
                }
            }

            if (GlobalVar.fileChoices.Count > 0)
            {
                System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(ChoiceProc));
                t.Start();
                GlobalVar.searchType = "Game";
            }
            else notify("Error Couldn't find game: " + rawSearch);
        }

        private void musicSearcher(string originalCMD)
        {
            string rawSearch = musicSearch.Replace(originalCMD, "");
            GlobalVar.fileChoices.Clear();
            DirectoryInfo dir = new DirectoryInfo(Properties.Settings.musicDirectory);
            foreach (FileInfo f in dir.GetFiles())
            {
                if ((f.Name.IndexOf(".mp3") != -1) || (f.Name.IndexOf(".wav") != -1) || (f.Name.IndexOf(".mp4") != -1) || (f.Name.IndexOf(".flac") != -1))
                {
                    if ((f.Name.ToLower()).IndexOf(rawSearch) != -1) GlobalVar.fileChoices.Add(f);
                }
            }

            if (GlobalVar.fileChoices.Count > 0)
            {
                System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(ChoiceProc));
                t.Start();
                GlobalVar.searchType = "Music";
            }
            else notify("Error Couldn't find music: " + rawSearch);
        }

        private void movieSearcher(string originalCMD)
        {
            string rawSearch = movieSearch.Replace(originalCMD, "");
            GlobalVar.fileChoices.Clear();
            ArrayList tempList2 = new ArrayList();
            string[] tempList = Directory.GetFiles(Properties.Settings.moviesDirectory, "*.*", SearchOption.AllDirectories);
            foreach (string s in tempList) tempList2.Add(new FileInfo(s));
            foreach (FileInfo f in tempList2)
            {
                if (((f.Name.IndexOf(".avi") != -1) || (f.Name.IndexOf(".mkv") != -1) || (f.Name.IndexOf(".rar") != -1)) && (f.Name.IndexOf("sample") == -1))
                {
                    if ((f.Name.ToLower()).IndexOf(rawSearch) != -1) GlobalVar.fileChoices.Add(f);
                }
            }

            if (GlobalVar.fileChoices.Count > 0)
            {
                System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(ChoiceProc));
                t.Start();
                GlobalVar.searchType = "Movie";
            }
            else notify("Error Couldn't find movie: " + rawSearch);
        }

        private void webSiteOpener(webCombo combo, string originalCMD)
        {
            //Initial Variable Settings
            string browserPath = "";
            string searchTerms = originalCMD;
            webSiteHit = true;

            Match webSiteMatch = Regex.Match(originalCMD, combo.keyword, RegexOptions.IgnoreCase);
            if (webSiteMatch.Success)
            {
                //Choose browser
                foreach (wwwBrowser b in browserList)
                {
                    Match browserMatch = Regex.Match(originalCMD, b.keyword, RegexOptions.IgnoreCase);
                    if (browserMatch.Success)
                    {
                        browserPath = b.filePath;
                        //Remove browser terms from search
                        searchTerms = b.keyword.Replace(originalCMD, "");
                    }
                    else if (b.defaultBrowser) browserPath = b.filePath;
                }
                //Searching here
                if (combo.searchable)
                {
                    //Remove keyword terms, turn spaces into +
                    searchTerms = Regex.Replace(searchTerms, combo.keyword, "");
                    searchTerms = Regex.Replace(searchTerms, @"\s+", "+");
                    foreach (string s in combo.websiteBase)
                    {
                        GlobalVar.Run(browserPath, s + searchTerms);
                        Thread.Sleep(500);
                    }
                }
                else
                {
                    foreach (string s in combo.websiteBase)
                    {
                        GlobalVar.Run(browserPath, s);
                        Thread.Sleep(500);
                    }
                }
            }
        }
        #endregion

        #region MouseClicked Handlers
        private void button1_Click(object sender, EventArgs e) { fadeBool = !fadeBool; }
        private void textBox1_DoubleClick(object sender, EventArgs e) { Close(); }
        #endregion

        #region ConfigForm/ChoiceForm startup
        public static void ConfigProc() { Application.Run(GlobalVar.configInstance = new ConfigForm()); }
        public static void ChoiceProc() { Application.Run(GlobalVar.choiceInstance = new ChoiceForm()); }
        #endregion

        #region Timer Functions
        public void fadeTimerTick(object sender, EventArgs e, int direction)
        {
            int yAmt;
            if (direction == 1)
                yAmt = GlobalVar.topBound-21;
            else
                yAmt = GlobalVar.topBound-1;

            if (fadeTickAmount <= 20 && !hasFaded)
            {
                this.SetDesktopLocation(GlobalVar.leftBound, yAmt += (direction * fadeTickAmount));

                fadeTickAmount++;
                isFading = true;
            }
            else if (fadeTickAmount > 20 && !hasFaded)
            {
                fadeTickAmount = 0;
                hasFaded = true;
                isFading = false;
                fadeTimer.Stop();
            }
            else fadeTimer.Stop();
        }
        public void fadeAway(int direction)
        {
            hasFaded = false;

            fadeTimer = new System.Windows.Forms.Timer();
            fadeTimer.Interval = 15;
            fadeTimer.Tick += delegate { fadeTimerTick(fadeTimer, EventArgs.Empty, direction); };
            fadeTimer.Enabled = true;
        }
        public void hideTimerTick(object sender, EventArgs e)
        {
            //coordinate checking and collapsing
            Point cursorPos = Cursor.Position;
            if (isHidden)
            {   
               foreach (Rectangle r in GlobalVar.dropDownRects)
               {
                    if ((cursorPos.X > r.Left && cursorPos.X < r.Right) && (cursorPos.Y < r.Bottom) && (!isFading))
                    {
                        GlobalVar.log("^^^ Should be activating window now.");
                        //toggle hidden status
                        isHidden = false;
                        //make window foreground
                        this.TopMost = true;
                        GlobalVar.topBound = r.Top - this.ClientSize.Height;
                        GlobalVar.leftBound = r.Left;
                        GlobalVar.rightBound = r.Right;
                        GlobalVar.bottomBound = r.Top;/**/
                        //move window down 20 pixels
                        fadeAway(1);
                    }
               }
            }
            else
            {
                Boolean shouldHide = true;
                foreach (Rectangle r in GlobalVar.dropDownRects)
                {
                    if (((cursorPos.X < r.Left || cursorPos.X > r.Right) || (cursorPos.Y > r.Bottom)) && (!isFading) && (!fadeBool))
                        continue;
                    else
                    {
                        shouldHide = false;
                        break;
                    }
                }

                if (shouldHide)
                {
                    GlobalVar.log("!!! Should be hiding window now.");
                    //toggle hidden status
                    isHidden = true;
                    //move window position up 20 pixels
                    fadeAway(-1);
                    //make window not foreground
                    this.TopMost = false;
                }
            }
        }        
        public void TimerTick(EventArgs e)
        {
            if (GlobalVar.hourlyChime.Enabled)
            {
                System.Media.SoundPlayer myPlayer = new System.Media.SoundPlayer();
                string[] splitTime;

                splitTime = SplitWords(DateTime.Now.ToShortTimeString());
                onHour = (int)Convert.ToDecimal(splitTime[0]);
                if ((splitTime[1] == "00") && (hourSounded[onHour] == false))
                {
                    myPlayer.SoundLocation = "Sounds\\" + splitTime[0] + ".wav";
                    myPlayer.PlaySync();
                    myPlayer.SoundLocation = "Sounds\\" + splitTime[2] + ".wav";
                    myPlayer.PlaySync();
                    hourSounded[onHour] = true;
                    hourSounded[onHour - 1] = false;
                }
            }
        }
        #endregion

        #region Shell Event Handlers
        public void Shell_Load(object sender, EventArgs e)
        {
            //GlobalVar.setBounds(this);

            GlobalVar.setCentered(Screen.FromPoint(Properties.Settings.positionSave),this);

            /*widthAdder = GlobalVar.calculateWidth();

            this.Location = new System.Drawing.Point(widthAdder - (this.MaximumSize.Width / 2), 1 + heightDiff);
            GlobalVar.log("!!! Top bound: " + heightDiff);
            GlobalVar.topBound = heightDiff;
            GlobalVar.leftBound = widthAdder - (this.MaximumSize.Width / 2);
            GlobalVar.rightBound = widthAdder + (this.MaximumSize.Width / 2);
            GlobalVar.bottomBound = this.MaximumSize.Height + heightDiff;*/
            //Getting colors from settings.ini
            this.BackColor = this.textBox1.BackColor = this.label1.BackColor = this.button1.BackColor = Properties.Settings.backgroundColor;
            this.button1.ForeColor = this.textBox1.ForeColor = this.label1.ForeColor = Properties.Settings.foregroundColor;
        }
        private void Shell_FormClosed(object sender, FormClosedEventArgs e)
        {
            if(GlobalVar.configInstance != null) GlobalVar.configInstance.Close();
        }
        #endregion

        #region ToolTipper Notify/Splitwords
        public void notify(string args) { GlobalVar.Run("Bin\\ToolTipper.exe", args); }
        public string[] SplitWords(string splitMe) { return Regex.Split(splitMe, @"\W+"); }
        #endregion

        #region Change font/background Colors
        public void changeFontColor() { this.textBox1.ForeColor = this.label1.ForeColor = Properties.Settings.foregroundColor; }
        public void changeBackgroundColor() { this.textBox1.BackColor = this.label1.BackColor = this.BackColor = Properties.Settings.backgroundColor; }
        #endregion
    }
}