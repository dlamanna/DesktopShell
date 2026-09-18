namespace HomeHub;

/// <summary>Reads a credential: process env first, then the HomeHub secrets shim
/// ($HOMEHUB_SECRETS, else %LOCALAPPDATA%\homehub\.secrets). Values never logged.</summary>
public static class HomeHubSecrets
{
    // Immutable snapshot, published by a single reference swap so concurrent readers never see a
    // half-built map. Keyed by path + mtime + size, so a rewritten shim is re-read (one stat per call).
    sealed record Snapshot(string? Path, DateTime WriteUtc, long Length, Dictionary<string, string> Map);
    static volatile Snapshot? cache;

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
        DateTime writeUtc = default; long length = -1;
        if (path is not null)
        {
            try { var fi = new FileInfo(path); writeUtc = fi.LastWriteTimeUtc; length = fi.Length; }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        var snap = cache;
        if (snap is not null && snap.Path == path && snap.WriteUtc == writeUtc && snap.Length == length) return snap.Map;
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
        cache = new Snapshot(path, writeUtc, length, map);
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
        : Load().TryGetValue(name, out var v) && v.Length > 0 ? "shim:" + ShimPath() : "missing";
}
