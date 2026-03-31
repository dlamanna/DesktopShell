using System.IO;
using DesktopShell.VR;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopShell.Tests.VR;

[TestClass]
public class VROrchestratorTests
{
    [TestMethod]
    public async Task LaunchVrGame_AllStepsSucceed_ReportsComplete()
    {
        var process = new FakeProcessManager(
            hmdPresent: true,
            vrServerRunning: true,
            gameProcessName: "HLA.exe"
        );

        var orchestrator = new VROrchestrator(process);
        var steps = new List<VrLaunchStep>();

        await foreach (var step in orchestrator.LaunchAsync(546560))
            steps.Add(step);

        steps.Should().Contain(s => s.Step == "headset_check" && s.Status == "ok");
        steps.Should().Contain(s => s.Step == "steamvr_start" && s.Status == "ok");
        steps.Should().Contain(s => s.Step == "game_launch" && s.Status == "ok");
        steps.Should().Contain(s => s.Step == "process_confirm" && s.Status == "ok");
        steps.Last().Status.Should().Be("complete");
    }

    [TestMethod]
    public async Task LaunchVrGame_HeadsetNotDetected_FailsAtFirstStep()
    {
        var process = new FakeProcessManager(hmdPresent: false);

        var orchestrator = new VROrchestrator(process);
        var steps = new List<VrLaunchStep>();

        await foreach (var step in orchestrator.LaunchAsync(546560))
            steps.Add(step);

        steps.Should().HaveCount(1);
        steps[0].Step.Should().Be("headset_check");
        steps[0].Status.Should().Be("failed");
        steps[0].Error.Should().Contain("not detected");
    }

    [TestMethod]
    public async Task LaunchVrGame_SteamVrNotRunning_StartsIt()
    {
        var process = new FakeProcessManager(
            hmdPresent: true,
            vrServerRunning: false,
            vrServerStartsAfterLaunch: true,
            gameProcessName: "game.exe"
        );

        var orchestrator = new VROrchestrator(process);
        var steps = new List<VrLaunchStep>();

        await foreach (var step in orchestrator.LaunchAsync(546560))
            steps.Add(step);

        process.SteamVrLaunched.Should().BeTrue();
        steps.Should().Contain(s => s.Step == "steamvr_start" && s.Status == "ok");
    }

    [TestMethod]
    public async Task LaunchVrGame_DuplicateLaunch_ReturnsAlreadyLaunching()
    {
        var process = new FakeProcessManager(
            hmdPresent: true,
            vrServerRunning: true,
            gameProcessName: "game.exe",
            launchDelayMs: 5000 // Simulate slow launch
        );

        var orchestrator = new VROrchestrator(process);

        // Start first launch (don't await)
        var firstLaunch = Task.Run(async () =>
        {
            await foreach (var _ in orchestrator.LaunchAsync(546560)) { }
        });

        // Small delay to ensure first launch has started
        await Task.Delay(50);

        // Second launch should return already_launching
        var steps = new List<VrLaunchStep>();
        await foreach (var step in orchestrator.LaunchAsync(546560))
            steps.Add(step);

        steps.Should().HaveCount(1);
        steps[0].Status.Should().Be("already_launching");

        await firstLaunch;
    }

    [TestMethod]
    public async Task KillVrSession_AfterLaunch_KillsGameAndSteamVr()
    {
        var process = new FakeProcessManager(
            hmdPresent: true,
            vrServerRunning: true,
            gameProcessName: "bg3_dx11.exe"
        );

        var orchestrator = new VROrchestrator(process);

        // Launch first so the orchestrator tracks the game process
        await foreach (var _ in orchestrator.LaunchAsync(1086940)) { }

        orchestrator.LastGameProcess.Should().Be("bg3_dx11");

        var result = orchestrator.KillVrSession([]);
        result.Status.Should().Be("ok");
        result.Killed.Should().Contain("bg3_dx11.exe");
        result.Killed.Should().Contain("vrserver.exe");
    }

    [TestMethod]
    public void KillVrSession_NothingRunning_ReturnsEmptyKilled()
    {
        var process = new FakeProcessManager(
            hmdPresent: false,
            vrServerRunning: false
        );

        var orchestrator = new VROrchestrator(process);

        var result = orchestrator.KillVrSession([]);
        result.Status.Should().Be("ok");
        result.Killed.Should().BeEmpty();
    }

    [TestMethod]
    public async Task KillVrSession_ResetsLaunchingFlag()
    {
        var process = new FakeProcessManager(
            hmdPresent: true,
            vrServerRunning: true,
            gameProcessName: "game.exe",
            launchDelayMs: 10_000
        );

        var orchestrator = new VROrchestrator(process);

        // Start a long-running launch
        var launchTask = Task.Run(async () =>
        {
            await foreach (var _ in orchestrator.LaunchAsync(546560)) { }
        });
        await Task.Delay(50);

        // Kill should reset the launching flag
        orchestrator.KillVrSession([]);

        // Should be able to get status without "launching"
        var status = orchestrator.GetStatus();
        status.Process.Should().BeNull();

        await Task.Delay(100); // let the background launch finish
    }
}

public class FakeProcessManager : IProcessManager
{
    private readonly bool _hmdPresent;
    private readonly bool _vrServerRunning;
    private readonly bool _vrServerStartsAfterLaunch;
    private readonly string? _gameProcessName;
    private readonly int _launchDelayMs;

    public bool SteamVrLaunched { get; private set; }
    public List<string> KilledProcesses { get; } = new();

    public FakeProcessManager(
        bool hmdPresent = true,
        bool vrServerRunning = true,
        bool vrServerStartsAfterLaunch = false,
        string? gameProcessName = null,
        int launchDelayMs = 0)
    {
        _hmdPresent = hmdPresent;
        _vrServerRunning = vrServerRunning;
        _vrServerStartsAfterLaunch = vrServerStartsAfterLaunch;
        _gameProcessName = gameProcessName;
        _launchDelayMs = launchDelayMs;
    }

    public Task<bool> IsHmdPresentAsync()
        => Task.FromResult(_hmdPresent);

    public bool IsProcessRunning(string name)
        => name == "vrserver" ? (_vrServerRunning || SteamVrLaunched && _vrServerStartsAfterLaunch) : false;

    public void LaunchSteamVr()
        => SteamVrLaunched = true;

    public async Task LaunchSteamGameAsync(int appId)
    {
        if (_launchDelayMs > 0) await Task.Delay(_launchDelayMs);
    }

    public Task<string?> WaitForGameProcessAsync(int appId, string installDir, int timeoutMs, CancellationToken ct)
        => Task.FromResult(_gameProcessName);

    public Task<string?> RunVrCmdAsync(string args, int timeoutMs = 5_000)
        => Task.FromResult<string?>(null);

    public List<string> ForceKillByName(IEnumerable<string> processNames)
    {
        var killed = new List<string>();
        foreach (var name in processNames)
        {
            if (IsProcessRunning(name) ||
                (_gameProcessName != null && name == Path.GetFileNameWithoutExtension(_gameProcessName)))
            {
                killed.Add($"{name}.exe");
                KilledProcesses.Add(name);
            }
        }
        return killed;
    }

    public List<string> FindCandidateProcessNames(string installDir)
        => _gameProcessName != null ? [Path.GetFileNameWithoutExtension(_gameProcessName)!] : [];
}
