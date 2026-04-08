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

        // SE mod is "loaded" if state files exist AND at least one was written in the last 5 minutes
        bool seModLoaded = stateFiles.Values.Any(sf => sf.Exists && sf.AgeSeconds >= 0 && sf.AgeSeconds <= FreshnessThresholdSeconds);

        bool hasIssues = !dmRunning || !mcpServer || !seModLoaded;

        var result = new
        {
            dmRunning,
            mcpServer,
            tts,
            xsOverlay,
            seModLoaded,
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
