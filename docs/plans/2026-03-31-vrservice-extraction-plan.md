# VRService Extraction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract DesktopShell's inline VR module (4 files) into a standalone `VRService.exe` console app, replacing inline VR handling with process spawning and stdout/stderr relay.

**Architecture:** DesktopShell receives `vr-*` TCP commands, spawns `Bin\VRService.exe` as a child process with the command as CLI args, relays its stdout (JSON lines) to the TCP client wrapped with the passphrase, and captures its stderr into the DesktopShell log. VRService is a single-file self-contained console app that writes JSON to stdout and diagnostics to stderr.

**Tech Stack:** .NET 10 (`net10.0-windows`), C#, MSTest + FluentAssertions + Moq, System.Text.Json, P/Invoke (user32.dll)

---

## File Structure

### New Project: `C:\Users\phuze\Dropbox\Programming\VRService\`

| File | Responsibility |
|------|---------------|
| `VRService.sln` | Solution file |
| `VRService/VRService.csproj` | Console app, net10.0-windows, single-file publish |
| `VRService/Program.cs` | CLI entry point — arg parsing, stdout/stderr setup, exit codes |
| `VRService/Log.cs` | Static stderr logging helper (replaces GlobalVar.Log) |
| `VRService/VRCommandHandler.cs` | Command router — writes JSON to Console.Out instead of TCP stream |
| `VRService/VROrchestrator.cs` | Launch pipeline, device status, kill session (moved from DesktopShell) |
| `VRService/VRGameListService.cs` | Steam game discovery (moved from DesktopShell) |
| `VRService/SteamConfigParser.cs` | VDF/JSON parsing (moved as-is, no GlobalVar deps) |
| `VRService.Tests/VRService.Tests.csproj` | Test project — MSTest + FluentAssertions + Moq |
| `VRService.Tests/SteamConfigParserTests.cs` | Moved from DesktopShell.Tests |
| `VRService.Tests/VRGameListServiceTests.cs` | Moved from DesktopShell.Tests |
| `VRService.Tests/VROrchestratorTests.cs` | Moved from DesktopShell.Tests |
| `VRService.Tests/VRCommandHandlerTests.cs` | New — tests stdout JSON output |

### Modified in DesktopShell

| File | Change |
|------|--------|
| `DesktopShell/TCPServer.cs` | Replace inline VrHandler with VRService process spawn + relay |

### Removed from DesktopShell

| File | Reason |
|------|--------|
| `DesktopShell/VR/VRCommandHandler.cs` | Moved to VRService |
| `DesktopShell/VR/VROrchestrator.cs` | Moved to VRService |
| `DesktopShell/VR/VRGameListService.cs` | Moved to VRService |
| `DesktopShell/VR/SteamConfigParser.cs` | Moved to VRService |
| `DesktopShell.Tests/VR/SteamConfigParserTests.cs` | Moved to VRService.Tests |
| `DesktopShell.Tests/VR/VRGameListServiceTests.cs` | Moved to VRService.Tests |
| `DesktopShell.Tests/VR/VROrchestratorTests.cs` | Moved to VRService.Tests |

---

## Task 1: Create VRService Project Scaffold

**Files:**
- Create: `C:\Users\phuze\Dropbox\Programming\VRService\VRService\VRService.csproj`
- Create: `C:\Users\phuze\Dropbox\Programming\VRService\VRService\Program.cs`
- Create: `C:\Users\phuze\Dropbox\Programming\VRService\VRService.sln`

- [ ] **Step 1: Create the project directory and solution**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming
mkdir -p VRService
cd VRService
dotnet new console -n VRService --framework net10.0-windows -o VRService
dotnet new sln -n VRService
dotnet sln add VRService/VRService.csproj
```

- [ ] **Step 2: Configure VRService.csproj for single-file Windows console app**

Replace the generated `VRService/VRService.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Write the minimal Program.cs stub**

Replace the generated `VRService/Program.cs` with:

```csharp
namespace VRService;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: VRService.exe <command> [args]");
            Console.Error.WriteLine("Commands: vr-games, vr-launch <appId>, vr-devices, vr-status, vr-kill");
            return 1;
        }

        // Reconstruct the full command string (e.g. "vr-launch 546560")
        string command = string.Join(' ', args);

        Console.Error.WriteLine($"{DateTime.Now:HH:mm:ss.fff}:\t^^^ VRService started: {command}");

        // TODO: Wire up VRCommandHandler in Task 5
        Console.Error.WriteLine($"{DateTime.Now:HH:mm:ss.fff}:\t### VRService: not yet implemented");
        return 1;
    }
}
```

- [ ] **Step 4: Verify the project builds**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
dotnet build VRService.sln -p:EnableWindowsTargeting=true
```

Expected: Build succeeded. 0 Error(s).

- [ ] **Step 5: Commit**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
git init
git add VRService.sln VRService/VRService.csproj VRService/Program.cs
git commit -m "feat: scaffold VRService console app project"
```

---

## Task 2: Create Test Project Scaffold

**Files:**
- Create: `C:\Users\phuze\Dropbox\Programming\VRService\VRService.Tests\VRService.Tests.csproj`

- [ ] **Step 1: Create the test project**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
dotnet new mstest -n VRService.Tests -o VRService.Tests
dotnet sln add VRService.Tests/VRService.Tests.csproj
dotnet add VRService.Tests/VRService.Tests.csproj reference VRService/VRService.csproj
```

- [ ] **Step 2: Configure VRService.Tests.csproj**

Replace `VRService.Tests/VRService.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageReference Include="MSTest.TestAdapter" Version="3.8.3" />
    <PackageReference Include="MSTest.TestFramework" Version="3.8.3" />
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="FluentAssertions" Version="7.2.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\VRService\VRService.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Remove the auto-generated test file**

```bash
rm /mnt/c/Users/phuze/Dropbox/Programming/VRService/VRService.Tests/Test1.cs 2>/dev/null || true
rm /mnt/c/Users/phuze/Dropbox/Programming/VRService/VRService.Tests/UnitTest1.cs 2>/dev/null || true
```

- [ ] **Step 4: Verify both projects build**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
dotnet build VRService.sln -p:EnableWindowsTargeting=true
```

Expected: Build succeeded. 0 Error(s).

- [ ] **Step 5: Commit**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
git add VRService.sln VRService.Tests/VRService.Tests.csproj
git commit -m "feat: add VRService.Tests project with MSTest + FluentAssertions + Moq"
```

---

## Task 3: Move SteamConfigParser (No Dependencies)

**Files:**
- Create: `VRService/SteamConfigParser.cs`
- Create: `VRService.Tests/SteamConfigParserTests.cs`

This file has zero `GlobalVar` dependencies — it moves as-is with only a namespace change.

- [ ] **Step 1: Create SteamConfigParser.cs**

Copy `DesktopShell/VR/SteamConfigParser.cs` to `VRService/SteamConfigParser.cs`. Change the namespace from `DesktopShell.VR` to `VRService`:

