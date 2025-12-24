namespace DesktopShell;

internal class WebCombo
{
    public string Keyword { get; init; }
    public List<string> WebsiteBase { get; init; }
    public bool? Searchable { get; init; }

    public WebCombo(string? keyword, string websiteBase)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            throw new ArgumentException($"'{nameof(keyword)}' cannot be null or empty.", nameof(keyword));
        }

        Keyword = keyword;
        WebsiteBase = [websiteBase];
        Searchable = false;
    }

    public WebCombo(string? keyword, string websiteBase, bool? searchable)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            throw new ArgumentException($"'{nameof(keyword)}' cannot be null or empty.", nameof(keyword));
        }

        Keyword = keyword;
        WebsiteBase = [websiteBase];
        Searchable = searchable;
    }

    public WebCombo(string? keyword, List<string> websiteBase, bool? searchable)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            throw new ArgumentException($"'{nameof(keyword)}' cannot be null or empty.", nameof(keyword));
        }

        Keyword = keyword;
        WebsiteBase = websiteBase ?? throw new ArgumentNullException(nameof(websiteBase));
        Searchable = searchable;
    }
}
