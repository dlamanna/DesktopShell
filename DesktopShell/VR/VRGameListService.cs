namespace DesktopShell.VR;

public record VrGame(int AppId, string Name, string InstallDir);

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

    // Default Steam paths on Windows
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
        // 1. Get Steam library paths
        string libraryVdf = _reader.ReadFile(Path.Combine(DefaultSteamPath, LibraryFoldersFile)) ?? "";
        var libraries = SteamConfigParser.ParseLibraryFolders(libraryVdf);
        if (libraries.Count == 0)
        {
            GlobalVar.Log("### VRGameListService: No Steam libraries found");
            return [];
        }

        // 2. Build set of all installed app IDs
        var installedApps = new HashSet<int>();
        foreach (var lib in libraries)
            foreach (int appId in lib.AppIds)
                installedApps.Add(appId);

        // 3. Get native VR app IDs from vrmanifest
        string vrManifestJson = _reader.ReadFile(Path.Combine(DefaultSteamPath, VrManifestFile)) ?? "";
        var vrManifestAppIds = SteamConfigParser.ParseVrManifest(vrManifestJson);

        // 4. Get user VR category overrides from cloud storage
        var vrCollection = FindVrCollection();
        var addedIds = vrCollection?.Added ?? [];
        var removedIds = new HashSet<int>(vrCollection?.Removed ?? []);

        // 5. Combine: (manifest ∪ added) - removed, filtered to installed
        var candidateIds = new HashSet<int>(vrManifestAppIds);
        foreach (int id in addedIds)
            candidateIds.Add(id);
        candidateIds.ExceptWith(removedIds);
        candidateIds.IntersectWith(installedApps);

        // 6. Look up names from app manifests
        var games = new List<VrGame>();
        foreach (int appId in candidateIds)
        {
            var appInfo = FindAppInfo(appId, libraries);
            if (appInfo != null)
                games.Add(new VrGame(appInfo.AppId, appInfo.Name, appInfo.InstallDir));
        }

        games.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return games;
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