```csharp
using System.Text.Json;
using System.Text.RegularExpressions;

namespace VRService;

public record SteamLibrary(string Path, List<int> AppIds);
public record SteamAppInfo(int AppId, string Name, string InstallDir);
public record VrCollection(List<int> Added, List<int> Removed);

public static partial class SteamConfigParser
{
    // VDF is a simple nested key-value format: "key" "value" or "key" { ... }
    // We only need shallow parsing for our use cases.

    public static List<SteamLibrary> ParseLibraryFolders(string vdf)
    {
        var libraries = new List<SteamLibrary>();
        var blocks = ExtractTopLevelBlocks(vdf);

        foreach (string block in blocks)
        {
            string? path = ExtractValue(block, "path");
            if (path == null) continue;

            var appIds = new List<int>();
            string? appsBlock = ExtractBlock(block, "apps");
            if (appsBlock != null)
            {
                foreach (Match m in KeyValuePairRegex().Matches(appsBlock))
                {
                    if (int.TryParse(m.Groups[1].Value, out int appId))
                        appIds.Add(appId);
                }
            }

            libraries.Add(new SteamLibrary(path.Replace(@"\\", @"\"), appIds));
        }

        return libraries;
    }

    public static SteamAppInfo? ParseAppManifest(string acf)
    {
        string? appIdStr = ExtractValue(acf, "appid");
        string? name = ExtractValue(acf, "name");
        string? installDir = ExtractValue(acf, "installdir");

        if (appIdStr == null || name == null || !int.TryParse(appIdStr, out int appId))
            return null;

        return new SteamAppInfo(appId, name, installDir ?? "");
    }

    public static List<int> ParseVrManifest(string json)
    {
        var appIds = new List<int>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("applications", out var apps))
                return appIds;

            foreach (var app in apps.EnumerateArray())
            {
                if (!app.TryGetProperty("app_key", out var keyEl))
                    continue;

                string? appKey = keyEl.GetString();
                if (appKey != null && appKey.StartsWith("steam.app.") &&
                    int.TryParse(appKey["steam.app.".Length..], out int appId))
                {
                    appIds.Add(appId);
                }
            }
        }
        catch (JsonException)
        {
            // Corrupt manifest — return empty
        }

        return appIds;
    }

    public static VrCollection? ParseVrCollection(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                if (entry.GetArrayLength() < 2) continue;

                var valueObj = entry[1];
                if (valueObj.ValueKind != JsonValueKind.Object) continue;
                if (!valueObj.TryGetProperty("value", out var valueStr))
                    continue;

                string? innerJson = valueStr.GetString();
                if (innerJson == null) continue;

                using var inner = JsonDocument.Parse(innerJson);
                if (inner.RootElement.ValueKind != JsonValueKind.Object) continue;
                if (!inner.RootElement.TryGetProperty("name", out var nameEl))
                    continue;

                if (!string.Equals(nameEl.GetString(), "VR", StringComparison.OrdinalIgnoreCase))
                    continue;

                var added = new List<int>();
                var removed = new List<int>();

                if (inner.RootElement.TryGetProperty("added", out var addedEl))
                    foreach (var a in addedEl.EnumerateArray())
                        if (a.TryGetInt32(out int id)) added.Add(id);

                if (inner.RootElement.TryGetProperty("removed", out var removedEl))
                    foreach (var r in removedEl.EnumerateArray())
                        if (r.TryGetInt32(out int id)) removed.Add(id);

                return new VrCollection(added, removed);
            }
        }
        catch (Exception)
        {
            // Corrupt or unexpected config — return null
        }

        return null;
    }

    private static string? ExtractValue(string vdf, string key)
    {
        var match = Regex.Match(vdf, $"\"{Regex.Escape(key)}\"\\s+\"([^\"]+)\"");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractBlock(string vdf, string key)
    {
        int idx = vdf.IndexOf($"\"{key}\"", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        int braceStart = vdf.IndexOf('{', idx);
        if (braceStart < 0) return null;

        int depth = 1;
        int i = braceStart + 1;
        while (i < vdf.Length && depth > 0)
        {
            if (vdf[i] == '{') depth++;
            else if (vdf[i] == '}') depth--;
            i++;
        }

        return vdf[(braceStart + 1)..(i - 1)];
    }

    private static List<string> ExtractTopLevelBlocks(string vdf)
    {
        var blocks = new List<string>();
        // Find the outermost block first
        string? outer = ExtractBlock(vdf, vdf.TrimStart().Split('"')[1]);
        if (outer == null) return blocks;

        // Then find numbered sub-blocks: "0" { ... }, "1" { ... }
        int pos = 0;
        while (pos < outer.Length)
        {
            int braceStart = outer.IndexOf('{', pos);
            if (braceStart < 0) break;

            int depth = 1;
            int i = braceStart + 1;
            while (i < outer.Length && depth > 0)
            {
                if (outer[i] == '{') depth++;
                else if (outer[i] == '}') depth--;
                i++;
            }

            blocks.Add(outer[braceStart..i]);
            pos = i;
        }

        return blocks;
    }

    [GeneratedRegex("\"(\\d+)\"\\s+\"([^\"]+)\"")]
    private static partial Regex KeyValuePairRegex();
}
```

- [ ] **Step 2: Create SteamConfigParserTests.cs**

Copy `DesktopShell.Tests/VR/SteamConfigParserTests.cs` to `VRService.Tests/SteamConfigParserTests.cs`. Change namespace and using:

