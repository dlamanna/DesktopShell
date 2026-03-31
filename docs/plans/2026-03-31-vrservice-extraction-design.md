# VRService Extraction Design

**Date**: 2026-03-31
**Status**: Approved

## Goal

Extract DesktopShell's inline VR module (4 files in `VR/`) into a separate console executable (`VRService.exe`) so that:
- DesktopShell stays a lean TCP command broker
- VR functionality is fully optional (not shipped in the DesktopShell binary)
- Follows existing pattern where `Bin/` tools (ToolTipper, PasswordTabula, etc.) are independent projects

## Architecture

```
Phone (VRLauncher app)
    | TLS/TCP
    v
DesktopShell (TCP broker)
    | spawns process, pipes stdout/stderr
    v
Bin\VRService.exe (CLI tool)
    | launches games, queries devices
    v
Steam / SteamVR / Windows
```

DesktopShell receives `vr-*` commands via TCP, spawns VRService as a child process, relays its stdout to the TCP client (wrapping each line with the passphrase), and captures stderr into its own log.

## VRService.exe — CLI Interface

Single-file self-contained console app. .NET 10, `net10.0-windows` target. Takes one command as CLI args, writes JSON lines to stdout, diagnostics to stderr. Exits when done.

```
VRService.exe vr-games
VRService.exe vr-launch <appId>
VRService.exe vr-devices
VRService.exe vr-status
VRService.exe vr-kill
```

### Output Channels

| Channel | Purpose | Format |
|---------|---------|--------|
| **stdout** | JSON responses for the phone app | One JSON object per line (same format VRLauncher already parses) |
| **stderr** | Diagnostic logging | Timestamped lines with DesktopShell prefix convention (`!!!`, `^^^`, `###`, `&&&`) |
| **Exit code** | 0 = ran to completion, 1 = fatal startup error | Integer |

### Command Behaviors

- **vr-games**: Single JSON line with `{games: [...], cDriveFreeSpaceBytes: N}`
- **vr-launch \<appId\>**: Streams multiple JSON lines as steps complete: `headset_check` -> `steamvr_start` -> `game_launch` -> `process_confirm` -> `{status: "complete"}`. Post-launch hooks fire after `game_launch` (e.g., BG3 DungeonMaster).
- **vr-devices**: Single JSON line with `{steamVrRunning, headset, baseStations, controllers}`
- **vr-status**: Stateless heuristic — checks if SteamVR and game processes are running rather than tracking a launch flag. Answers "what's actually happening" rather than "did I start something."
- **vr-kill**: Force-terminates VR-related processes (game + SteamVR). Single JSON line with `{status, killed: [...]}`

## DesktopShell Changes — TCPServer

The `vr-*` case in `HandleClientComm` replaces the inline `VrHandler` with process spawning:

### Relay Flow

1. **Existence check**: Verify `Bin\VRService.exe` exists. If missing: ToolTip notification + `{"status":"failed","error":"VR service not available"}` to client. Return.

2. **Spawn**: Start `VRService.exe {command}` with `UseShellExecute=false`, `RedirectStandardOutput=true`, `RedirectStandardError=true`. Wrap the entire spawn in try/catch — handle access denied, bad path, missing runtime, etc. All failures: ToolTip + error JSON to client.

3. **Stderr thread**: Read stderr line-by-line on a background thread, forward each line to `GlobalVar.Log($"[VRService] {line}")`.

4. **Stdout relay loop**: Read stdout line-by-line. For each line, wrap with passphrase and write to the TCP client stream. If the TCP client disconnects mid-stream, kill the VRService process and stop relaying.

5. **Outer timeout (90s)**: If VRService hasn't exited within 90 seconds, kill the process. If no stdout lines were relayed yet, send `{"status":"failed","error":"VR service timed out"}` to client.

6. **Post-exit**: Check exit code. If non-zero and no stdout lines were relayed, send `{"status":"failed","error":"VR service exited with error"}` to client.

### Error Injection Rule

**DesktopShell only injects its own error JSON if VRService produced zero stdout lines.** If progress lines were already relayed, DesktopShell does NOT inject conflicting terminal states — the phone app handles incomplete sequences.

### Removed from DesktopShell

- `using DesktopShell.VR;` import in TCPServer.cs
- Lazy `VrHandler` singleton and all VR dependency construction
- Entire `VR/` directory (4 files)
- VR test files from `DesktopShell.Tests/VR/`

