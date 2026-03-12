using System.Text.Json;
using System.Text.RegularExpressions;

namespace DesktopShell.VR;

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
                if (!valueObj.TryGetProperty("value", out var valueStr))
                    continue;

                string? innerJson = valueStr.GetString();
                if (innerJson == null) continue;

                using var inner = JsonDocument.Parse(innerJson);
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
        catch (JsonException)
        {
            // Corrupt config — return null
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