```csharp
using VRService;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VRService.Tests;

[TestClass]
public class SteamConfigParserTests
{
    [TestMethod]
    public void ParseVdf_LibraryFolders_ReturnsLibraryPaths()
    {
        string vdf = """
            "libraryfolders"
            {
            	"0"
            	{
            		"path"		"C:\\Program Files (x86)\\Steam"
            		"apps"
            		{
            			"570"		"71891128118"
            			"250820"		"5955281920"
            		}
            	}
            	"1"
            	{
            		"path"		"D:\\SteamLibrary"
            		"apps"
            		{
            			"546560"		"67132813312"
            		}
            	}
            }
            """;

        var result = SteamConfigParser.ParseLibraryFolders(vdf);

        result.Should().HaveCount(2);
        result[0].Path.Should().Be(@"C:\Program Files (x86)\Steam");
        result[0].AppIds.Should().Contain(570);
        result[1].Path.Should().Be(@"D:\SteamLibrary");
        result[1].AppIds.Should().Contain(546560);
    }

    [TestMethod]
    public void ParseVdf_AppManifest_ReturnsGameInfo()
    {
        string acf = """
            "AppState"
            {
            	"appid"		"546560"
            	"name"		"Half-Life: Alyx"
            	"installdir"		"Half-Life Alyx"
            }
            """;

        var result = SteamConfigParser.ParseAppManifest(acf);

        result.Should().NotBeNull();
        result!.AppId.Should().Be(546560);
        result.Name.Should().Be("Half-Life: Alyx");
        result.InstallDir.Should().Be("Half-Life Alyx");
    }

    [TestMethod]
    public void ParseVrManifest_ReturnsVrAppIds()
    {
        string json = """
            {
              "applications": [
                {
                  "app_key": "steam.app.546560",
                  "launch_type": "url",
                  "url": "steam://launch/546560/VR",
                  "strings": { "en_us": { "name": "Half-Life: Alyx" } }
                },
                {
                  "app_key": "steam.app.348250",
                  "launch_type": "url",
                  "url": "steam://launch/348250/VR",
                  "strings": { "en_us": { "name": "Google Earth VR" } }
                }
              ]
            }
            """;

        var result = SteamConfigParser.ParseVrManifest(json);

        result.Should().HaveCount(2);
        result.Should().Contain(546560);
        result.Should().Contain(348250);
    }

    [TestMethod]
    public void ParseCloudStorage_FindsVrCollection()
    {
        string json = """
            [
              ["user-collections.uc-abc123", {
                "key": "user-collections.uc-abc123",
                "timestamp": 1773309669,
                "value": "{\"id\":\"uc-abc123\",\"name\":\"VR\",\"added\":[493490,2580190,1086940],\"removed\":[238280,850450]}"
              }],
              ["user-collections.uc-xyz789", {
                "key": "user-collections.uc-xyz789",
                "timestamp": 1773309000,
                "value": "{\"id\":\"uc-xyz789\",\"name\":\"FPS\",\"added\":[570],\"removed\":[]}"
              }]
            ]
            """;

        var result = SteamConfigParser.ParseVrCollection(json);

        result.Should().NotBeNull();
        result!.Added.Should().BeEquivalentTo([493490, 2580190, 1086940]);
        result.Removed.Should().BeEquivalentTo([238280, 850450]);
    }

    [TestMethod]
    public void ParseCloudStorage_NoVrCollection_ReturnsNull()
    {
        string json = """
            [
              ["user-collections.uc-xyz789", {
                "key": "user-collections.uc-xyz789",
                "timestamp": 1773309000,
                "value": "{\"id\":\"uc-xyz789\",\"name\":\"FPS\",\"added\":[570],\"removed\":[]}"
              }]
            ]
            """;

        var result = SteamConfigParser.ParseVrCollection(json);

        result.Should().BeNull();
    }
}
```

- [ ] **Step 3: Verify tests pass**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
dotnet test VRService.sln -p:EnableWindowsTargeting=true --verbosity normal
```

Expected: 5 tests passed.

- [ ] **Step 4: Commit**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
git add VRService/SteamConfigParser.cs VRService.Tests/SteamConfigParserTests.cs
git commit -m "feat: move SteamConfigParser from DesktopShell (no changes needed)"
```

---

## Task 4: Create Log Helper and Move VRGameListService

**Files:**
- Create: `VRService/Log.cs`
- Create: `VRService/VRGameListService.cs`
- Create: `VRService.Tests/VRGameListServiceTests.cs`

VRGameListService has 2 `GlobalVar.Log` calls that need replacing with the new `Log` helper.

- [ ] **Step 1: Create Log.cs**

```csharp
namespace VRService;

internal static class Log
{
    internal static void Info(string message)
        => Console.Error.WriteLine($"{DateTime.Now:HH:mm:ss.fff}:\t^^^ {message}");

    internal static void Error(string message)
        => Console.Error.WriteLine($"{DateTime.Now:HH:mm:ss.fff}:\t### {message}");

    internal static void Command(string message)
        => Console.Error.WriteLine($"{DateTime.Now:HH:mm:ss.fff}:\t$$$ {message}");

    internal static void Perf(string message)
        => Console.Error.WriteLine($"{DateTime.Now:HH:mm:ss.fff}:\t&&& {message}");
}
```

- [ ] **Step 2: Create VRGameListService.cs**

Copy from DesktopShell, change namespace to `VRService`, replace `GlobalVar.Log(...)` with `Log.Error(...)`:

```csharp
namespace VRService;

public record VrGame(
    int AppId,
    string Name,
    string InstallDir,
    string? InstallPath = null,
    string? InstallDrive = null,
    long InstallSizeBytes = 0
);

public interface IFileReader
{
    string? ReadFile(string path);
    string[] GetFiles(string directory, string pattern);
    bool FileExists(string path);
    string[] GetDirectories(string path);
}

public class DiskFileReader : IFileReader
{
    public string? ReadFile(string path)
    {
        try { return File.ReadAllText(path); }
        catch { return null; }
    }

    public string[] GetFiles(string directory, string pattern)
    {
        try { return Directory.GetFiles(directory, pattern); }
        catch { return []; }
    }

    public bool FileExists(string path) => File.Exists(path);

    public string[] GetDirectories(string path)
    {
        try { return Directory.GetDirectories(path); }
        catch { return []; }
    }
}

public class VRGameListService
{
    private readonly IFileReader _reader;

    private const string DefaultSteamPath = @"C:\Program Files (x86)\Steam";
    private const string LibraryFoldersFile = @"steamapps\libraryfolders.vdf";
    private const string VrManifestFile = @"config\steamapps.vrmanifest";
    private const string UserDataPath = @"userdata";
    private const string CloudStorageRelPath = @"config\cloudstorage\cloud-storage-namespace-1.json";

    public VRGameListService(IFileReader reader)
    {
        _reader = reader;
    }

    public List<VrGame> GetVrGames()
    {
        string libraryVdf = _reader.ReadFile(Path.Combine(DefaultSteamPath, LibraryFoldersFile)) ?? "";
        var libraries = SteamConfigParser.ParseLibraryFolders(libraryVdf);
        if (libraries.Count == 0)
        {
            Log.Error("VRGameListService: No Steam libraries found");
            return [];
        }

        var installedApps = new HashSet<int>();
        foreach (var lib in libraries)
            foreach (int appId in lib.AppIds)
                installedApps.Add(appId);

        string vrManifestJson = _reader.ReadFile(Path.Combine(DefaultSteamPath, VrManifestFile)) ?? "";
        var vrManifestAppIds = SteamConfigParser.ParseVrManifest(vrManifestJson);

        var vrCollection = FindVrCollection();
        var addedIds = vrCollection?.Added ?? [];
        var removedIds = new HashSet<int>(vrCollection?.Removed ?? []);

        var candidateIds = new HashSet<int>(vrManifestAppIds);
        foreach (int id in addedIds)
            candidateIds.Add(id);
        candidateIds.ExceptWith(removedIds);
        candidateIds.IntersectWith(installedApps);

        var games = new List<VrGame>();
        foreach (int appId in candidateIds)
        {
            var appInfo = FindAppInfo(appId, libraries);
            if (appInfo == null) continue;

            string? fullPath = ResolveInstallPath(appInfo.InstallDir, libraries);
            string? drive = fullPath != null && fullPath.Length >= 2 ? fullPath[..2] : null;
            long sizeBytes = fullPath != null ? GetDirectorySizeBytes(fullPath) : 0;

            games.Add(new VrGame(appInfo.AppId, appInfo.Name, appInfo.InstallDir, fullPath, drive, sizeBytes));
        }

        games.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return games;
    }

    public static long GetCDriveFreeSpace()
    {
        try
        {
            var drive = new DriveInfo("C");
            return drive.AvailableFreeSpace;
        }
        catch { return 0; }
    }

    private VrCollection? FindVrCollection()
    {
        string userDataDir = Path.Combine(DefaultSteamPath, UserDataPath);
        string[] userDirs = _reader.GetDirectories(userDataDir);

        foreach (string userDir in userDirs)
        {
            string cloudFile = Path.Combine(userDir, CloudStorageRelPath);
            string? json = _reader.ReadFile(cloudFile);
            if (json == null) continue;

            var collection = SteamConfigParser.ParseVrCollection(json);
            if (collection != null)
                return collection;
        }

        return null;
    }

    private static string? ResolveInstallPath(string installDir, List<SteamLibrary> libraries)
    {
        if (string.IsNullOrEmpty(installDir)) return null;

        foreach (var lib in libraries)
        {
            string fullPath = Path.Combine(lib.Path, "steamapps", "common", installDir);
            if (Directory.Exists(fullPath))
                return fullPath;
        }

        return null;
    }

    private static long GetDirectorySizeBytes(string path)
    {
        try
        {
            if (!Directory.Exists(path)) return 0;
            return new DirectoryInfo(path)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(f => f.Length);
        }
        catch { return 0; }
    }

    private SteamAppInfo? FindAppInfo(int appId, List<SteamLibrary> libraries)
    {
        foreach (var lib in libraries)
        {
            string acfPath = Path.Combine(lib.Path, "steamapps", $"appmanifest_{appId}.acf");
            string? content = _reader.ReadFile(acfPath);
            if (content != null)
            {
                var info = SteamConfigParser.ParseAppManifest(content);
                if (info != null) return info;
            }
        }

        return null;
    }
}
```

