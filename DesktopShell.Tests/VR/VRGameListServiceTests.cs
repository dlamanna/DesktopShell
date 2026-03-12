using DesktopShell.VR;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopShell.Tests.VR;

[TestClass]
public class VRGameListServiceTests
{
    [TestMethod]
    public void GetVrGames_CombinesManifestAndCategoryOverrides()
    {
        // vrmanifest has app 546560 (native VR)
        // VR category added: 1086940 (non-native, e.g. BG3 with RealVR)
        // VR category removed: 348250 (user doesn't want Google Earth VR)
        // App 999 is in category added but NOT installed
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

        // 546560 from manifest (not removed)
        // 1086940 from category added (installed)
        // 348250 removed by category
        // 999 not installed, excluded
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

// Test double for file system access
public class FakeFileReader : IFileReader
{
    private readonly Dictionary<string, string> _files;

    public FakeFileReader(Dictionary<string, string> files)
    {
        _files = files;
    }

    public string? ReadFile(string path)
    {
        // Match by exact path or by filename suffix
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

    public string[] GetDirectories(string path) => [];
}
