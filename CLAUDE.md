# DesktopShell

## CLAUDE.md Scope

When working in this repo, follow this `CLAUDE.md`. If you switch to a different repo in this workspace, use that repo's `CLAUDE.md` instead (and the root `CLAUDE.md` for workspace-wide rules, if applicable).

Windows Forms command launcher and desktop extension. Replaces desktop shortcuts/batch files, extends the Windows start menu. .NET 8.0 targeting `net8.0-windows10.0.26100.0`.

## Build / Run / Test

```bash
# Build
dotnet build DesktopShell.sln

# Run
dotnet run --project DesktopShell

# Publish single-file
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true

# Run all tests (87 tests, MSTest + FluentAssertions + Moq)
dotnet test DesktopShell.sln

# Run specific test class
dotnet test --filter "FullyQualifiedName~SettingsTests"

# Coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Linux CI harness (sandboxed, writes to /tmp)
./scripts/test-harness-linux.sh
./scripts/publish-win-x64-singlefile-harness-linux.sh
```

## Architecture

### Entry Point & Startup

`Program.Main()` → sets DPI awareness → singleton check → creates `Shell` form → `Application.Run()`.

Shell constructor: resets log → loads `settings.ini` via `Settings.ScanSettings()` → scans `hostlist.txt` → starts `TCPServer` (if enabled) → loads shortcuts/websites/browsers from text files → pulls queued messages async.

### Core Components

- **GlobalVar** (`GlobalVar.cs`, ~800 lines) — Static facade for all global state and utilities:
  - Settings I/O (`GetSetting`/`SetSetting` for INI file)
  - UI layout (`SetBounds`, `SetCentered`, `InitDropDownRects` for multi-monitor trigger areas)
  - Networking (`SendRemoteCommand`, TLS stream creation, host scanning)
  - Logging (`Log()` with prefixes: `!!!` commands, `^^^` info, `###` errors, `&&&` perf)
  - Color utilities (`ColorHandler` HSV/RGB conversions)

- **Shell** (`Forms/ShellForm.cs`) — Main form:
  - Dropdown animation (fade in/out with 15ms timer ticks)
  - Command processing: user types → Enter → `ProcessCommand()` matches against hardcoded regex → `shortcuts.txt` combos → `websites.txt` searches → browser launch
  - Hourly chime timer
  - Multi-screen support with per-screen trigger rectangles
  - DPI change handling via `WndProc` override

- **TCPServer** (`TCPServer.cs`) — Background TCP listener for remote command execution. TLS support with certificate pinning. Passphrase authentication.

- **MessageQueueClient** (`MessageQueueClient.cs`) — HTTPS queue fallback via Cloudflare Durable Objects. Cloudflare Access integration. Optional AES encryption. Pull/ack/enqueue pattern.

- **Settings** (`Settings.cs`) — Static config class. Regex-based INI parsing for colors (hex), booleans, paths, coordinates, multi-screen enable list.

- **ConfigForm** (`Forms/ConfigForm.cs`) — Settings UI: multi-screen checkboxes, color picker, hourly chime toggle.

- **ColorWheel** (`Forms/ColorWheel.cs`) — HSV color wheel picker control.

### Data Structures

```csharp
// Shortcut from shortcuts.txt (regex → executable + args)
internal record Combination(string? Keyword, List<string> FilePath, List<string> Arguments);

// Browser from webbrowsers.txt (regex → browser executable)
internal record WwwBrowser(string? keyword, string? filePath, bool defaultBrowser);

// Website group from websites.txt (keyword → URLs, optional search)
internal class WebCombo { string Keyword; List<string> WebsiteBase; bool? Searchable; }
```

### Command Flow

1. User types command → `CheckKeys()` on Enter
2. `ProcessCommand()` tries in order:
   - 7 hardcoded `[GeneratedRegex]` patterns (crosshair, password, rescan, shutdown, config, etc.)
   - `shortcuts.txt` combinations (user-defined regex → process launch)
   - `websites.txt` searches (keyword → URL open)
   - Browser fallback