- [ ] **Step 3: Create VRGameListServiceTests.cs**

Copy from DesktopShell.Tests, change namespaces:

```csharp
using VRService;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VRService.Tests;

[TestClass]
public class VRGameListServiceTests
{
    [TestMethod]
    public void GetVrGames_CombinesManifestAndCategoryOverrides()
    {
        var reader = new FakeFileReader(new Dictionary<string, string>
        {
            ["libraryfolders.vdf"] = """
                "libraryfolders"
                {
                	"0"
                	{
                		"path"		"C:\\Steam"
                		"apps"
                		{
                			"546560"	"1"
                			"348250"	"1"
                			"1086940"	"1"
                		}
                	}
                }
                """,
            ["vrmanifest"] = """
                {
                  "applications": [
                    { "app_key": "steam.app.546560", "launch_type": "url", "url": "steam://launch/546560/VR", "strings": { "en_us": { "name": "Half-Life: Alyx" } } },
                    { "app_key": "steam.app.348250", "launch_type": "url", "url": "steam://launch/348250/VR", "strings": { "en_us": { "name": "Google Earth VR" } } }
                  ]
                }
                """,
            ["cloudstorage"] = """
                [
                  ["user-collections.uc-abc", {
                    "key": "user-collections.uc-abc",
                    "timestamp": 1,
                    "value": "{\"id\":\"uc-abc\",\"name\":\"VR\",\"added\":[1086940,999],\"removed\":[348250]}"
                  }]
                ]
                """,
            [@"C:\Steam\steamapps\appmanifest_546560.acf"] = """
                "AppState"
                {
                	"appid"		"546560"
                	"name"		"Half-Life: Alyx"
                	"installdir"		"Half-Life Alyx"
                }
                """,
            [@"C:\Steam\steamapps\appmanifest_348250.acf"] = """
                "AppState"
                {
                	"appid"		"348250"
                	"name"		"Google Earth VR"
                	"installdir"		"EarthVR"
                }
                """,
            [@"C:\Steam\steamapps\appmanifest_1086940.acf"] = """
                "AppState"
                {
                	"appid"		"1086940"
                	"name"		"Baldur's Gate 3"
                	"installdir"		"Baldurs Gate 3"
                }
                """,
        });

        var service = new VRGameListService(reader);
        var games = service.GetVrGames();

        games.Should().HaveCount(2);
        games.Should().Contain(g => g.AppId == 546560 && g.Name == "Half-Life: Alyx");
        games.Should().Contain(g => g.AppId == 1086940 && g.Name == "Baldur's Gate 3");
        games.Should().NotContain(g => g.AppId == 348250);
        games.Should().NotContain(g => g.AppId == 999);
    }

    [TestMethod]
    public void GetVrGames_MissingCloudStorage_StillReturnsManifestGames()
    {
        var reader = new FakeFileReader(new Dictionary<string, string>
        {
            ["libraryfolders.vdf"] = """
                "libraryfolders"
                {
                	"0"
                	{
                		"path"		"C:\\Steam"
                		"apps" { "546560" "1" }
                	}
                }
                """,
            ["vrmanifest"] = """
                { "applications": [{ "app_key": "steam.app.546560", "launch_type": "url", "url": "steam://launch/546560/VR", "strings": { "en_us": { "name": "HLA" } } }] }
                """,
            [@"C:\Steam\steamapps\appmanifest_546560.acf"] = """
                "AppState" { "appid" "546560" "name" "Half-Life: Alyx" "installdir" "Half-Life Alyx" }
                """,
        });

        var service = new VRGameListService(reader);
        var games = service.GetVrGames();

        games.Should().HaveCount(1);
        games[0].AppId.Should().Be(546560);
    }
}

public class FakeFileReader : IFileReader
{
    private readonly Dictionary<string, string> _files;

    public FakeFileReader(Dictionary<string, string> files)
    {
        _files = files;
    }

    public string? ReadFile(string path)
    {
        if (_files.TryGetValue(path, out string? content))
            return content;

        foreach (var kvp in _files)
        {
            if (path.EndsWith(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                path.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return null;
    }

    public string[] GetFiles(string directory, string pattern)
    {
        return _files.Keys
            .Where(k => k.Contains("appmanifest_"))
            .ToArray();
    }

    public bool FileExists(string path) =>
        _files.Keys.Any(k => path.Contains(k, StringComparison.OrdinalIgnoreCase));

    public string[] GetDirectories(string path) =>
        _files.ContainsKey("cloudstorage") ? [Path.Combine(path, "12345")] : [];
}
```

- [ ] **Step 4: Verify tests pass**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
dotnet test VRService.sln -p:EnableWindowsTargeting=true --verbosity normal
```

Expected: 7 tests passed (5 SteamConfigParser + 2 VRGameListService).

- [ ] **Step 5: Commit**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
git add VRService/Log.cs VRService/VRGameListService.cs VRService.Tests/VRGameListServiceTests.cs
git commit -m "feat: move VRGameListService from DesktopShell, replace GlobalVar.Log with stderr"
```

---

## Task 5: Move VROrchestrator

**Files:**
- Create: `VRService/VROrchestrator.cs`
- Create: `VRService.Tests/VROrchestratorTests.cs`

VROrchestrator has ~12 `GlobalVar.Log` calls that need replacing with `Log.*` calls. The `_launching` flag and `GetStatus()` change to heuristic process checking (stateless — no persistent state between invocations).

