using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;

namespace DesktopShell;

internal static class BG3HealthHandler
{
    private static readonly string[] StateFileNames =
    [
        "zone_state.json",
        "levelup_state.json",
        "vendor_state.json",
        "checklist.json",
        "debug.log",
    ];

    private static readonly string StateFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Larian Studios", "Baldur's Gate 3", "Script Extender", "DungeonMaster");

    private const int XsOverlayPort = 42070;
    private const int FreshnessThresholdSeconds = 300; // 5 minutes

    internal static string CheckHealth()
    {
        var dmStatus = ReadDmStatus();
        bool dmRunning = dmStatus.Alive;
        bool mcpServer = dmStatus.Alive;
        bool tts = dmStatus.Tts;
        bool xsOverlay = CheckXsOverlay();
        var stateFiles = CheckStateFiles();
        var vrService = CheckVrService();
        var binaryDates = GetBinaryDates();

        // SE mod is "loaded" if state files exist AND at least one was written in the last 5 minutes
        bool seModLoaded = stateFiles.Values.Any(sf => sf.Exists && sf.AgeSeconds >= 0 && sf.AgeSeconds <= FreshnessThresholdSeconds);

        bool hasIssues = !dmRunning || !mcpServer || !seModLoaded || !vrService.Ok;

        var result = new
        {
            dmRunning,
            mcpServer,
            tts,
            xsOverlay,
            seModLoaded,
            vrServiceOk = vrService.Ok,
            vrServiceError = vrService.Error,
            desktopShellBuildDate = binaryDates.DesktopShell,
            vrServiceBuildDate = binaryDates.VrService,
            stateFiles = stateFiles.ToDictionary(
                kv => kv.Key,
                kv => new { exists = kv.Value.Exists, ageSeconds = kv.Value.AgeSeconds }),
            verdict = hasIssues ? "issues" : "ok",
        };

        return JsonSerializer.Serialize(result);
    }

    private record DmStatus(bool Alive, bool Tts);

    /// <summary>
    /// Reads dm_status.json written by the MCP server heartbeat (every 30s).
    /// If the file is fresh, the server is alive. TTS config comes from the file contents.
    /// </summary>
    private static DmStatus ReadDmStatus()
    {
        try
        {
            string statusPath = Path.Combine(StateFilePath, "dm_status.json");
            if (!File.Exists(statusPath)) return new(false, false);

            int age = (int)(DateTime.UtcNow - File.GetLastWriteTimeUtc(statusPath)).TotalSeconds;
            if (age > FreshnessThresholdSeconds) return new(false, false);

            string json = File.ReadAllText(statusPath);
            using var doc = JsonDocument.Parse(json);
            bool tts = doc.RootElement.TryGetProperty("tts", out var ttsProp) && ttsProp.GetBoolean();

            return new(true, tts);
        }
        catch
        {
            return new(false, false);
        }
    }

    private static bool CheckXsOverlay()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var result = socket.BeginConnect("127.0.0.1", XsOverlayPort, null, null);
            bool connected = result.AsyncWaitHandle.WaitOne(2_000);
            if (connected)
            {
                socket.EndConnect(result);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static (bool Ok, string? Error) CheckVrService()
    {
        try
        {
            string vrServicePath = Path.Combine(AppContext.BaseDirectory, "Bin", "VRService.exe");
            if (!File.Exists(vrServicePath))
                return (false, "VRService.exe not found");

            var psi = new ProcessStartInfo(vrServicePath, "vr-status")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(psi);
            if (process == null)
                return (false, "Failed to start VRService");

            string output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(5_000))
            {
                try { process.Kill(); } catch { }
                return (false, "VRService timed out");
            }

            if (process.ExitCode != 0)
                return (false, $"Exit code {process.ExitCode}");

            // Verify valid JSON response
            using var doc = JsonDocument.Parse(output);
            return (true, null);
        }
        catch (JsonException)
        {
            return (false, "Invalid JSON response");
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }
    }

    private static (string? DesktopShell, string? VrService) GetBinaryDates()
    {
        string? ds = null, vr = null;
        try
        {
            string dsPath = Path.Combine(AppContext.BaseDirectory, "DesktopShell.exe");
            if (File.Exists(dsPath))
                ds = File.GetLastWriteTimeUtc(dsPath).ToString("o");
        }
        catch { }
        try
        {
            string vrPath = Path.Combine(AppContext.BaseDirectory, "Bin", "VRService.exe");
            if (File.Exists(vrPath))
                vr = File.GetLastWriteTimeUtc(vrPath).ToString("o");
        }
        catch { }
        return (ds, vr);
    }

    private static Dictionary<string, (bool Exists, int AgeSeconds)> CheckStateFiles()
    {
        var result = new Dictionary<string, (bool, int)>();
        foreach (string fileName in StateFileNames)
        {
            string filePath = Path.Combine(StateFilePath, fileName);
            if (File.Exists(filePath))
            {
                int age = (int)(DateTime.UtcNow - File.GetLastWriteTimeUtc(filePath)).TotalSeconds;
                result[fileName] = (true, age);
            }
            else
            {
                result[fileName] = (false, -1);
            }
        }
        return result;
    }
}
