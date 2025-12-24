using DesktopShell.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace DesktopShell
{
    public class GlobalVar
    {
        public static System.Windows.Forms.Timer? hourlyChime;
        public static Shell? shellInstance = null;
        public static TCPServer? serverInstance = null;
        public static ConfigForm? configInstance = null;
        public static ChoiceForm? choiceInstance = null;
        public static ScreenSelectorForm? screenSelectorInstance = null;
        public static ColorWheel? colorWheelInstance = null;
        public static List<FileInfo> fileChoices = new();
        public static List<Rectangle> dropDownRects = new();
        public static List<KeyValuePair<string, string>> hostList = new();
        public static Color backColor;
        public static Color fontColor;
        public static bool settingFontColor = false;
        public static bool settingBackColor = false;
        public static bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static string searchType = "";
        public static readonly string passPhrase = "cupcake";

        // FilePath Section
        public static string[] deletePaths = { @"C:\automount.bat", @"C:\keyk", @"C:\keye", @"C:\keyd", @"C:\keyx" };
        public static string currentAssemblyDirectory = Directory.GetCurrentDirectory();
        public static string desktopShellPath = @"C:\Users\phuzE\Dropbox\Programming\DesktopShell\DesktopShell.sln";
        public static string desktopShellReleasePath = @"C:\Users\phuzE\Dropbox\Programming\DesktopShell\DesktopShell\bin\Release";

        // Form Bounds
        public static int leftBound;

        public static int rightBound;
        public static int bottomBound;
        public static int topBound;
        public static int width;

        private static readonly uint SWP_NOSIZE = 0x0001;
        private static readonly uint SWP_NOZORDER = 0x0004;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int W, int H, uint uFlags);

        // Global Functions
        public static string? GetSetting(int line)
        {
            using var sr = new StreamReader("settings.ini");
            for (int i = 1; i < line; i++)
            {
                sr.ReadLine();
            }

            try 
            { 
                string? retLine = sr.ReadLine();
                return retLine;
            }
            catch
            {
                Log($"### GlobalVar::GetSetting() - Can't get {line}'th setting");
                return null;
            }
        }

        public static string? GetSetting(string whichSetting)
        {
            using StreamReader? sr = new("settings.ini");
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (line == null) return null;
                string[] tempSettingLine = line.Split('=');
                if (tempSettingLine.Length == 2)
                {
                    string tempSetting = tempSettingLine[0].Trim().ToLower();
                    string tempValue = tempSettingLine[1].Trim();

                    if (tempSetting.Equals(whichSetting.ToLower()))
                    {
                        return tempValue;
                    }
                }
            }
            return null;
        }

        public static void SetSetting(int line, string settingChange)
        {
            line -= 1;
            string[] tempLines = File.ReadAllLines("settings.ini");
            tempLines[line] = settingChange;

            File.WriteAllLines("settings.ini", tempLines);
        }

        public static bool StartProcess(Process p)
        {
            Log($"!!! Running {p.StartInfo.WorkingDirectory}{p.StartInfo.FileName}\t{p.StartInfo.Arguments}");
            try
            {
                p.Start();
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(Win32Exception))
                {
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.Verb = "runas";
                    try
                    {
                        p.Start();
                    }
                    catch 
                    {
                        Log($"### GlobalVar::StartProcess() Failed to start program on second attempt - {ex.Message}");
                        return false;
                    }
                }
                else
                {               
                    ToolTip("Error", $"Run: {p.StartInfo.FileName}\n{ex.GetType()} - {ex.Message}");
                    return false;
                }
            }
            return true;
        }

        public static void MoveToCurrentScreen(Process p)
        {
            Point curPos = Cursor.Position;
            Screen curScreen = Screen.FromPoint(curPos);

            // temp hack for now, fix later
            if (curScreen.Bounds.Width <= 1025)
            {
                int numIncrements = 0;
                int numSecondsUntilTimeout = 10;
                int increment = 50;
                int numMaxIncrements = ((numSecondsUntilTimeout * 1000) / increment);
                bool timeout = false;
                do
                {
                    p.Refresh();
                    Thread.Sleep(increment);
                    numIncrements++;

                    if (numIncrements == numMaxIncrements)
                    {
                        Log("### Timeout getting process handle to move screens");
                        timeout = true;
                    }
                    else if (p.MainWindowHandle != (IntPtr)0)
                    {
                        Log($"&&& Moved process in: {increment * numIncrements} ms");
                    }
                } while (numIncrements < numMaxIncrements && p.MainWindowHandle == (IntPtr)0);

                if (!timeout)
                {
                    try
                    {
                        IntPtr hWnd = p.MainWindowHandle;
                        if (!SetWindowPos(hWnd, (IntPtr)null, curScreen.WorkingArea.Left, curScreen.WorkingArea.Top, 0, 0, SWP_NOSIZE | SWP_NOZORDER))
                        {
                            throw new Win32Exception();
                        }
                    }
                    catch { /*Process.Start("Bin\\ToolTipper.exe", "Error " + path);*/ }
                }
            }
        }

        public static void Run(string path, string arguments)
        {
            Process p = new();
            p.StartInfo.Arguments = arguments;
            p.StartInfo.FileName = path;
            //p.StartInfo.WorkingDirectory = Path.GetDirectoryName(path);   // i think this should be unnecessary
            p.StartInfo.UseShellExecute = false;                            // this must be true, or we can't run as admin below
            //p.StartInfo.RedirectStandardOutput = true;                    // these lines cause stackoverflow exceptions, not sure why
            //p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.CreateNoWindow = true;
            if (!StartProcess(p)) return;

            MoveToCurrentScreen(p);
        }

        public static void Run(string path)
        {
            Run(path, "");
        }

        public static void InitDropDownRects(object sender)
        {
            dropDownRects.Clear();
            int numScreensDetected = Screen.AllScreens.Length;
            int numScreensEnteredInSettings = Properties.Settings.multiscreenEnabled.Count;

            if(numScreensDetected > numScreensEnteredInSettings)
            {
                Console.WriteLine($"### GlobalVar::initDropDownRects: number of screens {{{numScreensDetected}}} exceeds number" +
                                  $" entered in settings file {{{numScreensEnteredInSettings}}}");
                return;
            }

            for(int i = 0; i < numScreensDetected; i++) {
                if (Properties.Settings.multiscreenEnabled[i])
                {
                    Screen s = Screen.AllScreens[i];
                    Size shellSize = ((Shell)sender).ClientSize;
                    int pointX = s.WorkingArea.Left + ((s.WorkingArea.Width / 2) - shellSize.Width / 2);
                    int pointY = s.WorkingArea.Top + shellSize.Height;
                    Point rectPoint = new(pointX, pointY);

                    Rectangle tempRect = new(rectPoint, shellSize);
                    dropDownRects.Add(tempRect);
                    
                    Log($"### InitDropDownRects: Screen {i} - WorkingArea: L={s.WorkingArea.Left}, T={s.WorkingArea.Top}, W={s.WorkingArea.Width}, H={s.WorkingArea.Height}");
                    Log($"### InitDropDownRects: ShellSize: W={shellSize.Width}, H={shellSize.Height}");
                    Log($"### InitDropDownRects: BoundingRect: L={tempRect.Left}, T={tempRect.Top}, R={tempRect.Right}, B={tempRect.Bottom}");
                }
                else
                {
                    continue;
                }
            }
        }

        public static void ScanHosts()
        {
            try
            {
                using StreamReader? sr = new("hostlist.txt");
                while (!sr.EndOfStream)
                {
                    var hostPort = sr.ReadLine();
                    if (hostPort == null) return;

                    if (hostPort.Contains(':'))
                    {
                        string[] splitHostPort = hostPort.Split(':');
                        KeyValuePair<string, string> hostPortPair = new(splitHostPort[0], splitHostPort[1]);
                        hostList.Add(hostPortPair);
                    }
                    else
                    {
                        Log($"### GlobalVar::ScanHosts() - Bad format found in hostlist.txt, no colon");
                    }
                }
            }
            catch (IOException e)
            {
                Log($"### GlobalVar::ScanHosts() - {e.Message}");
            }
        }

        public static Label[] PopulateLabels()
        {
            Regex extension = new(".([a-z]|[A-Z]){3,4}$");
            List<Label> tempArray = new();
            int fileCount = fileChoices.Count;
            for(int i = 0; i < fileCount; i++) {
                Label tempLabel = new()
                {
                    BackColor = Properties.Settings.backgroundColor,
                    ForeColor = Properties.Settings.foregroundColor,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Microsoft Sans Serif", 9.5F),
                    Location = new Point(10, (i * 18) + 20),
                    Size = new Size(350, 18)
                };

                FileInfo tempFileInfo = fileChoices[i];
                if(tempFileInfo != null) 
                { 
                    if(searchType == "Movie") {
                        tempLabel.Text = "• " + tempFileInfo.Name;
                    }
                    else {
                        tempLabel.Text = $"• {extension.Replace(tempFileInfo.Name, "")}";
                    }
                }
                tempArray.Add(tempLabel);
            }
            return tempArray.ToArray();
        }

        public static void SetBounds(Form obj)
        {
            Screen[] screens = Screen.AllScreens;
            int widthAdder = CalculateWidth();
            int heightDiff = screens[Screen.AllScreens.Length - 1].WorkingArea.Top;

            if (obj.Name == "Shell")
            {
                topBound = heightDiff;
                leftBound = widthAdder - (obj.Size.Width / 2);
                rightBound = widthAdder + (obj.Size.Width / 2);
                bottomBound = obj.Size.Height + heightDiff;
            }
            else {
                if(shellInstance != null)
                    heightDiff += shellInstance.Size.Height;
            }

            obj.Location = new Point(widthAdder - (obj.Size.Width / 2), 1 + heightDiff);
        }

        public static void SetCentered(Screen screen, Form obj)
        {
            int heightDiff = screen.Bounds.Top;
            int widthAdder;
            if(screen.Bounds.Left > 0) {
                widthAdder = screen.Bounds.Left + ((Math.Abs(screen.Bounds.Right) - Math.Abs(screen.Bounds.Left)) / 2) - (obj.Size.Width / 2);
            }
            else {
                widthAdder = ((Math.Abs(screen.Bounds.Right) - Math.Abs(screen.Bounds.Left)) / 2) - (obj.Size.Width / 2);
            }

            if(obj.Name == "Shell") {
                topBound = heightDiff;
                leftBound = widthAdder;
                rightBound = widthAdder + obj.Size.Width;
                bottomBound = heightDiff + obj.Size.Height;
            }
            else {
                if(shellInstance != null)
                    heightDiff += shellInstance.Size.Height;
            }

            obj.Location = new Point(widthAdder, 1 + heightDiff - 20);
        }

        public static void ToolTip(string title, string body)
        {
            Log($"\t\t@@@ ToolTipping: {title} // {body}");
            Run($@"{currentAssemblyDirectory}\Bin\ToolTipper.exe", $"{title} {body}");
        }

        public static int CalculateWidth()
        {
            Screen[] screens = Screen.AllScreens;
            int widthAdder = 0;

            //Getting/Setting Position on screen
            foreach(Screen s in Screen.AllScreens) 
            {
                if (Screen.AllScreens.Length == 3)
                {
                    if(s.Bounds.Width == 1024) 
                    {
                        continue;
                    }
                }
                if(s == screens[Screen.AllScreens.Length - 1]) {
                    widthAdder += (s.Bounds.Width / 2);
                }
                else {
                    widthAdder += s.Bounds.Width;
                }
            }

            Log($"!!! GlobalVar::WidthAdder() - {widthAdder}");

            return widthAdder;
        }

        public static void UpdateColors()
        {
            Properties.Settings.backgroundColor = backColor;
            Properties.Settings.foregroundColor = fontColor;
            if (shellInstance != null)
            {
                shellInstance.ChangeBackgroundColor();
                shellInstance.ChangeFontColor();
            }
            Log($"&&& GlobalVar::UpdateColors() - Changing Colors: ({backColor.Name})\t({fontColor.Name})");
        }

        public static void SendRemoteCommand(int port, string command, string serverHost)
        {
            Socket? m_clientSocket;
            try
            {
                m_clientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress serverAddress = Dns.GetHostEntry(serverHost).AddressList[0];
                IPEndPoint ipEndPoint = new(serverAddress, port);
                SocketExtensions.Connect(m_clientSocket, serverHost, port, new TimeSpan(5000));
                if (m_clientSocket.Connected)
                {
                    ASCIIEncoding encoder = new();
                    byte[] buffer = encoder.GetBytes(passPhrase + command);
                    m_clientSocket.Send(buffer);
                }
                else
                {
                    Log($"### Socket not connected when trying to send '{command}', closing connection");
                }
            }
            catch (Exception e) 
            {
                Log($"### GlobalVar::SendRemoteCommand() - {e.GetType()}: {e.Message}");
            }
            /*finally
            {
                if (m_clientSocket != null)
                {
                    m_clientSocket.Close();
                }             
            }*/
        }

        public static void SendRemoteCommand(TcpClient client, string command)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                if (stream.CanWrite)
                {
                    ASCIIEncoding encoder = new();
                    byte[] buffer = command.Equals("lol\r\n") ? encoder.GetBytes(command) : encoder.GetBytes(passPhrase + command);
                    stream.Write(buffer, 0, buffer.Length);
                    if (client.Client.RemoteEndPoint is IPEndPoint endPoint)
                    {
                        Log($"!!! Sent command: '{command.Trim()}' to: {IPAddress.Parse(endPoint.Port.ToString())}");
                    }
                    else
                    {
                        Log($"!!! Sent command: '{command.Trim()}' to: unknown endpoint");
                    }
                }
                else
                {
                    Log("### GlobalVar::sendRemoteCommand: networkStream not showing as writable");
                }
            }
            catch (Exception e)
            {
                Log($"### GlobalVar::SendRemoteCommand() - {e.GetType()}: {e.Message}");
            }
            /*finally
            {
                if (command.Equals($"{passPhrase}ack"))
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                }
            }*/
        }

        public static int WhichPort(string hostName)
        {
            Log($"^^^ HostName: {hostName}");
            foreach (KeyValuePair<string, string> hostPair in hostList)
            {
                string tempHostName = hostPair.Key.Trim().ToLower();
                if (tempHostName.Equals(hostName))
                {
                    Log($"^^^ Starting server on port: {hostPair.Value}");
                    if (!int.TryParse(hostPair.Value, out int serverPort))
                    {
                        Log($"### GlobalVar::whichPort - Error trying to parse port number as int: {hostPair.Value}");
                    }
                    return serverPort;
                }
            }
            Log($"### No hostname found in hostlist.txt: {hostName}");
            return -1;
        }

        public static bool KillProcess(string processName)
        {
            bool isRunning = false;
            try
            {
                Process[] processList = Process.GetProcessesByName(processName);
                if(processList.Length >= 1) 
                {
                    foreach(Process p in processList) 
                    {
                        Console.WriteLine($"Process Found: {p.ProcessName}\t{p.Id}");
                        p.Kill();
                    }
                    isRunning = true;
                }
                return isRunning;
            }
            catch (Exception e)
            {
                Log($"### GlobalVar::KillProcess() - {e.Message}");
                return false;
            }
        }

        public static string GetSoundFolderLocation()
        {
            string folderPath = $"{Path.GetDirectoryName(AppContext.BaseDirectory)}\\Sounds";
            //throw new NotImplementedException();
            return folderPath;
        }

        public static void Log(string logOutput)
        {
            string logPath = "DesktopShell.log";
            try
            {
                using FileStream fs = new(logPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                using StreamWriter w = new(fs);
                {
                    w.WriteLine($"{DateTime.Now:HH:mm:ss.fff}:\t{logOutput}");
                }
            }
            catch(UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void ResetLog()
        {
            try
            {
                using (File.Create("DesktopShell.log")) { };
            }
            catch (Exception e)
            {
                ToolTip("Error", $"GlobalVar::ResetLog() - {e.GetType()}\n{e.Message}");
            }
        }
    }
}