- [ ] **Step 1: Create VROrchestrator.cs**

Copy from DesktopShell, change namespace to `VRService`, replace all `GlobalVar.Log(...)` calls. Key changes:
- `GlobalVar.Log($"### ...")` → `Log.Error(...)`
- `GlobalVar.Log($"$$$ ...")` → `Log.Command(...)`
- `GetStatus()` becomes heuristic — checks running processes instead of `_launching` flag

```csharp
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VRService;

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

public class VrKillResult
{
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("killed")]
    public required List<string> Killed { get; init; }

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
                Log.Error("VR: vrcmd.exe timed out checking HMD presence");
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
            Log.Error($"VR: Failed to launch SteamVR: {e.Message}");
        }
    }

    public async Task LaunchSteamGameAsync(int appId)
    {
        try
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo($"steam://launch/{appId}/VR") { UseShellExecute = true });
            await Task.Delay(1000);
        }
        catch (Exception e)
        {
            Log.Error($"VR: Failed to launch game {appId}: {e.Message}");
        }
    }

    public async Task<string?> WaitForGameProcessAsync(int appId, string installDir, int timeoutMs, CancellationToken ct)
    {
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
                        Log.Command($"VR kill: terminated {proc.ProcessName}.exe (PID {proc.Id})");
                    }
                    catch (Exception e)
                    {
                        Log.Error($"VR kill: failed to terminate {name}: {e.Message}");
                    }
                    finally
                    {
                        proc.Dispose();
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
    private volatile string? _lastGameProcess;
    public string? LastGameProcess => _lastGameProcess;

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
        if (Interlocked.CompareExchange(ref _launching, 1, 0) != 0)
        {
            yield return new VrLaunchStep { Status = "already_launching", AppId = appId };
            yield break;
        }

        try
        {
            bool hmdPresent = await _process.IsHmdPresentAsync();
            if (!hmdPresent)
            {
                yield return new VrLaunchStep { Step = "headset_check", Status = "failed", Error = "Headset not detected" };
                yield break;
            }
            yield return new VrLaunchStep { Step = "headset_check", Status = "ok" };

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

            await _process.LaunchSteamGameAsync(appId);
            yield return new VrLaunchStep { Step = "game_launch", Status = "ok" };

            string? processName = await _process.WaitForGameProcessAsync(appId, installDir, GameProcessConfirmTimeoutMs, ct);
            if (processName != null)
            {
                _lastGameProcess = Path.GetFileNameWithoutExtension(processName);
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

    /// <summary>
    /// Stateless heuristic status check — answers "what's actually running" rather than tracking intent.
    /// </summary>
    public VrLaunchStep GetStatus()
    {
        bool steamVrRunning = _process.IsProcessRunning("vrserver");

        return new VrLaunchStep
        {
            Status = "ok",
            Step = "status",
            Message = steamVrRunning ? "SteamVR is running" : "SteamVR is not running",
            Process = steamVrRunning ? "vrserver" : null
        };
    }

    private static readonly string[] SteamVrProcesses = ["vrserver", "vrcompositor", "vrmonitor"];

    public VrKillResult KillVrSession(IEnumerable<string> gameInstallDirs)
    {
        var processesToKill = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (_lastGameProcess != null)
            processesToKill.Add(_lastGameProcess);

        foreach (var dir in gameInstallDirs)
        {
            foreach (var name in _process.FindCandidateProcessNames(dir))
            {
                if (_process.IsProcessRunning(name))
                    processesToKill.Add(name);
            }
        }

        var killed = _process.ForceKillByName(processesToKill);
        killed.AddRange(_process.ForceKillByName(SteamVrProcesses));

        Interlocked.Exchange(ref _launching, 0);
        _lastGameProcess = null;

        return new VrKillResult
        {
            Status = "ok",
            Killed = killed.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
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
            Log.Error($"VR: Failed to read steamvr.vrsettings: {e.Message}");
        }

        try
        {
            if (File.Exists(LighthouseDbPath))
            {
                var db = JsonDocument.Parse(File.ReadAllText(LighthouseDbPath));

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
            Log.Error($"VR: Failed to read lighthousedb.json: {e.Message}");
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
```

- [ ] **Step 2: Create VROrchestratorTests.cs**

Copy from DesktopShell.Tests, change namespaces. The `DuplicateLaunch` and `ResetsLaunchingFlag` tests still work within a single process invocation (they test the in-memory `_launching` guard during `vr-launch`):

```csharp
using VRService;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VRService.Tests;

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
            launchDelayMs: 5000
        );

        var orchestrator = new VROrchestrator(process);

        var firstLaunch = Task.Run(async () =>
        {
            await foreach (var _ in orchestrator.LaunchAsync(546560)) { }
        });

        await Task.Delay(50);

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

        var launchTask = Task.Run(async () =>
        {
            await foreach (var _ in orchestrator.LaunchAsync(546560)) { }
        });
        await Task.Delay(50);

        orchestrator.KillVrSession([]);

        var status = orchestrator.GetStatus();
        status.Process.Should().BeNull();

        await Task.Delay(100);
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
```

- [ ] **Step 3: Verify all tests pass**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
dotnet test VRService.sln -p:EnableWindowsTargeting=true --verbosity normal
```

Expected: 14 tests passed (5 SteamConfigParser + 2 VRGameListService + 7 VROrchestrator).

- [ ] **Step 4: Commit**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
git add VRService/VROrchestrator.cs VRService.Tests/VROrchestratorTests.cs
git commit -m "feat: move VROrchestrator from DesktopShell, replace GlobalVar.Log with stderr"
```

---

## Task 6: Move and Adapt VRCommandHandler

**Files:**
- Create: `VRService/VRCommandHandler.cs`
- Create: `VRService.Tests/VRCommandHandlerTests.cs`

This is the most significant adaptation. Key changes:
- `HandleAsync(string command, Stream clientStream)` → `HandleAsync(string command)` (no stream)
- `GlobalVar.WriteRemoteCommand(stream, json, includePassPhrase: true)` → `Console.Out.WriteLine(json)`
- `WriteJsonLine(stream, step)` → `Console.Out.WriteLine(step.ToJson())`
- `GlobalVar.Log(...)` → `Log.*(...)`
- P/Invoke `SetForegroundWindow` and BG3 hook move as-is

- [ ] **Step 1: Write VRCommandHandlerTests.cs (test first)**

We test that the handler writes correct JSON to stdout by capturing Console.Out:

```csharp
using System.Text.Json;
using VRService;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VRService.Tests;

[TestClass]
public class VRCommandHandlerTests
{
    [TestMethod]
    public async Task HandleAsync_UnknownCommand_WritesFailedJson()
    {
        var handler = CreateHandler();
        var output = await CaptureStdout(() => handler.HandleAsync("vr-bogus"));

        output.Should().HaveCount(1);
        var json = JsonDocument.Parse(output[0]);
        json.RootElement.GetProperty("status").GetString().Should().Be("failed");
        json.RootElement.GetProperty("error").GetString().Should().Contain("Unknown VR command");
    }

    [TestMethod]
    public async Task HandleAsync_VrGames_WritesGameListJson()
    {
        var reader = new FakeFileReader(new Dictionary<string, string>
        {
            ["libraryfolders.vdf"] = """
                "libraryfolders"
                {
                	"0"
                	{
                		"path"		"C:\\Steam"
                		"apps" { "546560" "1" }
                	}
                }
                """,
            ["vrmanifest"] = """
                { "applications": [{ "app_key": "steam.app.546560", "launch_type": "url", "url": "steam://launch/546560/VR", "strings": { "en_us": { "name": "HLA" } } }] }
                """,
            [@"C:\Steam\steamapps\appmanifest_546560.acf"] = """
                "AppState" { "appid" "546560" "name" "Half-Life: Alyx" "installdir" "Half-Life Alyx" }
                """,
        });

        var gameListService = new VRGameListService(reader);
        var processManager = new FakeProcessManager();
        var orchestrator = new VROrchestrator(processManager);
        var handler = new VRCommandHandler(gameListService, orchestrator);

        var output = await CaptureStdout(() => handler.HandleAsync("vr-games"));

        output.Should().HaveCount(1);
        var json = JsonDocument.Parse(output[0]);
        json.RootElement.GetProperty("games").GetArrayLength().Should().Be(1);
    }

    [TestMethod]
    public async Task HandleAsync_VrLaunch_StreamsMultipleJsonLines()
    {
        var processManager = new FakeProcessManager(
            hmdPresent: true,
            vrServerRunning: true,
            gameProcessName: "game.exe"
        );

        var reader = new FakeFileReader(new Dictionary<string, string>());
        var gameListService = new VRGameListService(reader);
        var orchestrator = new VROrchestrator(processManager);
        var handler = new VRCommandHandler(gameListService, orchestrator);

        var output = await CaptureStdout(() => handler.HandleAsync("vr-launch 546560"));

        // Should have multiple lines: headset_check, steamvr_start, game_launch, process_confirm, complete
        output.Count.Should().BeGreaterThanOrEqualTo(4);
        output.Last().Should().Contain("\"status\":\"complete\"");
    }

    private static VRCommandHandler CreateHandler()
    {
        var reader = new FakeFileReader(new Dictionary<string, string>());
        var gameListService = new VRGameListService(reader);
        var processManager = new FakeProcessManager();
        var orchestrator = new VROrchestrator(processManager);
        return new VRCommandHandler(gameListService, orchestrator);
    }

    private static async Task<List<string>> CaptureStdout(Func<Task> action)
    {
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            await action();
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        return sw.ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
    }
}
```

- [ ] **Step 2: Verify the tests fail (handler doesn't exist yet)**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
dotnet build VRService.sln -p:EnableWindowsTargeting=true 2>&1 | head -20
```

Expected: Build errors — `VRCommandHandler` not found.

- [ ] **Step 3: Create VRCommandHandler.cs**

```csharp
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace VRService;

public class VRCommandHandler
{
    private readonly VRGameListService _gameListService;
    private readonly VROrchestrator _orchestrator;

    private static readonly Dictionary<int, Action> PostLaunchHooks = new()
    {
        [1086940] = OpenDungeonMasterTerminal, // Baldur's Gate 3
    };

    public VRCommandHandler(VRGameListService gameListService, VROrchestrator orchestrator)
    {
        _gameListService = gameListService;
        _orchestrator = orchestrator;
    }

    public async Task HandleAsync(string command)
    {
        try
        {
            if (command == "vr-games")
            {
                HandleVrGames();
            }
            else if (command.StartsWith("vr-launch "))
            {
                string appIdStr = command["vr-launch ".Length..].Trim();
                if (int.TryParse(appIdStr, out int appId))
                    await HandleVrLaunchAsync(appId);
                else
                    WriteJsonLine(new VrLaunchStep { Status = "failed", Error = $"Invalid app ID: {appIdStr}" });
            }
            else if (command == "vr-devices")
            {
                await HandleVrDevicesAsync();
            }
            else if (command == "vr-status")
            {
                HandleVrStatus();
            }
            else if (command == "vr-kill")
            {
                HandleVrKill();
            }
            else
            {
                WriteJsonLine(new VrLaunchStep { Status = "failed", Error = $"Unknown VR command: {command}" });
            }
        }
        catch (Exception e)
        {
            Log.Error($"VRCommandHandler: {e.GetType()}: {e.Message}");
            try
            {
                WriteJsonLine(new VrLaunchStep { Status = "failed", Error = "Internal error" });
            }
            catch { /* stdout may be broken */ }
        }
    }

    private void HandleVrGames()
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
        Console.Out.WriteLine(JsonSerializer.Serialize(response));
    }

    private async Task HandleVrLaunchAsync(int appId)
    {
        var games = _gameListService.GetVrGames();
        var game = games.FirstOrDefault(g => g.AppId == appId);
        string fullInstallPath = game?.InstallPath ?? "";

        await foreach (var step in _orchestrator.LaunchAsync(appId, fullInstallPath))
        {
            WriteJsonLine(step);

            if (step.Step == "game_launch" && step.Status == "ok" && PostLaunchHooks.TryGetValue(appId, out var hook))
            {
                try
                {
                    hook();
                    Log.Command($"VR post-launch hook fired for appId {appId}");
                }
                catch (Exception e)
                {
                    Log.Error($"VR post-launch hook failed for appId {appId}: {e.Message}");
                }
            }
        }
    }

    private async Task HandleVrDevicesAsync()
    {
        var status = await _orchestrator.GetDeviceStatusAsync();
        Console.Out.WriteLine(JsonSerializer.Serialize(status));
    }

    private void HandleVrStatus()
    {
        var status = _orchestrator.GetStatus();
        Console.Out.WriteLine(JsonSerializer.Serialize(status));
    }

    private void HandleVrKill()
    {
        var games = _gameListService.GetVrGames();
        var installDirs = games
            .Where(g => !string.IsNullOrEmpty(g.InstallPath))
            .Select(g => g.InstallPath!)
            .Distinct()
            .ToList();

        var result = _orchestrator.KillVrSession(installDirs);
        Log.Command($"VR kill: {result.Killed.Count} processes terminated");
        Console.Out.WriteLine(result.ToJson());
    }

    private static void WriteJsonLine(VrLaunchStep step)
    {
        Console.Out.WriteLine(step.ToJson());
    }

    private const string WindowsTerminalPreview =
        @"C:\Users\phuze\AppData\Local\Microsoft\WindowsApps\Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe\wt.exe";

    private const string AudioBridgeExe =
        @"C:\Users\phuze\Dropbox\Programming\BG3DungeonMaster\publish-new\BG3DungeonMaster.AudioBridge.exe";

    private static void OpenDungeonMasterTerminal()
    {
        Process.Start(new ProcessStartInfo(WindowsTerminalPreview, "-w 0 new-tab -p \"wsl_claude_dungeonmaster\"")
        {
            UseShellExecute = true,
            CreateNoWindow = true
        });

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
                Log.Command("AudioBridge started for DM voice pipeline");
            }
            catch (Exception e)
            {
                Log.Error($"AudioBridge failed to start: {e.Message}");
            }
        }
        else
        {
            Log.Command("AudioBridge already running, skipping launch");
        }

        _ = Task.Run(async () =>
        {
            IntPtr hwnd = IntPtr.Zero;
            for (int i = 0; i < 45 && hwnd == IntPtr.Zero; i++)
            {
                await Task.Delay(2000);
                hwnd = GetBg3WindowHandle();
            }

            if (hwnd == IntPtr.Zero)
            {
                Log.Error("BG3 window not found after 90s, giving up on refocus");
                return;
            }

            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(3000);
                SetForegroundWindow(hwnd);
            }
            Log.Command("BG3 refocus sequence complete");
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
```

- [ ] **Step 4: Verify all tests pass**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
dotnet test VRService.sln -p:EnableWindowsTargeting=true --verbosity normal
```

Expected: 17 tests passed (5 + 2 + 7 + 3 new).

- [ ] **Step 5: Commit**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
git add VRService/VRCommandHandler.cs VRService.Tests/VRCommandHandlerTests.cs
git commit -m "feat: move VRCommandHandler from DesktopShell, output JSON to stdout"
```

---

## Task 7: Wire Up Program.cs Entry Point

**Files:**
- Modify: `VRService/Program.cs`

- [ ] **Step 1: Complete Program.cs with full command handling**

Replace the stub `Program.cs` with:

```csharp
namespace VRService;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: VRService.exe <command> [args]");
            Console.Error.WriteLine("Commands: vr-games, vr-launch <appId>, vr-devices, vr-status, vr-kill");
            return 1;
        }

        string command = string.Join(' ', args);
        Log.Info($"VRService started: {command}");

        try
        {
            var fileReader = new DiskFileReader();
            var gameListService = new VRGameListService(fileReader);
            var processManager = new ProcessManager();
            var orchestrator = new VROrchestrator(processManager);
            var handler = new VRCommandHandler(gameListService, orchestrator);

            await handler.HandleAsync(command);

            Log.Info($"VRService completed: {command}");
            return 0;
        }
        catch (Exception e)
        {
            Log.Error($"VRService fatal: {e.GetType()}: {e.Message}");
            return 1;
        }
    }
}
```

- [ ] **Step 2: Verify the project builds**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
dotnet build VRService.sln -p:EnableWindowsTargeting=true
```

