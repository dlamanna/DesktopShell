using DesktopShell.Forms;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Text.Json;
using System.Security.Cryptography;

namespace DesktopShell;

public static partial class GlobalVar
{
    #region Declarations
    public static System.Windows.Forms.Timer? HourlyChime;
    public static Shell? ShellInstance;
    public static TCPServer? ServerInstance;
    public static ConfigForm? ConfigInstance;
    public static ChoiceForm? ChoiceInstance;
    public static ScreenSelectorForm? ScreenSelectorInstance;
    public static ColorWheel? ColorWheelInstance;
    public static List<FileInfo> FileChoices = [];
    public static List<Rectangle> DropDownRects = [];
    public static int DropDownRectHorizontalPadding = 50;  // Extra pixels on left/right of trigger area
    public static int DropDownRectVerticalPadding = 20;     // Extra pixels on top/bottom of trigger area
    public static List<KeyValuePair<string, string>> HostList = [];
    public static Color BackColor;
    public static Color FontColor;
    public static bool SettingFontColor = false;
    public static bool SettingBackColor = false;
    public static bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static string SearchType = "";

    // Security: PassPhrase is loaded from environment variable DESKTOPSHELL_PASSPHRASE
    // Fallback to "default" if not set (should be changed in production)
    public static readonly string PassPhrase = Environment.GetEnvironmentVariable("DESKTOPSHELL_PASSPHRASE") ?? "default";

    // Optional TLS for TCP remote commands.
    // Enable with DESKTOPSHELL_TCP_TLS=1 and provide server cert via DESKTOPSHELL_TCP_TLS_PFX + DESKTOPSHELL_TCP_TLS_PFX_PASSWORD.
    // Client validates via OS trust by default; for self-signed, pin with DESKTOPSHELL_TCP_TLS_THUMBPRINT.
    public static bool TcpTlsEnabled => string.Equals(Environment.GetEnvironmentVariable("DESKTOPSHELL_TCP_TLS"), "1", StringComparison.OrdinalIgnoreCase);
    public static string? TcpTlsPfxPath => Environment.GetEnvironmentVariable("DESKTOPSHELL_TCP_TLS_PFX");
    public static string? TcpTlsPfxPassword => Environment.GetEnvironmentVariable("DESKTOPSHELL_TCP_TLS_PFX_PASSWORD");
    public static string? TcpTlsPinnedThumbprint => Environment.GetEnvironmentVariable("DESKTOPSHELL_TCP_TLS_THUMBPRINT");

    // HTTPS message queue fallback (Cloudflare Worker + Durable Objects)
    public static bool QueueEnabled => string.Equals(Environment.GetEnvironmentVariable("DESKTOPSHELL_QUEUE_ENABLED"), "1", StringComparison.OrdinalIgnoreCase);
    public static string QueueBaseUrl => (Environment.GetEnvironmentVariable("DESKTOPSHELL_QUEUE_BASEURL") ?? "https://queue.dlamanna.com").TrimEnd('/');
    public static string? QueueKeyBase64 => Environment.GetEnvironmentVariable("DESKTOPSHELL_QUEUE_KEY_B64");
    public static string? CfAccessClientId => Environment.GetEnvironmentVariable("DESKTOPSHELL_CF_ACCESS_CLIENT_ID");
    public static string? CfAccessClientSecret => Environment.GetEnvironmentVariable("DESKTOPSHELL_CF_ACCESS_CLIENT_SECRET");

    private static X509Certificate2? tcpServerCertificate;

    // FilePath Section
    public static string CurrentAssemblyDirectory = Directory.GetCurrentDirectory();

    // Form Bounds
    public static int LeftBound;
    public static int RightBound;
    public static int BottomBound;
    public static int TopBound;
    public static int Width;
    public static int Height;

    // Animation Constants
    public static int FadeAnimationStartOffset = 21;  // Initial offset for fade animation
    public static int FadeTickMaxAmount = 20;         // Maximum fade tick count

