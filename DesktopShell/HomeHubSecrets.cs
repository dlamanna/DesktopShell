namespace HomeHub;

/// <summary>Reads a credential: process env first, then the HomeHub secrets shim
/// ($HOMEHUB_SECRETS, else %LOCALAPPDATA%\homehub\.secrets). Values never logged.</summary>
public static class HomeHubSecrets
{
    static Dictionary<string, string>? cache;
    static string? cachePath;

    public static string? ShimPath()
    {
        var fromEnv = Environment.GetEnvironmentVariable("HOMEHUB_SECRETS");
        if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv)) return fromEnv;
        var local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "homehub", ".secrets");
        return File.Exists(local) ? local : null;
    }

    static Dictionary<string, string> Load()
    {
        var path = ShimPath();
        if (cache is not null && cachePath == path) return cache;
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (path is not null)
        {
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var v = line[(eq + 1)..].Trim();
                if (v.Length >= 2 && v[0] == v[^1] && (v[0] == '\'' || v[0] == '"')) v = v[1..^1];
                map[line[..eq].Trim()] = v;
            }
        }
        cache = map; cachePath = path;
        return map;
    }

    public static string? Get(string name)
    {
        var env = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(env)) return env.Trim();
        return Load().TryGetValue(name, out var v) && v.Length > 0 ? v : null;
    }

    public static string Source(string name) =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)) ? "env"
        : Load().ContainsKey(name) ? "shim:" + ShimPath() : "missing";
}