3. Remote execution: `SendRemoteCommand()` over TCP (or queue fallback)

## Configuration Files

| File | Format | Purpose |
|------|--------|---------|
| `settings.ini` | `key=value` | Colors, directories, chime, screens, TCP toggle, position |
| `shortcuts.txt` | 3-line blocks: regex, exe path, args (or `-`) | User-defined command shortcuts |
| `websites.txt` | 3-line blocks: keyword, searchable bool, URL | Website/search mappings |
| `webbrowsers.txt` | 2-line blocks: regex, exe path | Browser mappings |
| `hostlist.txt` | `hostname:port` per line | TCP server discovery targets |

## Environment Variables

**Required:**
- `DESKTOPSHELL_PASSPHRASE` — TCP server auth passphrase

**TCP/TLS (optional):**
- `DESKTOPSHELL_TCP_TLS` — Set `1` to enable TLS
- `DESKTOPSHELL_TCP_TLS_PFX` — PFX certificate path
- `DESKTOPSHELL_TCP_TLS_PFX_PASSWORD` — PFX password
- `DESKTOPSHELL_TCP_TLS_THUMBPRINT` — Self-signed cert pinning

**Queue fallback (optional):**
- `DESKTOPSHELL_QUEUE_ENABLED` — Set `1` to enable HTTPS queue
- `DESKTOPSHELL_QUEUE_BASEURL` — Queue API URL (default: `https://queue.dlamanna.com`)
- `DESKTOPSHELL_QUEUE_KEY_B64` — Base64 AES key for message encryption
- `DESKTOPSHELL_QUEUE_SHARED_SECRET` — Queue auth token
- `DESKTOPSHELL_CF_ACCESS_CLIENT_ID` / `DESKTOPSHELL_CF_ACCESS_CLIENT_SECRET` — Cloudflare Access service token

**Network:**
- `DESKTOPSHELL_HOME_GATEWAY` — Home gateway IP (default: `10.0.0.1`)

## Dependencies

**NuGet (main app):** Microsoft.CSharp, Microsoft.Windows.Compatibility, System.Net.Sockets, Roslynator.Analyzers (build-time)

**NuGet (tests):** MSTest.TestFramework 3.1.1, MSTest.TestAdapter 3.1.1, Moq 4.20.70, FluentAssertions 6.12.0, coverlet.collector

**PInvoke:** user32.dll — `SetProcessDPIAware`, `SetWindowPos`, `SetForegroundWindow`

**Build flags:** `AllowUnsafeBlocks=true`, `Nullable=enable`, `UseWindowsForms=true`

## Test Suite (87 tests)

| File | Count | Coverage |
|------|-------|----------|
| BasicTests.cs | 19 | SplitWords parsing, IsInField collision detection |
| GlobalVarTests.cs | 22 | Logging, constants, colors, rectangles, animation |
| SettingsTests.cs | 46 | Color/boolean/path/coordinate/screen parsing |
| QueueConfigurationTests.cs | 3+ | Queue + Cloudflare Access config validation |

## Key Patterns

- **`[GeneratedRegex]`** attributes for compile-time regex (C# source generators)
- **Singleton form** instances stored in `GlobalVar` (ShellInstance, ServerInstance, ConfigInstance)
- **Animation system:** FadeDirection (+1/-1/0), 15ms tick interval, 21-unit opacity steps
- **Multi-monitor:** `Screen.AllScreens` → per-screen trigger rectangles with 50px H / 20px V padding
- **Network resilience:** TCP with 300ms retry delay, 250ms read timeout, 1500ms idle timeout, 4KB buffer. HTTPS queue as store-and-forward fallback.
- **Logging:** `DesktopShell.log` in working directory, reset on startup

## Additional Docs

- `ENVIRONMENT_SETUP.md` — Full env var setup guide (Windows, CI/CD, TLS, Cloudflare)
- `TESTING_GUIDE.md` — Test patterns, coverage goals, CI workflow
- `TEST_COVERAGE_SUMMARY.md` — 87 test breakdown, execution stats