## VRService Project Structure

Independent solution at `C:\Users\phuze\Dropbox\Programming\VRService\`:

```
VRService/
  VRService.sln
  VRService/
    VRService.csproj            net10.0-windows, console app, single-file publish
    Program.cs                  CLI entry point, arg parsing, stdout/stderr setup
    VRCommandHandler.cs         Moved from DesktopShell, writes to Console.Out
    VROrchestrator.cs           Moved as-is (IProcessManager, ProcessManager, VROrchestrator)
    VRGameListService.cs        Moved as-is (IFileReader, DiskFileReader, VRGameListService)
    SteamConfigParser.cs        Moved as-is
  VRService.Tests/
    VRService.Tests.csproj      MSTest + FluentAssertions + Moq
    SteamConfigParserTests.cs   Moved from DesktopShell.Tests
    VRGameListServiceTests.cs   Moved from DesktopShell.Tests
    VROrchestratorTests.cs      Moved from DesktopShell.Tests
```

## Key Adaptations After Move

### VRCommandHandler
- `WriteRemoteCommand(stream, json, passphrase)` -> `Console.Out.WriteLine(json)`
- `HandleAsync(command, stream)` -> `HandleAsync(command)` (no stream parameter)
- No passphrase handling — DesktopShell wraps on relay

### Logging
- `GlobalVar.Log(msg)` -> `Console.Error.WriteLine($"{DateTime.Now:HH:mm:ss.fff}:\t{msg}")`
- Same prefix convention so DesktopShell log remains consistent

### Post-Launch Hooks
- BG3 DungeonMaster hook (appId 1086940) moves as-is
- P/Invoke declarations (`SetForegroundWindow`, `SetWindowPos`) move with it

### vr-status
- No longer tracks `_launching` flag (can't persist between invocations)
- Checks running processes: is SteamVR running? Any VR game exe running?
- More robust — answers actual state rather than tracking intent

## Error Handling

| Scenario | Behavior |
|----------|----------|
| VRService.exe missing | ToolTip + error JSON to client |
| VRService fails to spawn (access denied, bad path, etc.) | ToolTip + error JSON to client |
| VRService crashes (exit code != 0), no stdout emitted | Error JSON to client |
| VRService crashes after partial stdout | No injection — phone app handles incomplete sequence |
| VRService hangs past 90s | Process killed. Error JSON only if no stdout yet |
| TCP client disconnects mid-stream | Kill VRService, stop relay, log disconnection |
| VRService emits malformed stdout | Relayed as-is — phone app handles parsing |
| VRService stderr output | Forwarded to DesktopShell.log with `[VRService]` prefix |

## Timeout Strategy

Two layers:
- **VRService internal**: `SteamVrStartTimeoutMs` (20s), `GameProcessConfirmTimeoutMs` (45s) — handles normal slow-start cases
- **DesktopShell outer**: 90s safety net — kills a hung VRService process

## Testing

- Existing 13 VR tests move to `VRService.Tests` with minimal changes (already use mocked `IProcessManager` and `IFileReader` interfaces)
- `DesktopShell.Tests/VR/` directory removed
- DesktopShell's relay logic is ~30 lines of process spawn + stream read — verified manually

## Deployment

Published single-file exe copied to `C:\Program Files\DesktopShell\Bin\VRService.exe` — same pattern as ToolTipper and other Bin/ tools.

```bash
cd C:\Users\phuze\Dropbox\Programming\VRService
dotnet publish VRService/VRService.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
# Copy to Bin/
```

## Future Improvements

- Make post-launch hooks configurable via a config file (appId -> script/exe mapping) instead of hardcoded in VRCommandHandler

## Codex Review Summary

- **Accepted**: Error delivery semantics clarified (inject only when zero stdout lines emitted); stream handling for client disconnect specified; process launch failure handling expanded beyond file-exists check
- **Rejected**: Explicit JSON schema/correlation IDs (unnecessary for single-user LAN tool); cleanup/rollback on timeout (same limitation as current inline code, vr-kill exists for this); library extraction alternative (user explicitly wants separate exe); version handshake (single developer, deployed together); BG3 hook YAGNI (deliberate decision, future work noted); integration tests (relay is trivial, unit tests cover VR logic)
- **Deferred**: vr-status heuristic edge cases (refine if real issues arise); stateful operations model (add PID file if needed); deployment sync documentation
