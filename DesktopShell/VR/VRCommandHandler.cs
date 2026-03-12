using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DesktopShell.VR;

public class VRCommandHandler
{
    private readonly VRGameListService _gameListService;
    private readonly VROrchestrator _orchestrator;

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
            else if (command == "vr-status")
            {
                await HandleVrStatusAsync(clientStream);
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
        var response = new { games = games.Select(g => new { appid = g.AppId, name = g.Name }).ToArray() };
        string json = JsonSerializer.Serialize(response);
        GlobalVar.WriteRemoteCommand(clientStream, json, includePassPhrase: true);
        await Task.CompletedTask;
    }

    private async Task HandleVrLaunchAsync(int appId, Stream clientStream)
    {
        // Look up install dir for process confirmation
        var games = _gameListService.GetVrGames();
        var game = games.FirstOrDefault(g => g.AppId == appId);
        string installDir = game?.InstallDir ?? "";

        // Resolve full install path from Steam libraries
        string fullInstallPath = ResolveInstallPath(appId, installDir);

        await foreach (var step in _orchestrator.LaunchAsync(appId, fullInstallPath))
        {
            WriteJsonLine(clientStream, step);
        }
    }

    private async Task HandleVrStatusAsync(Stream clientStream)
    {
        var status = _orchestrator.GetStatus();
        string json = JsonSerializer.Serialize(status);
        GlobalVar.WriteRemoteCommand(clientStream, json, includePassPhrase: true);
        await Task.CompletedTask;
    }

    private string ResolveInstallPath(int appId, string installDir)
    {
        if (string.IsNullOrEmpty(installDir)) return "";

        // Check common Steam library locations
        string[] steamPaths = [
            @"C:\Program Files (x86)\Steam\steamapps\common",
            @"D:\SteamLibrary\steamapps\common"
        ];

        foreach (string basePath in steamPaths)
        {
            string fullPath = Path.Combine(basePath, installDir);
            if (Directory.Exists(fullPath))
                return fullPath;
        }

        return "";
    }

    private static void WriteJsonLine(Stream stream, VrLaunchStep step)
    {
        GlobalVar.WriteRemoteCommand(stream, step.ToJson(), includePassPhrase: false);
    }
}