Expected: Build succeeded.

- [ ] **Step 3: Verify all tests still pass**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
dotnet test VRService.sln -p:EnableWindowsTargeting=true --verbosity normal
```

Expected: 17 tests passed.

- [ ] **Step 4: Commit**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
git add VRService/Program.cs
git commit -m "feat: wire up Program.cs entry point with CLI arg parsing"
```

---

## Task 8: Replace DesktopShell VR Handling with VRService Process Spawn

**Files:**
- Modify: `DesktopShell/TCPServer.cs`

This replaces the inline `VrHandler` with process spawning + stdout/stderr relay per the design spec.

- [ ] **Step 1: Replace the vr-* case in HandleClientComm**

In `DesktopShell/TCPServer.cs`, replace the entire `case string cmd when cmd.StartsWith("vr-"):` block (lines 262-278) and remove the VR imports/fields (lines 8, 18-25).

Remove at the top of the file:
```csharp
using DesktopShell.VR;
```

Remove the lazy VrHandler field:
```csharp
    private static readonly Lazy<VRCommandHandler> VrHandler = new(() =>
    {
        var fileReader = new DiskFileReader();
        var gameListService = new VRGameListService(fileReader);
        var processManager = new ProcessManager();
        var orchestrator = new VROrchestrator(processManager);
        return new VRCommandHandler(gameListService, orchestrator);
    });
```

Replace the vr-* case with:

```csharp
                        case string cmd when cmd.StartsWith("vr-"):
                            GlobalVar.Log($"$$$ VR command: {cmd}");
                            RelayVrService(cmd, clientStream);
                            isCommunicationOver = true;
                            break;
```

Add the `RelayVrService` method to the `TCPServer` class:

```csharp
    private const string VrServicePath = @"Bin\VRService.exe";
    private const int VrServiceTimeoutMs = 90_000;

    private static void RelayVrService(string command, Stream clientStream)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, VrServicePath);

        if (!File.Exists(fullPath))
        {
            GlobalVar.ToolTip("VR", "VRService.exe not found in Bin folder");
            GlobalVar.Log($"### VRService not found at: {fullPath}");
            TryWriteErrorJson(clientStream, "VR service not available");
            return;
        }

        Process? process = null;
        int stdoutLines = 0;

        try
        {
            var psi = new ProcessStartInfo(fullPath, command)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            process = Process.Start(psi);
            if (process == null)
            {
                GlobalVar.ToolTip("VR", "Failed to start VRService");
                TryWriteErrorJson(clientStream, "VR service failed to start");
                return;
            }

            // Stderr → DesktopShell log (background thread)
            var stderrThread = new Thread(() =>
            {
                try
                {
                    while (process.StandardError.ReadLine() is { } line)
                        GlobalVar.Log($"[VRService] {line}");
                }
                catch { /* process exited or stream closed */ }
            })
            { IsBackground = true };
            stderrThread.Start();

            // Stdout → TCP client (relay loop)
            using var cts = new CancellationTokenSource(VrServiceTimeoutMs);
            try
            {
                while (process.StandardOutput.ReadLine() is { } line)
                {
                    GlobalVar.WriteRemoteCommand(clientStream, line, includePassPhrase: true);
                    stdoutLines++;
                }
            }
            catch (IOException)
            {
                // TCP client disconnected — kill VRService
                GlobalVar.Log("### VR relay: TCP client disconnected, killing VRService");
                try { process.Kill(); } catch { }
                return;
            }

            // Wait for process exit (respect timeout)
            if (!process.WaitForExit(VrServiceTimeoutMs))
            {
                GlobalVar.Log("### VRService timed out after 90s, killing process");
                try { process.Kill(); } catch { }
                if (stdoutLines == 0)
                    TryWriteErrorJson(clientStream, "VR service timed out");
                return;
            }

            // Check exit code
            if (process.ExitCode != 0 && stdoutLines == 0)
            {
                GlobalVar.Log($"### VRService exited with code {process.ExitCode}");
                TryWriteErrorJson(clientStream, "VR service exited with error");
            }
        }
        catch (Exception e)
        {
            GlobalVar.ToolTip("VR", $"VRService error: {e.Message}");
            GlobalVar.Log($"### VR relay error: {e.GetType()}: {e.Message}");
            if (stdoutLines == 0)
                TryWriteErrorJson(clientStream, "VR service error");
        }
        finally
        {
            process?.Dispose();
        }
    }

    private static void TryWriteErrorJson(Stream stream, string error)
    {
        try
        {
            string json = $"{{\"status\":\"failed\",\"error\":\"{error}\"}}";
            GlobalVar.WriteRemoteCommand(stream, json, includePassPhrase: true);
        }
        catch { /* stream may already be closed */ }
    }
```

