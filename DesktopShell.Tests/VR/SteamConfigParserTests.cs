using DesktopShell.VR;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopShell.Tests.VR;

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
        // The cloud storage file is a JSON array of [key, {key, timestamp, value}] pairs
        // The "value" field is a JSON string that must be parsed again
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
