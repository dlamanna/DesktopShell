namespace DesktopShell;

internal class WebCombo
{
    public string keyword { get; init; }
    public List<string> websiteBase { get; init; }
    public bool? searchable { get; init; }

    public WebCombo(string? _keyword, string _websiteBase)
    {
        if (string.IsNullOrEmpty(_keyword))
        {
            throw new ArgumentException($"'{nameof(_keyword)}' cannot be null or empty.", nameof(_keyword));
        }

        keyword = _keyword;
        websiteBase = [_websiteBase];
        searchable = false;
    }

    public WebCombo(string? _keyword, string _websiteBase, bool? _searchable)
    {
        if (string.IsNullOrEmpty(_keyword))
        {
            throw new ArgumentException($"'{nameof(_keyword)}' cannot be null or empty.", nameof(_keyword));
        }

        keyword = _keyword;
        websiteBase = [_websiteBase];
        searchable = _searchable;
    }

    public WebCombo(string? _keyword, List<string> _websiteBase, bool? _searchable)
    {
        if (string.IsNullOrEmpty(_keyword))
        {
            throw new ArgumentException($"'{nameof(_keyword)}' cannot be null or empty.", nameof(_keyword));
        }

        keyword = _keyword;
        websiteBase = _websiteBase ?? throw new ArgumentNullException(nameof(_websiteBase));
        searchable = _searchable;
    }
}
