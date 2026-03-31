using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace DesktopShell.VR;

public class VRCommandHandler
{
    private readonly VRGameListService _gameListService;
    private readonly VROrchestrator _orchestrator;

    // Post-launch hooks: appId → action to run after the game_launch step
    private static readonly Dictionary<int, Action> PostLaunchHooks = new()
    {
        [1086940] = OpenDungeonMasterTerminal, // Baldur's Gate 3
    };

    public VRCommandHandler(VRGameListService gameListService, VROrchestrator orchestrator)
    {
        _gameListService = gameListService;
        _orchestrator = orchestrator;
    }

    public async Task HandleAsync(string command, Stream clientStream)
    {
        try
        {
            if (command == "vr-games")
            {
                await HandleVrGamesAsync(clientStream);
            }
            else if (command.StartsWith("vr-launch "))
            {
                string appIdStr = command["vr-launch ".Length..].Trim();
                if (int.TryParse(appIdStr, out int appId))
                    await HandleVrLaunchAsync(appId, clientStream);
                else
                    WriteJsonLine(clientStream, new VrLaunchStep { Status = "failed", Error = $"Invalid app ID: {appIdStr}" });
            }
            else if (command == "vr-devices")
            {
                await HandleVrDevicesAsync(clientStream);
            }
            else if (command == "vr-status")
            {
                await HandleVrStatusAsync(clientStream);
            }
            else if (command == "vr-kill")
            {
                HandleVrKill(clientStream);
            }
            else
            {
                WriteJsonLine(clientStream, new VrLaunchStep { Status = "failed", Error = $"Unknown VR command: {command}" });
            }
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### VRCommandHandler: {e.GetType()}: {e.Message}");
            try
            {
                WriteJsonLine(clientStream, new VrLaunchStep { Status = "failed", Error = "Internal error" });
            }
            catch { /* stream may be broken */ }
        }
    }

    private async Task HandleVrGamesAsync(Stream clientStream)
    {
        var games = _gameListService.GetVrGames();
        long cFreeSpace = VRGameListService.GetCDriveFreeSpace();
        var response = new
        {
            games = games.Select(g => new
            {
                appid = g.AppId,
                name = g.Name,
                installDrive = g.InstallDrive,
                installSizeBytes = g.InstallSizeBytes
            }).ToArray(),
            cDriveFreeSpaceBytes = cFreeSpace
        };
        string json = JsonSerializer.Serialize(response);
        GlobalVar.WriteRemoteCommand(clientStream, json, includePassPhrase: true);
        await Task.CompletedTask;
    }

    private async Task HandleVrLaunchAsync(int appId, Stream clientStream)
    {
        // Look up install path for process confirmation
        var games = _gameListService.GetVrGames();
        var game = games.FirstOrDefault(g => g.AppId == appId);
        string fullInstallPath = game?.InstallPath ?? "";

        await foreach (var step in _orchestrator.LaunchAsync(appId, fullInstallPath))
        {
            WriteJsonLine(clientStream, step);

            // Fire post-launch hook after the game has been sent to Steam
            if (step.Step == "game_launch" && step.Status == "ok" && PostLaunchHooks.TryGetValue(appId, out var hook))
            {
                try
                {
                    hook();
                    GlobalVar.Log($"$$$ VR post-launch hook fired for appId {appId}");
                }
                catch (Exception e)
                {
                    GlobalVar.Log($"### VR post-launch hook failed for appId {appId}: {e.Message}");
                }
            }
        }
    }

    private async Task HandleVrDevicesAsync(Stream clientStream)
    {
        var status = await _orchestrator.GetDeviceStatusAsync();
        string json = JsonSerializer.Serialize(status);
        GlobalVar.WriteRemoteCommand(clientStream, json, includePassPhrase: true);
    }

    private async Task HandleVrStatusAsync(Stream clientStream)
    {
        var status = _orchestrator.GetStatus();
        string json = JsonSerializer.Serialize(status);
        GlobalVar.WriteRemoteCommand(clientStream, json, includePassPhrase: true);
        await Task.CompletedTask;
    }

    private void HandleVrKill(Stream clientStream)
    {
        var games = _gameListService.GetVrGames();
        var installDirs = games
            .Where(g => !string.IsNullOrEmpty(g.InstallPath))
            .Select(g => g.InstallPath!)
            .Distinct()
            .ToList();

        var result = _orchestrator.KillVrSession(installDirs);
        GlobalVar.Log($"$$$ VR kill: {result.Killed.Count} processes terminated");
        GlobalVar.WriteRemoteCommand(clientStream, result.ToJson(), includePassPhrase: false);
    }

    private static void WriteJsonLine(Stream stream, VrLaunchStep step)
    {
        GlobalVar.WriteRemoteCommand(stream, step.ToJson(), includePassPhrase: false);
    }

    private const string WindowsTerminalPreview =
        @"C:\Users\phuze\AppData\Local\Microsoft\WindowsApps\Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe\wt.exe";

    private const string AudioBridgeExe =
        @"C:\Users\phuze\Dropbox\Programming\BG3DungeonMaster\publish-new\BG3DungeonMaster.AudioBridge.exe";

    private static void OpenDungeonMasterTerminal()
    {
        // Opens wsl_claude_dungeonmaster profile in Windows Terminal Preview.
        // With windowingBehavior=useAnyExisting, wt -w 0 adds a tab if Terminal
        // is already open, or creates a new window if not.
        Process.Start(new ProcessStartInfo(WindowsTerminalPreview, "-w 0 new-tab -p \"wsl_claude_dungeonmaster\"")
        {
            UseShellExecute = true,
            CreateNoWindow = true
        });

        // Launch AudioBridge for voice input (wake word + speech capture).
        // Runs as a native Windows process for WASAPI audio access.
        // Skip if already running (survives MCP server restarts).
        if (Process.GetProcessesByName("BG3DungeonMaster.AudioBridge").Length == 0)
        {
            try
            {
                Process.Start(new ProcessStartInfo(AudioBridgeExe)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(AudioBridgeExe)!
                });
                GlobalVar.Log("$$$ AudioBridge started for DM voice pipeline");
            }
            catch (Exception e)
            {
                GlobalVar.Log($"### AudioBridge failed to start: {e.Message}");
            }
        }
        else
        {
            GlobalVar.Log("$$$ AudioBridge already running, skipping launch");
        }

        // Refocus BG3 after SteamVR steals focus during init.
        // Phase 1: wait for BG3 window to exist (up to 90s — game loads slowly).
        // Phase 2: once found, refocus every 3s for 30s to outlast SteamVR's focus grabs.
        _ = Task.Run(async () =>
        {
            // Phase 1: wait for BG3 to have a window
            IntPtr hwnd = IntPtr.Zero;
            for (int i = 0; i < 45 && hwnd == IntPtr.Zero; i++)
            {
                await Task.Delay(2000);
                hwnd = GetBg3WindowHandle();
            }

            if (hwnd == IntPtr.Zero)
            {
                GlobalVar.Log("### BG3 window not found after 90s, giving up on refocus");
                return;
            }

            // Phase 2: repeatedly reclaim focus
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(3000);
                SetForegroundWindow(hwnd);
            }
            GlobalVar.Log("$$$ BG3 refocus sequence complete");
        });
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private static IntPtr GetBg3WindowHandle()
    {
        var bg3 = Process.GetProcessesByName("bg3")
            .Concat(Process.GetProcessesByName("bg3_dx11"))
            .FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
        return bg3?.MainWindowHandle ?? IntPtr.Zero;
    }
}