- [ ] **Step 2: Verify DesktopShell builds without VR code**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/DesktopShell
dotnet build DesktopShell.sln -p:EnableWindowsTargeting=true
```

Expected: Build succeeded. The VR/ files still exist but are now unused — they'll be removed in the next task.

**Note:** If there are build errors because the VR/ files reference `GlobalVar` and other DesktopShell types, they will still compile fine since they're still in the project. The key verification is that `TCPServer.cs` no longer references `DesktopShell.VR`.

- [ ] **Step 3: Commit DesktopShell changes**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/DesktopShell
git add DesktopShell/TCPServer.cs
git commit -m "feat: replace inline VR handler with VRService process spawn and relay"
```

---

## Task 9: Remove VR Code from DesktopShell

**Files:**
- Delete: `DesktopShell/VR/VRCommandHandler.cs`
- Delete: `DesktopShell/VR/VROrchestrator.cs`
- Delete: `DesktopShell/VR/VRGameListService.cs`
- Delete: `DesktopShell/VR/SteamConfigParser.cs`
- Delete: `DesktopShell.Tests/VR/SteamConfigParserTests.cs`
- Delete: `DesktopShell.Tests/VR/VRGameListServiceTests.cs`
- Delete: `DesktopShell.Tests/VR/VROrchestratorTests.cs`

- [ ] **Step 1: Remove VR source files**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/DesktopShell
rm DesktopShell/VR/VRCommandHandler.cs
rm DesktopShell/VR/VROrchestrator.cs
rm DesktopShell/VR/VRGameListService.cs
rm DesktopShell/VR/SteamConfigParser.cs
rmdir DesktopShell/VR
```

- [ ] **Step 2: Remove VR test files**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/DesktopShell
rm DesktopShell.Tests/VR/SteamConfigParserTests.cs
rm DesktopShell.Tests/VR/VRGameListServiceTests.cs
rm DesktopShell.Tests/VR/VROrchestratorTests.cs
rmdir DesktopShell.Tests/VR
```

- [ ] **Step 3: Verify DesktopShell builds without VR files**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/DesktopShell
dotnet build DesktopShell.sln -p:EnableWindowsTargeting=true
```

Expected: Build succeeded. 0 Error(s).

- [ ] **Step 4: Verify DesktopShell tests pass (should be 87 minus 13 VR tests = 74)**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/DesktopShell
dotnet test DesktopShell.sln -p:EnableWindowsTargeting=true --verbosity normal
```

Expected: All remaining tests pass (74 tests, 0 failures).

- [ ] **Step 5: Commit**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/DesktopShell
git add -A DesktopShell/VR/ DesktopShell.Tests/VR/
git commit -m "refactor: remove VR module from DesktopShell (moved to VRService)"
```

---

## Task 10: Publish and Deploy

**Files:**
- No new files — publish and copy artifacts

- [ ] **Step 1: Publish VRService as single-file exe**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
dotnet publish VRService/VRService.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:EnableWindowsTargeting=true
```

Expected: VRService.exe produced in `VRService/bin/Release/net10.0-windows/win-x64/publish/`.

- [ ] **Step 2: Deploy VRService.exe to DesktopShell Bin folder**

```bash
\cp /mnt/c/Users/phuze/Dropbox/Programming/VRService/VRService/bin/Release/net10.0-windows/win-x64/publish/VRService.exe "/mnt/c/Program Files/DesktopShell/Bin/VRService.exe"
```

- [ ] **Step 3: Verify VRService.exe is in place**

```bash
ls -la "/mnt/c/Program Files/DesktopShell/Bin/VRService.exe"
```

Expected: File exists.

- [ ] **Step 4: Publish and deploy DesktopShell**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/DesktopShell
dotnet publish DesktopShell/DesktopShell.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:EnableWindowsTargeting=true
\cp DesktopShell/bin/Release/net10.0-windows10.0.26100.0/win-x64/publish/DesktopShell.exe "/mnt/c/Program Files/DesktopShell/DesktopShell.exe"
```

- [ ] **Step 5: Commit VRService publish configuration**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
git add -A
git commit -m "chore: final project state after initial implementation"
```

---

## Task 11: Create CLAUDE.md for VRService

**Files:**
- Create: `C:\Users\phuze\Dropbox\Programming\VRService\CLAUDE.md`

- [ ] **Step 1: Write CLAUDE.md**

```markdown
# VRService

Standalone CLI tool for VR game management. Extracted from DesktopShell. Launches VR games via Steam, queries SteamVR devices, manages VR sessions.

## Build / Run / Test

```bash
# Build
dotnet build VRService.sln

# Run (requires Windows — uses Steam, SteamVR, P/Invoke)
VRService.exe vr-games
VRService.exe vr-launch <appId>
VRService.exe vr-devices
VRService.exe vr-status
VRService.exe vr-kill

# Publish single-file
dotnet publish VRService/VRService.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true

# Run all tests
dotnet test VRService.sln

# Linux CI (sandboxed build + test)
dotnet build VRService.sln -p:EnableWindowsTargeting=true
dotnet test VRService.sln -p:EnableWindowsTargeting=true
```

## Architecture

- **Program.cs** — CLI entry point. Parses args, creates dependencies, calls VRCommandHandler, exits.
- **VRCommandHandler** — Routes commands to appropriate handler. Writes JSON to stdout, diagnostics to stderr.
- **VROrchestrator** — Launch pipeline (headset check → SteamVR → game launch → process confirm). Kill session. Device status.
- **VRGameListService** — Discovers VR games from Steam library folders, VR manifest, and cloud storage collections.
- **SteamConfigParser** — Parses Steam VDF config files and JSON manifests.
- **Log** — Static stderr helper with DesktopShell prefix convention (`!!!`, `^^^`, `###`, `$$$`, `&&&`).

## Output Protocol

- **stdout**: JSON lines (one JSON object per line). DesktopShell relays these to the TCP client.
- **stderr**: Timestamped diagnostic lines. DesktopShell captures these into its log with `[VRService]` prefix.
- **Exit code**: 0 = success, 1 = fatal error.

## Dependencies

**NuGet (main):** None (uses built-in System.Text.Json, System.Diagnostics.Process)

**NuGet (tests):** MSTest 3.8.3, FluentAssertions 7.2.0, Moq 4.20.72, coverlet.collector

**PInvoke:** user32.dll — `SetForegroundWindow` (BG3 post-launch hook)

## Deployment

Published single-file exe to `C:\Program Files\DesktopShell\Bin\VRService.exe`.

## Key Interfaces

- `IProcessManager` — Abstraction over system process operations (testable with FakeProcessManager)
- `IFileReader` — Abstraction over file system reads (testable with FakeFileReader)
```

- [ ] **Step 2: Commit**

```bash
cd /mnt/c/Users/phuze/Dropbox/Programming/VRService
git add CLAUDE.md
git commit -m "docs: add CLAUDE.md with project documentation"
```
