namespace DesktopShell
{
    internal class WwwBrowser
    {
        public string? keyword;
        public string? filePath;
        public bool defaultBrowser;

        //Constructors
        public WwwBrowser(string? _keyword, string? _filePath, bool _defaultBrowser) { keyword = _keyword; filePath = _filePath; defaultBrowser = _defaultBrowser; }
    }
}