    // Timer Interval Constants (milliseconds)
    public const int HourlyChimeIntervalMs = 1000;
    public const int HideTimerIntervalMs = 50;
    public const int FadeTimerIntervalMs = 15;

    // Thread Sleep Durations (milliseconds)
    public const int WebBrowserLaunchDelayMs = 500;
    public const int TcpConnectionRetryDelayMs = 300;
    public const int TcpReadDelayMs = 100;

    // Network Constants
    public const int TcpBufferSize = 4096;
    public const int TcpConnectionTimeoutSeconds = 5;
    public const int TcpReadTimeoutMs = 250;
    public const int TcpMessageIdleTimeoutMs = 1500;

    public const string TcpMessageTerminator = "\r\n";

    // UI Layout Constants
    public const int HoursInDay = 24;
    public const int MinimumScreenWidth = 1025;
    public const int LegacyScreenWidth = 1024;
    public const int ControlHeight = 18;
    public const int ControlSpacing = 50;

    // Regex patterns
    [GeneratedRegex(".([a-z]|[A-Z]){3,4}$")]
    private static partial Regex FileExtension();

#pragma warning disable IDE1006 // Naming rule violation - Windows API constants use UPPER_CASE
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int W, int H, uint uFlags);
#pragma warning restore IDE1006
    #endregion

    #region Setting Functions
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
        line--;
        string[] tempLines = File.ReadAllLines("settings.ini");
        tempLines[line] = settingChange;

        File.WriteAllLines("settings.ini", tempLines);
    }

    public static void MoveToCurrentScreen(Process p)
    {
        Point curPos = Cursor.Position;
        Screen curScreen = Screen.FromPoint(curPos);

        // temp hack for now, fix later
        if (curScreen.Bounds.Width <= MinimumScreenWidth)
        {
            int numIncrements = 0;
            const int numSecondsUntilTimeout = 10;
            const int increment = 50;
            const int numMaxIncrements = ((numSecondsUntilTimeout * 1000) / increment);
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

    #endregion

    #region UI Functions
    public static void InitDropDownRects(object sender)
    {
        DropDownRects.Clear();
        int numScreensDetected = Screen.AllScreens.Length;
        int numScreensEnteredInSettings = Properties.Settings.MultiscreenEnabled.Count;

        if (numScreensDetected > numScreensEnteredInSettings)
        {
            Console.WriteLine($"### GlobalVar::initDropDownRects: number of screens {{{numScreensDetected}}} exceeds number" +
                              $" entered in settings file {{{numScreensEnteredInSettings}}}");
            return;
        }

        for (int i = 0; i < numScreensDetected; i++)
        {
            if (Properties.Settings.MultiscreenEnabled[i])
            {
                Screen s = Screen.AllScreens[i];
                Size shellSize = ((Shell)sender).ClientSize;
                int pointX = s.WorkingArea.Left + ((s.WorkingArea.Width / 2) - shellSize.Width / 2);
                // Position the form's reference point after dropdown animation completes
                int pointY = s.WorkingArea.Top + shellSize.Height;
                // Extend trigger area downward from screen top to make it easier to trigger
                int extendedX = pointX - DropDownRectHorizontalPadding;
                int extendedY = s.WorkingArea.Top;  // Start trigger area at screen top
                int extendedWidth = shellSize.Width + (DropDownRectHorizontalPadding * 2);
                int extendedHeight = pointY - s.WorkingArea.Top + DropDownRectVerticalPadding;  // Extend downward by padding amount
                Point rectPoint = new(extendedX, extendedY);
                Size extendedSize = new(extendedWidth, extendedHeight);

                Rectangle tempRect = new(rectPoint, extendedSize);
                DropDownRects.Add(tempRect);

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

    public static Label[] PopulateLabels()
    {
        List<Label> tempArray = [];
        int fileCount = FileChoices.Count;
        for (int i = 0; i < fileCount; i++)
        {
            Label tempLabel = new()
            {
                BackColor = Properties.Settings.BackgroundColor,
                ForeColor = Properties.Settings.ForegroundColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft Sans Serif", 9.5F),
                Location = new Point(10, (i * 18) + 20),
                Size = new Size(350, 18)
            };

            FileInfo tempFileInfo = FileChoices[i];
            if (tempFileInfo != null)
            {
                if (SearchType == "Movie")
                {
                    tempLabel.Text = "• " + tempFileInfo.Name;
                }
                else
                {
                    tempLabel.Text = $"• {FileExtension().Replace(tempFileInfo.Name, "")}";
                }
            }
            tempArray.Add(tempLabel);
        }
        return [.. tempArray];
    }

    public static void SetBounds(Form obj)
    {
        Screen[] screens = Screen.AllScreens;
        int widthAdder = CalculateWidth();
        int heightDiff = screens[Screen.AllScreens.Length - 1].WorkingArea.Top;

        if (obj.Name == "Shell")
        {
            TopBound = heightDiff;
            LeftBound = widthAdder - (obj.Size.Width / 2);
            RightBound = widthAdder + (obj.Size.Width / 2);
            BottomBound = obj.Size.Height + heightDiff;
        }
        else
        {
            if (ShellInstance != null)
                heightDiff += ShellInstance.Size.Height;
        }

        obj.Location = new Point(widthAdder - (obj.Size.Width / 2), 1 + heightDiff);
    }

    public static void SetCentered(Screen screen, Form obj)
    {
        int heightDiff = screen.Bounds.Top;
        int widthAdder;
        if (screen.Bounds.Left > 0)
        {
            widthAdder = screen.Bounds.Left + ((Math.Abs(screen.Bounds.Right) - Math.Abs(screen.Bounds.Left)) / 2) - (obj.Size.Width / 2);
        }
        else
        {
            widthAdder = ((Math.Abs(screen.Bounds.Right) - Math.Abs(screen.Bounds.Left)) / 2) - (obj.Size.Width / 2);
        }

        if (obj.Name == "Shell")
        {
            TopBound = heightDiff;
            LeftBound = widthAdder;
            RightBound = widthAdder + obj.Size.Width;
            BottomBound = heightDiff + obj.Size.Height;
        }
        else
        {
            if (ShellInstance != null)
                heightDiff += ShellInstance.Size.Height;
        }

        obj.Location = new Point(widthAdder, 1 + heightDiff - 20);
    }

    public static int CalculateWidth()
    {
        Screen[] screens = Screen.AllScreens;
        int widthAdder = 0;

        //Getting/Setting Position on screen
        foreach (Screen s in Screen.AllScreens)
        {
            if (Screen.AllScreens.Length == 3)
            {
                if (s.Bounds.Width == LegacyScreenWidth)
                {
                    continue;
                }
            }
            if (s == screens[Screen.AllScreens.Length - 1])
            {
                widthAdder += (s.Bounds.Width / 2);
            }
            else
            {
                widthAdder += s.Bounds.Width;
            }
        }

        Log($"!!! GlobalVar::WidthAdder() - {widthAdder}");

        return widthAdder;
    }

    public static void UpdateColors()
    {
        Properties.Settings.BackgroundColor = BackColor;
        Properties.Settings.ForegroundColor = FontColor;
        if (ShellInstance != null)
        {
            ShellInstance.ChangeBackgroundColor();
            ShellInstance.ChangeFontColor();
        }
        Log($"&&& GlobalVar::UpdateColors() - Changing Colors: ({BackColor.Name})\t({FontColor.Name})");
    }
    #endregion

    #region Networking Functions
    public static bool TryGetTcpServerCertificate(out X509Certificate2? certificate)
    {
        certificate = tcpServerCertificate;
        if (certificate != null)
        {
            return true;
        }

        if (!TcpTlsEnabled)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(TcpTlsPfxPath))
        {
            Log("### TCP TLS enabled but DESKTOPSHELL_TCP_TLS_PFX not set");
            return false;
        }

        try
        {
            certificate = new X509Certificate2(TcpTlsPfxPath, TcpTlsPfxPassword);
            tcpServerCertificate = certificate;
            return true;
        }
        catch (Exception e)
        {
            Log($"### Failed to load TCP TLS certificate: {e.GetType()}: {e.Message}");
            return false;
        }
    }

    public static string EnsureMessageTerminator(string command)
    {
        if (string.IsNullOrEmpty(command))
        {
            return TcpMessageTerminator;
        }

        if (command.EndsWith("\n", StringComparison.Ordinal))
        {
            return command;
        }

        return command + TcpMessageTerminator;
    }

    public static Stream CreateInboundCommandStream(TcpClient tcpClient)
    {
        NetworkStream networkStream = tcpClient.GetStream();
        networkStream.ReadTimeout = TcpReadTimeoutMs;
        networkStream.WriteTimeout = TcpReadTimeoutMs;

        if (!TcpTlsEnabled)
        {
            return networkStream;
        }

        if (!TryGetTcpServerCertificate(out var certificate) || certificate == null)
        {
            throw new InvalidOperationException("TCP TLS is enabled but server certificate is unavailable");
        }

        var sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false);
        sslStream.AuthenticateAsServer(
            certificate,
            clientCertificateRequired: false,
            enabledSslProtocols: System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
            checkCertificateRevocation: false);
        return sslStream;
    }

    public static Stream CreateOutboundCommandStream(Socket connectedSocket, string serverHost)
    {
        var networkStream = new NetworkStream(connectedSocket, ownsSocket: false)
        {
            ReadTimeout = TcpReadTimeoutMs,
            WriteTimeout = TcpReadTimeoutMs
        };

        if (!TcpTlsEnabled)
        {
            return networkStream;
        }

        string? pinnedThumbprint = TcpTlsPinnedThumbprint;
        RemoteCertificateValidationCallback validator = (sender, cert, chain, sslPolicyErrors) =>
        {
            if (cert == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(pinnedThumbprint))
            {
                var presented = new X509Certificate2(cert);
                string presentedThumbprint = presented.Thumbprint?.Replace(" ", "", StringComparison.OrdinalIgnoreCase) ?? "";
                string expected = pinnedThumbprint.Replace(" ", "", StringComparison.OrdinalIgnoreCase);
                return string.Equals(presentedThumbprint, expected, StringComparison.OrdinalIgnoreCase);
            }

            return sslPolicyErrors == SslPolicyErrors.None;
        };

        var sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false, validator);
        sslStream.AuthenticateAsClient(serverHost);
        return sslStream;
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
                    HostList.Add(hostPortPair);
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

    private static string TrimPassPhrasePrefix(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.StartsWith(PassPhrase, StringComparison.Ordinal))
        {
            return text[PassPhrase.Length..];
        }
        return text;
    }

    private static string? ReadSingleLineResponse(Stream stream)
    {
        if (!stream.CanRead)
        {
            return null;
        }

        var buffer = new byte[1024];
        var collected = new List<byte>(256);
        var idle = Stopwatch.StartNew();

        while (idle.ElapsedMilliseconds < TcpMessageIdleTimeoutMs)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead <= 0)
                {
                    break;
                }

                idle.Restart();

                for (int i = 0; i < bytesRead; i++)
                {
                    byte b = buffer[i];
                    if (b == (byte)'\n')
                    {
                        string raw = Encoding.ASCII.GetString(collected.ToArray());
                        return raw.TrimEnd('\r');
                    }
                    collected.Add(b);

                    if (collected.Count >= TcpBufferSize)
                    {
                        return Encoding.ASCII.GetString(collected.ToArray());
                    }
                }
            }
            catch (IOException)
            {
                if (collected.Count > 0 && idle.ElapsedMilliseconds >= TcpMessageIdleTimeoutMs)
                {
                    return Encoding.ASCII.GetString(collected.ToArray());
                }

                Thread.Sleep(TcpReadDelayMs);
            }
            catch
            {
                break;
            }
        }

        return collected.Count > 0 ? Encoding.ASCII.GetString(collected.ToArray()) : null;
    }

    public static bool TrySendRemoteCommandTcpWithAck(int port, string command, string serverHost)
    {
        try
        {
            using Socket clientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(serverHost, port, TimeSpan.FromSeconds(TcpConnectionTimeoutSeconds));
            if (!clientSocket.Connected)
            {
                Log($"### Socket not connected when trying to send '{command}', closing connection");
                return false;
            }

            using Stream stream = CreateOutboundCommandStream(clientSocket, serverHost);
            WriteRemoteCommand(stream, command, includePassPhrase: true);

            // Wait for server ACK to confirm delivery.
            string? responseRaw = ReadSingleLineResponse(stream);
            string response = TrimPassPhrasePrefix((responseRaw ?? "").Trim());
            if (string.Equals(response, "ack", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(response, "lol", StringComparison.OrdinalIgnoreCase))
            {
                Log("### Remote returned 'lol' (likely bad passphrase)");
            }
            else
            {
                Log($"### No ACK received (response='{responseRaw ?? ""}')");
            }

            try
            {
                clientSocket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                // ignore
            }

            return false;
        }
        catch (Exception e)
        {
            Log($"### GlobalVar::SendRemoteCommand() - {e.GetType()}: {e.Message}");
            return false;
        }
    }

    public static void SendRemoteCommandWithQueueFallback(string targetName, int port, string command)
    {
        const string tcpHost = "msg.dlamanna.com";

        bool delivered = TrySendRemoteCommandTcpWithAck(port, command, tcpHost);
        if (delivered)
        {
            Log($"!!! Delivered command to {targetName} via TCP");
            return;
        }

        if (!QueueEnabled)
        {
            Log($"### TCP delivery failed and queue disabled. target={targetName}");
            return;
        }

        bool queued = MessageQueueClient.TryEnqueue(targetName, command);
        if (queued)
        {
            ToolTip("TCP", $"Message queued for {targetName}:\n{command}");
        }
        else
        {
            Log($"### Failed to queue message for {targetName}");
        }
    }

    public static void WriteRemoteCommand(Stream stream, string command, bool includePassPhrase)
    {
        try
        {
            if (!stream.CanWrite)
            {
                Log("### GlobalVar::WriteRemoteCommand: stream not writable");
                return;
            }

            string terminated = EnsureMessageTerminator(command);
            string payload = includePassPhrase ? PassPhrase + terminated : terminated;
            byte[] buffer = Encoding.ASCII.GetBytes(payload);
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
        }
        catch (Exception e)
        {
            Log($"### GlobalVar::WriteRemoteCommand() - {e.GetType()}: {e.Message}");
        }
    }

    public static void SendRemoteCommand(TcpClient client, string command)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            if (stream.CanWrite)
            {
                bool includePassPhrase = !command.Equals("lol\r\n");
                WriteRemoteCommand(stream, command, includePassPhrase);
                if (client.Client.RemoteEndPoint is IPEndPoint endPoint)
                {
                    Log($"!!! Sent command: '{command.Trim()}' to: {endPoint.Address}:{endPoint.Port}");
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
        foreach (KeyValuePair<string, string> hostPair in HostList)
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
    #endregion

    #region Utility Functions
    public static string GetSoundFolderLocation()
    {
        return $"{Path.GetDirectoryName(AppContext.BaseDirectory)}\\Sounds";
    }
    public static bool KillProcess(string processName)
    {
        bool isRunning = false;
        try
        {
            Process[] processList = Process.GetProcessesByName(processName);
            if (processList.Length >= 1)
            {
                foreach (Process p in processList)
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
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public static void ResetLog()
    {
        try
        {
            using (File.Create("DesktopShell.log")) { }
            ;
        }
        catch (Exception e)
        {
            ToolTip("Error", $"GlobalVar::ResetLog() - {e.GetType()}\n{e.Message}");
        }
    }

    public static void Run(string path, string arguments)
    {
        Process p = new();
        p.StartInfo.Arguments = arguments;
        p.StartInfo.FileName = path;
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

    public static void ToolTip(string title, string body)
    {
        Log($"\t\t@@@ ToolTipping: {title} // {body}");
        Run($@"{CurrentAssemblyDirectory}\Bin\ToolTipper.exe", $"{title} {body}");
    }
    #endregion
}