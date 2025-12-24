using System.Collections.Generic;

namespace DesktopShell
{
    internal class WebCombo
    {
        public string? keyword;
        public List<string> websiteBase;
        public bool? searchable;

        public WebCombo(string? _keyword, string _websiteBase) 
        {
            if (string.IsNullOrEmpty(_keyword))
            {
                throw new System.ArgumentException($"'{nameof(_keyword)}' cannot be null or empty.", nameof(_keyword));
            }

            keyword = _keyword;
            if (websiteBase == null) websiteBase = new List<string>();
            if (_websiteBase != null) websiteBase.Add(_websiteBase);
            searchable = false; 
        }

        public WebCombo(string? _keyword, string _websiteBase, bool? _searchable) 
        {
            if (string.IsNullOrEmpty(_keyword))
            {
                throw new System.ArgumentException($"'{nameof(_keyword)}' cannot be null or empty.", nameof(_keyword));
            }

            keyword = _keyword;
            if (websiteBase == null) websiteBase = new List<string>();
            if (_websiteBase != null) websiteBase.Add(_websiteBase);
            searchable = _searchable; 
        }

        public WebCombo(string? _keyword, List<string> _websiteBase, bool? _searchable) 
        {
            if (string.IsNullOrEmpty(_keyword))
            {
                throw new System.ArgumentException($"'{nameof(_keyword)}' cannot be null or empty.", nameof(_keyword));
            }

            keyword = _keyword;
            websiteBase = _websiteBase ?? throw new System.ArgumentNullException(nameof(_websiteBase));
            searchable = _searchable;
        }
    }
}
