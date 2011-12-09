using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace DesktopShell
{
    public partial class Shell : Form
    {
        private System.Collections.ArrayList lastCMD = new System.Collections.ArrayList();
        private System.Windows.Forms.Timer hideTimer;
        private System.Windows.Forms.Timer fadeTimer;
        private Process proc = new Process();
        private ArrayList shortcutList = new ArrayList();
        private ArrayList trackerList = new ArrayList();
        private bool[] hourSounded = new bool[24];
        private bool isHidden = false;
        private bool hasFaded = false;
        private bool isFading = false;
        private bool regexHit = false;
        private bool fadeBool = false;
        private bool exitFlag = false;
        private string tooltipArgs = "";
        private string webSite = "";
        private int onHour;
        private int fadeTickAmount = 0;
        private int upCounter = 0;
        private int shortcutCounter = 0;

        //Browser section
        private string firefoxPath = "";
        private string operaPath = "";
        private string iePath = "";
        private string chromePath = "";

        //Hardcoded regex section
        private Regex google = new Regex("(^g$){1}|(^google$){1}");
        private Regex passwd = new Regex("(^pass(wd)?){1}|(^password){1}|(^pw){1}");
        private Regex rescan = new Regex("(^rescan$){1}");
        private Regex roll = new Regex("(^random$){1}|(^roll$){1}");
        private Regex addme = new Regex("(^fixme$){1}|(^addme$){1}");
        private Regex shutdown = new Regex("^(timed )?(shutdown){1}$");
        private Regex disable = new Regex("(^disable$){1}|(^cancel$){1}|(^stop$){1}");
        private Regex panic = new Regex("(^panic$){1}");
        private Regex pr0n = new Regex("(^pr0n$){1}|(^pron$){1}|(^porn$){1}");
        private Regex search = new Regex("(^search$){1}");
        private Regex game = new Regex("^(game){1}(s)?$");
        private Regex show = new Regex("^(show){1}(s)?$");
        private Regex movie = new Regex("^(movie){1}(s)?$");
        private Regex app = new Regex("^(app){1}(lication)?$");
        private Regex firefox = new Regex("(^ff$){1}|(^firefox$){1}");
        private Regex chrome = new Regex("(^chrome$){1}");
        private Regex iexplorer = new Regex("(^ie){1}(9)?$|(^iexplore){1}(r)?$");
        private Regex opera = new Regex("(^opera$){1}");
        private Regex options = new Regex("(^config$){1}|(^options$){1}");
        private Regex showsRaw = new Regex("(^show){1}(s)?( ){1}(raw ){1}");
        private Regex toolTipperRegex = new Regex("(^tt$){1}");

        public Shell()
        {
            InitializeComponent();

            //Timer Instantiations
            GlobalVar.hourlyChime = new System.Windows.Forms.Timer();
            GlobalVar.hourlyChime.Interval = 1000;
            GlobalVar.hourlyChime.Tick += delegate { TimerTick(EventArgs.Empty); };
            GlobalVar.hourlyChime.Enabled = Convert.ToBoolean(GlobalVar.GetSetting(3));
            for (int i = 0; i < 24; i++) hourSounded[i] = false;

            hideTimer = new System.Windows.Forms.Timer();
            hideTimer.Interval = 50;
            hideTimer.Tick += delegate { hideTimerTick(hideTimer, EventArgs.Empty); };
            hideTimer.Enabled = true;

            populateCombos();
            populateTrackers();    
        }

        private bool populateCombos()
        {
            shortcutList.Clear();
            exitFlag = false;
            shortcutCounter = 0;
            //Populate Combinations
            using (var sr = new StreamReader("shortcuts.txt"))
            {
                while (!sr.EndOfStream)
                {
                    Combination tempCombo = new Combination();
                    string tempLine = "";
                    tempCombo.keyword = sr.ReadLine();
                    tempCombo.filePath = sr.ReadLine();
                    tempCombo.arguments = sr.ReadLine();
                    shortcutList.Add(tempCombo);
                    shortcutCounter++;
                    if (((tempLine = sr.ReadLine()) != "") && (!sr.EndOfStream))
                    {
                        GlobalVar.Run("Bin\\ToolTipper.exe", "Shortcuts.txt_Error(" + shortcutCounter*4 + ") " + tempLine);
                        return false;
                    }

                    //Adding web browsers
                    if (tempCombo.filePath.IndexOf("opera.exe") != -1) operaPath = tempCombo.filePath;
                    else if (tempCombo.filePath.IndexOf("chrome.exe") != -1) chromePath = tempCombo.filePath;
                    else if (tempCombo.filePath.IndexOf("iexplore.exe") != -1) iePath = tempCombo.filePath;
                    else if (tempCombo.filePath.IndexOf("TheHackerFireFox.exe") != -1) firefoxPath = tempCombo.filePath;
                }
            }
            return true;
        }

        private void populateTrackers()
        {
            //Populate trackersList Combinations
            using (var sr = new StreamReader("trackers.txt"))
            {
                while (!sr.EndOfStream)
                {
                    Combination tempCombo = new Combination();
                    tempCombo.keyword = sr.ReadLine();
                    tempCombo.arguments = sr.ReadLine();
                    trackerList.Add(tempCombo);
                    sr.ReadLine();
                }
            }
        }

        private void CheckKeys(object sender, KeyEventArgs e)
        {
            // Last command - Up-Key
            if (e.KeyCode == Keys.Up)
            {
                upCounter++;
                if (((lastCMD.Count) - upCounter) < 0) upCounter = 1;

                textBox1.Text = lastCMD[(lastCMD.Count)-upCounter].ToString();
                textBox1.SelectionStart = textBox1.Text.Length;
            }
            // If enter key is pressed
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; //prevents beep

                string originalCMD = (textBox1.Text).ToLower();

                lastCMD.Add(originalCMD);
                string[] splitWords = SplitWords(originalCMD);
                textBox1.Text = "";
                upCounter = 0;
                regexHit = false;
                
                foreach (Combination combo in shortcutList) {
                    Match match = Regex.Match(originalCMD, combo.keyword, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        GlobalVar.Run(combo.filePath, combo.arguments);
                        regexHit = true;
                    }
                }

                if (!regexHit)
                {
                    //Google search
                    if (google.IsMatch(splitWords[0]))
                    {
                        string searchString = @"www.google.com/search?q=";
                        for (int i = 1; i < splitWords.Length; i++)
                        {
                            searchString += splitWords[i];
                            if (i != splitWords.Length - 1)
                            {
                                searchString += "+";
                            }
                        }
                        webSite = searchString;
                    }
                    //PasswordTabula
                    else if (passwd.IsMatch(splitWords[0]))
                    {
                        if (splitWords.Length == 1) GlobalVar.Run("Bin\\PasswordTabula.exe");
                        else GlobalVar.Run("Bin\\PasswordTabula.exe", splitWords[1]);
                    }
                    //RescanRegex function
                    else if (rescan.IsMatch(splitWords[0]))
                    {
                        if (populateCombos()) GlobalVar.Run("Bin\\ToolTipper.exe", "Rescan Regex Rescan Successfull");
                    }
                    //Timed shutdown function
                    else if (shutdown.IsMatch(splitWords[0]))
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
                            tooltipArgs = "Shutdown";
                            tooltipArgs += (" Shutdown Scheduled: " + splitWords[1]);
                            GlobalVar.Run("Bin\\ToolTipper.exe", tooltipArgs);
                        }
                    }
                    //Fixme function
                    else if (addme.IsMatch(splitWords[0]))
                    {
                        GlobalVar.Run(GlobalVar.vcsPath, GlobalVar.desktopShellPath);
                        GlobalVar.Run(@"C:\Windows\explorer.exe", GlobalVar.desktopShellReleasePath);
                        Close();
                    }
                    //Roll function
                    else if (roll.IsMatch(splitWords[0]))
                    {
                        Random randNum = new Random();
                        int num = 0;
                        if (splitWords.Length == 2) num = randNum.Next(1, (int)Convert.ToDecimal(splitWords[1]));
                        else if (splitWords.Length == 3) num = randNum.Next((int)Convert.ToDecimal(splitWords[1]), (int)Convert.ToDecimal(splitWords[2]));
                        else num = randNum.Next(1, 100);

                        tooltipArgs = "Roll Randomed: " + Convert.ToString(num);
                        GlobalVar.Run("Bin\\ToolTipper.exe", tooltipArgs);
                    }
                    //Search function
                    else if (search.IsMatch(splitWords[0]))
                    {
                        string searchString = "";
                        for (int i = 2; i < splitWords.Length; i++) searchString += (splitWords[i] + "+");
                        //Game Search
                        if (game.IsMatch(splitWords[1]))
                        {
                            GlobalVar.Run(operaPath, @"http://www.underground-gamer.com/browse.php?search=" + searchString); Thread.Sleep(500);
                            GlobalVar.Run(operaPath, @"http://shadowthein.net/browse.php?&search=" + searchString); Thread.Sleep(500);
                            GlobalVar.Run(operaPath, @"https://www.sceneaccess.org/browse?search=" + searchString); Thread.Sleep(500);
                            GlobalVar.Run(operaPath, @"https://www.bitgamer.su/browse.php?c82=1&c88=1&c79=1&incldead=0&region=&genre=&searchtitle=1&search=" + searchString); Thread.Sleep(500);
                            GlobalVar.Run(operaPath, @"https://gazellegames.net/torrents.php?artistname=My+Platforms&action=advanced&year=&remastertitle=&remasteryear=&filelist=&encoding=&format=&region=&language=&rating=&miscellaneous=&scene=&freetorrent=&taglist=&tags_type=1&order_by=time&order_way=desc&groupname=" + searchString); Thread.Sleep(500);
                            GlobalVar.Run(operaPath, @"http://www.blackcats-games.net/browse.php?cat=Categories&incldead=0&blah=0&search=" + searchString);
                        }
                        //Show Search
                        else if (show.IsMatch(splitWords[1])) GlobalVar.Run(operaPath, @"https://broadcasthe.net/torrents.php?searchstr=" + searchString);
                        //Movie Search
                        else if (movie.IsMatch(splitWords[1]))
                        {
                            GlobalVar.Run(operaPath, @"http://shadowthein.net/browse.php?&search=" + searchString); Thread.Sleep(500);
                            GlobalVar.Run(operaPath, @"https://piratethe.net/browse.php"); Thread.Sleep(500);
                            GlobalVar.Run(operaPath, @"http://www.torrentleech.org/torrents/browse/index/query/" + searchString); Thread.Sleep(500);
                            GlobalVar.Run(operaPath, @"https://preto.me/browse.php?search=" + searchString);
                        }
                        //App Search
                        else if (app.IsMatch(splitWords[1]))
                        {
                            GlobalVar.Run(operaPath, @"http://www.thegft.org/browse.php?view=0&c1=1&searchtype=1&search=" + searchString); Thread.Sleep(500);
                            GlobalVar.Run(operaPath, @"http://www.demonoid.me/files/?category=0&subcategory=All&quality=All&seeded=0&external=2&uid=0&sort=&query=" + searchString); Thread.Sleep(500);
                            GlobalVar.Run(operaPath, @"http://on.iptorrents.com/torrents/?l1=1&q=" + searchString);
                        }
                        //Porn Search
                        else if (pr0n.IsMatch(splitWords[1]))
                        {
                            GlobalVar.Run(operaPath, @"http://www.badjojo.com/?q=creampie&order=recent&last=all"); Thread.Sleep(500);
                            GlobalVar.Run(operaPath, @"http://www.pornhub.com/video?c=15&o=mr"); Thread.Sleep(500);
                            GlobalVar.Run(operaPath, @"http://www.pussytorrents.org/browse.php?search=creampie&cat=0"); Thread.Sleep(500);
                            GlobalVar.Run(operaPath, @"http://www.pisexy.org/browseall.php?search=creampie&cat=0&titdesc=0&x=0&y=0");
                        }
                    }
                    //pr0n function
                    else if (pr0n.IsMatch(splitWords[0])) foreach (string s in GlobalVar.pronPaths) GlobalVar.Run(@"C:\Windows\explorer.exe", s);
                    //Panic function
                    else if (panic.IsMatch(splitWords[0]))
                    {
                        //delete files without asking permission
                        foreach (string s in GlobalVar.deletePaths) File.Delete(s);
                        Thread.Sleep(3000);

                        //close all torrent progs
                        Process[] processes = Process.GetProcessesByName("utorrent-3.0");
                        foreach (Process process in processes) process.Kill();
                        processes = Process.GetProcessesByName("utorrent-2.0.4");
                        foreach (Process process in processes) process.Kill();
                        processes = Process.GetProcessesByName("utorrent-2.2");
                        foreach (Process process in processes) process.Kill();
                        processes = Process.GetProcessesByName("Azureus");
                        foreach (Process process in processes) process.Kill();

                        //dismount TC volumes
                        GlobalVar.Run(@"C:\dismount.bat");
                    }
                    //WWW Browser section
                    else if ((opera.IsMatch(splitWords[0])) && (splitWords.Length < 2)) GlobalVar.Run(operaPath);
                    else if ((chrome.IsMatch(splitWords[0])) && (splitWords.Length < 2)) GlobalVar.Run(chromePath);
                    else if ((iexplorer.IsMatch(splitWords[0])) && (splitWords.Length < 2)) GlobalVar.Run(iePath);
                    else if ((firefox.IsMatch(splitWords[0])) && (splitWords.Length < 2)) GlobalVar.Run(firefoxPath);
                    //Options
                    else if (options.IsMatch(splitWords[0]))
                    {
                        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadProc));
                        t.Start();
                    }
                    //Shows Raw Input Function
                    else if (showsRaw.IsMatch(originalCMD))
                    {
                        string rawSearch = showsRaw.Replace(originalCMD, "");
                        GlobalVar.Run("Bin\\showListCreator.exe", rawSearch);
                    }
                    //Tooltipper Test Section
                    else if (toolTipperRegex.IsMatch(splitWords[0]))
                    {
                        GlobalVar.Run("Bin\\ToolTipper.exe");
                    }
                    //Tracker section
                    foreach (Combination combo in trackerList)
                    {
                        Match match = Regex.Match(originalCMD, combo.keyword, RegexOptions.IgnoreCase);
                        if (match.Success) webSite = combo.arguments;        
                    }
                    //Final website catcher
                    if (webSite != "")
                    {
                        if ((splitWords.Length > 1) && (iexplorer.IsMatch(splitWords[1]))) GlobalVar.Run(iePath, webSite);
                        else if ((splitWords.Length > 1) && (chrome.IsMatch(splitWords[1]))) GlobalVar.Run(chromePath, webSite);
                        else if ((splitWords.Length > 1) && (firefox.IsMatch(splitWords[1]))) GlobalVar.Run(firefoxPath, webSite);
                        else GlobalVar.Run(operaPath, webSite);
                        webSite = "";
                    }
                }
            }
        }

        public static void ThreadProc()
        {
            Application.Run(new configForm());
        }

        public void fadeTimerTick(object sender, EventArgs e, int direction)
        {
            int yAmt;
            if (direction == 1) yAmt = GlobalVar.topBound-21;
            else yAmt = GlobalVar.topBound-1;

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
                if ((cursorPos.X > GlobalVar.leftBound && cursorPos.X < GlobalVar.rightBound) && (cursorPos.Y < GlobalVar.topBound) && (!isFading))
                {
                    //toggle hidden status
                    isHidden = false;
                    //make window foreground
                    this.TopMost = true;
                    //move window down 20 pixels
                    fadeAway(1);
                }
            }
            else
            {
                //if window is inactive and mouse is not in coords x(2705-3055), y(121-140)
                if (((cursorPos.X < GlobalVar.leftBound || cursorPos.X > GlobalVar.rightBound) || (cursorPos.Y > GlobalVar.bottomBound)) && (!isFading) && (!fadeBool))
                {
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
            System.Media.SoundPlayer myPlayer = new System.Media.SoundPlayer();
            string[] splitTime;

            if (GlobalVar.hourlyChime.Enabled)
            {
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

        public string[] SplitWords(string splitMe)
        {
            return Regex.Split(splitMe, @"\W+");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            fadeBool = !fadeBool;
        }

        private void textBox1_DoubleClick(object sender, EventArgs e)
        {
            Close();
        }

        public void Shell_Load(object sender, EventArgs e)
        {
            //Getting/Setting Position on screen
            foreach (Screen s in Screen.AllScreens)
            {
                if (s == screens[Screen.AllScreens.Length - 1])
                {
                    widthAdder += (s.Bounds.Width / 2);
                }
                else widthAdder += s.Bounds.Width;
            }

            this.Location = new System.Drawing.Point(widthAdder - (this.MaximumSize.Width / 2), 1 + heightDiff);
            GlobalVar.topBound = 2 + heightDiff;
            GlobalVar.leftBound = widthAdder - (this.MaximumSize.Width / 2);
            GlobalVar.rightBound = widthAdder + (this.MaximumSize.Width / 2);
            GlobalVar.bottomBound = 20 + heightDiff;
            //Getting colors from settings.ini
            this.BackColor = this.textBox1.BackColor = this.label1.BackColor = this.button1.BackColor = System.Drawing.ColorTranslator.FromHtml(GlobalVar.GetSetting(2));
            this.button1.ForeColor = this.textBox1.ForeColor = this.label1.ForeColor = System.Drawing.ColorTranslator.FromHtml(GlobalVar.GetSetting(1));
        }

        public void changeFontColor(string color)
        {
            this.textBox1.ForeColor = this.label1.ForeColor = System.Drawing.ColorTranslator.FromHtml(color);
        }

        public void changeBackgroundColor(string color)
        {
            this.textBox1.BackColor = this.label1.BackColor = this.BackColor = System.Drawing.ColorTranslator.FromHtml(color);
        }
    }
}

//Alarm function
/*else if (alarm.IsMatch(splitWords[0]))
{
    //Syntax: alarm 11:00 AM
    if (splitWords.Length > 1)
    {
        if (disable.IsMatch(splitWords[1]))
        {
            alarmTime = "";
            if (alarmTimer != null)
            {
                alarmTimer.Stop();
                tooltipArgs = "Alarm";
                tooltipArgs += " Alarm Disabled";
                GlobalVar.Run(Global.tooltipper, tooltipArgs);
            }
            else
            {
                tooltipArgs = "Alarm";
                tooltipArgs += " !! No Alarm to Disable!!";
                GlobalVar.Run(Global.tooltipper, tooltipArgs);
            }
        }
        else
        {
            alarmTime = "";
            alarmTime += splitWords[1];
            alarmTime += ":";
            alarmTime += splitWords[2];
            if (splitWords.Length == 4)
            {
                alarmTime += " ";
                alarmTime += splitWords[3].ToUpper();
            }
            else
            {
                //if no AM/PM specified
                alarmTime += " AM";
            }

            alarmTimer = new System.Windows.Forms.Timer();
            alarmTimer.Interval = 1000;
            alarmTimer.Tick += delegate { TimerTick(alarmTimer, EventArgs.Empty); };
            alarmTimer.Enabled = true;

            tooltipArgs = "Alarm";
            tooltipArgs += (" Alarm Set for: " + alarmTime);
            GlobalVar.Run(Global.tooltipper, tooltipArgs);
        }
    }
}*/
