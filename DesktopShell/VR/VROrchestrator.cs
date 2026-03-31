using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopShell.VR;

public class VrLaunchStep
{
    [JsonPropertyName("step")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Step { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; init; }

    [JsonPropertyName("process")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Process { get; init; }

    [JsonPropertyName("appid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int AppId { get; init; }

    public string ToJson() => JsonSerializer.Serialize(this);
}

public interface IProcessManager
{
    Task<bool> IsHmdPresentAsync();
    bool IsProcessRunning(string processName);
    void LaunchSteamVr();
    Task LaunchSteamGameAsync(int appId);
    Task<string?> WaitForGameProcessAsync(int appId, string installDir, int timeoutMs, CancellationToken ct);
    Task<string?> RunVrCmdAsync(string args, int timeoutMs = 5_000);
    List<string> ForceKillByName(IEnumerable<string> processNames);
    List<string> FindCandidateProcessNames(string installDir);
}

public class ProcessManager : IProcessManager
{
    private const string VrCmdPath = @"C:\Program Files (x86)\Steam\steamapps\common\SteamVR\bin\win64\vrcmd.exe";

    private const int HmdCheckTimeoutMs = 5_000;

    public async Task<bool> IsHmdPresentAsync()
    {
        try
        {
            if (!File.Exists(VrCmdPath)) return false;

            var psi = new ProcessStartInfo(VrCmdPath, "--pollhmdpresent")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) return false;

            using var cts = new CancellationTokenSource(HmdCheckTimeoutMs);
            try
            {
                await proc.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { proc.Kill(); } catch { }
                GlobalVar.Log("### VR: vrcmd.exe timed out checking HMD presence");
                return false;
            }

            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public bool IsProcessRunning(string processName)
    {
        try
        {
            return System.Diagnostics.Process.GetProcessesByName(processName).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public void LaunchSteamVr()
    {
        try
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo("steam://run/250820") { UseShellExecute = true });
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### VR: Failed to launch SteamVR: {e.Message}");
        }
    }

    public async Task LaunchSteamGameAsync(int appId)
    {
        try
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo($"steam://launch/{appId}/VR") { UseShellExecute = true });
            await Task.Delay(1000); // Give Steam a moment to register the launch
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### VR: Failed to launch game {appId}: {e.Message}");
        }
    }

    public async Task<string?> WaitForGameProcessAsync(int appId, string installDir, int timeoutMs, CancellationToken ct)
    {
        // Scan install directory for candidate .exe files
        var candidateExes = FindCandidateExes(installDir);
        if (candidateExes.Count == 0) return null;

        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs && !ct.IsCancellationRequested)
        {
            foreach (string exeName in candidateExes)
            {
                string processName = Path.GetFileNameWithoutExtension(exeName);
                if (IsProcessRunning(processName))
                    return exeName;
            }
            await Task.Delay(2000, ct);
        }

        return null;
    }

    public async Task<string?> RunVrCmdAsync(string args, int timeoutMs = 5_000)
    {
        try
        {
            if (!File.Exists(VrCmdPath)) return null;

            var psi = new ProcessStartInfo(VrCmdPath, args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) return null;

            using var cts = new CancellationTokenSource(timeoutMs);
            try
            {
                string output = await proc.StandardOutput.ReadToEndAsync(cts.Token);
                await proc.WaitForExitAsync(cts.Token);
                return output;
            }
            catch (OperationCanceledException)
            {
                try { proc.Kill(); } catch { }
                return null;
            }
        }
        catch { return null; }
    }

    public List<string> ForceKillByName(IEnumerable<string> processNames)
    {
        var killed = new List<string>();
        foreach (var name in processNames)
        {
            try
            {
                var procs = System.Diagnostics.Process.GetProcessesByName(name);
                foreach (var proc in procs)
                {
                    try
                    {
                        proc.Kill();
                        killed.Add($"{proc.ProcessName}.exe");
                        GlobalVar.Log($"$$$ VR kill: terminated {proc.ProcessName}.exe (PID {proc.Id})");
                    }
                    catch (Exception e)
                    {
                        GlobalVar.Log($"### VR kill: failed to terminate {name}: {e.Message}");
                    }
                }
            }
            catch { /* process enumeration failed */ }
        }
        return killed;
    }

    public List<string> FindCandidateProcessNames(string installDir)
    {
        return FindCandidateExes(installDir)
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(n => n != null)
            .Cast<string>()
            .ToList();
    }

    private static List<string> FindCandidateExes(string installDir)
    {
        try
        {
            if (!Directory.Exists(installDir)) return [];

            return Directory.GetFiles(installDir, "*.exe", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(f => f != null &&
                    !f.StartsWith("Unins", StringComparison.OrdinalIgnoreCase) &&
                    !f.StartsWith("UnityCrash", StringComparison.OrdinalIgnoreCase) &&
                    !f.Contains("Launcher", StringComparison.OrdinalIgnoreCase) &&
                    !f.Contains("Setup", StringComparison.OrdinalIgnoreCase) &&
                    !f.Contains("Crash", StringComparison.OrdinalIgnoreCase))
                .Cast<string>()
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}

public class VROrchestrator
{
    private readonly IProcessManager _process;
    private int _launching; // 0 = idle, 1 = launching

    // Configurable timeouts
    public int SteamVrStartTimeoutMs { get; set; } = 20_000;
    public int GameProcessConfirmTimeoutMs { get; set; } = 45_000;

    public VROrchestrator(IProcessManager process)
    {
        _process = process;
    }

    public async IAsyncEnumerable<VrLaunchStep> LaunchAsync(
        int appId,
        string installDir = "",
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Duplicate guard
        if (Interlocked.CompareExchange(ref _launching, 1, 0) != 0)
        {
            yield return new VrLaunchStep { Status = "already_launching", AppId = appId };
            yield break;
        }

        try
        {
            // Step 1: Headset check
            bool hmdPresent = await _process.IsHmdPresentAsync();
            if (!hmdPresent)
            {
                yield return new VrLaunchStep { Step = "headset_check", Status = "failed", Error = "Headset not detected" };
                yield break;
            }
            yield return new VrLaunchStep { Step = "headset_check", Status = "ok" };

            // Step 2: SteamVR
            if (!_process.IsProcessRunning("vrserver"))
            {
                _process.LaunchSteamVr();
                var sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < SteamVrStartTimeoutMs && !ct.IsCancellationRequested)
                {
                    if (_process.IsProcessRunning("vrserver")) break;
                    await Task.Delay(1000, ct);
                }

                if (!_process.IsProcessRunning("vrserver"))
                {
                    yield return new VrLaunchStep { Step = "steamvr_start", Status = "failed", Error = "SteamVR failed to start" };
                    yield break;
                }
            }
            yield return new VrLaunchStep { Step = "steamvr_start", Status = "ok" };

            // Step 3: Launch game
            await _process.LaunchSteamGameAsync(appId);
            yield return new VrLaunchStep { Step = "game_launch", Status = "ok" };

            // Step 4: Process confirmation (best-effort)
            string? processName = await _process.WaitForGameProcessAsync(appId, installDir, GameProcessConfirmTimeoutMs, ct);
            if (processName != null)
            {
                yield return new VrLaunchStep { Step = "process_confirm", Status = "ok", Process = processName };
            }
            else
            {
                yield return new VrLaunchStep { Step = "process_confirm", Status = "timeout", Message = "Game may have launched — check headset" };
            }

            yield return new VrLaunchStep { Status = "complete" };
        }
        finally
        {
            Interlocked.Exchange(ref _launching, 0);
        }
    }

    public VrLaunchStep GetStatus()
    {
        return new VrLaunchStep
        {
            Status = "ok",
            Step = "status",
            Process = _launching == 1 ? "launching" : null
        };
    }

    private const string SteamVrSettingsPath = @"C:\Program Files (x86)\Steam\config\steamvr.vrsettings";
    private const string LighthouseDbPath = @"C:\Program Files (x86)\Steam\config\lighthouse\lighthousedb.json";

    public Task<VrDeviceStatusResult> GetDeviceStatusAsync()
    {
        var result = new VrDeviceStatusResult
        {
            SteamVrRunning = _process.IsProcessRunning("vrserver"),
        };

        try
        {
            if (File.Exists(SteamVrSettingsPath))
            {
                var settings = JsonDocument.Parse(File.ReadAllText(SteamVrSettingsPath));
                if (settings.RootElement.TryGetProperty("LastKnown", out var lastKnown))
                {
                    result.Headset = new VrHeadsetInfo
                    {
                        Model = lastKnown.TryGetProperty("HMDModel", out var m) ? m.GetString() : null,
                        Manufacturer = lastKnown.TryGetProperty("HMDManufacturer", out var mfr) ? mfr.GetString() : null,
                        Serial = lastKnown.TryGetProperty("HMDSerialNumber", out var s) ? s.GetString() : null,
                    };
                }
            }
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### VR: Failed to read steamvr.vrsettings: {e.Message}");
        }

        try
        {
            if (File.Exists(LighthouseDbPath))
            {
                var db = JsonDocument.Parse(File.ReadAllText(LighthouseDbPath));

                // Base stations
                if (db.RootElement.TryGetProperty("base_stations", out var stations))
                {
                    foreach (var station in stations.EnumerateArray())
                    {
                        long serialNum = 0;
                        if (station.TryGetProperty("config", out var config) &&
                            config.TryGetProperty("serialNumber", out var sn))
                        {
                            serialNum = sn.GetInt64();
                        }

                        // Get the most recent dynamic state (last entry in the array)
                        int mode = 0;
                        int faults = 0;
                        long lastSeen = 0;
                        if (station.TryGetProperty("dynamic_states", out var states))
                        {
                            JsonElement? latest = null;
                            foreach (var ds in states.EnumerateArray())
                                latest = ds;

                            if (latest.HasValue)
                            {
                                if (latest.Value.TryGetProperty("time_last_seen", out var tls))
                                    long.TryParse(tls.GetString(), out lastSeen);

                                if (latest.Value.TryGetProperty("dynamic_state", out var dynState))
                                {
                                    mode = dynState.TryGetProperty("basestation_mode", out var bm) ? bm.GetInt32() : 0;
                                    faults = dynState.TryGetProperty("faults", out var f) ? f.GetInt32() : 0;
                                }
                            }
                        }

                        result.BaseStations.Add(new VrBaseStationInfo
                        {
                            Serial = $"LHB-{serialNum:X8}",
                            LastSeen = lastSeen,
                            Mode = mode,
                            Faults = faults,
                        });
                    }
                }

                // Controllers from known_objects
                if (db.RootElement.TryGetProperty("known_objects", out var objects))
                {
                    int controllerCount = 0;
                    foreach (var obj in objects.EnumerateArray())
                    {
                        if (obj.TryGetProperty("deviceClass", out var dc) && dc.GetString() == "controller")
                            controllerCount++;
                    }
                    result.Controllers = controllerCount;
                }
            }
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### VR: Failed to read lighthousedb.json: {e.Message}");
        }

        return Task.FromResult(result);
    }
}

public class VrDeviceStatusResult
{
    [JsonPropertyName("steamVrRunning")]
    public bool SteamVrRunning { get; set; }

    [JsonPropertyName("headset")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VrHeadsetInfo? Headset { get; set; }

    [JsonPropertyName("baseStations")]
    public List<VrBaseStationInfo> BaseStations { get; set; } = new();

    [JsonPropertyName("controllers")]
    public int Controllers { get; set; }
}

public class VrHeadsetInfo
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; set; }

    [JsonPropertyName("serial")]
    public string? Serial { get; set; }
}

public class VrBaseStationInfo
{
    [JsonPropertyName("serial")]
    public string Serial { get; set; } = "";

    [JsonPropertyName("lastSeen")]
    public long LastSeen { get; set; }

    [JsonPropertyName("mode")]
    public int Mode { get; set; }

    [JsonPropertyName("faults")]
    public int Faults { get; set; }
}
