using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DesktopShell
{
    class wwwBrowser
    {
        public string keyword;
        public string filePath;
        public bool defaultBrowser;

        //Constructors
        public wwwBrowser(string _keyword, string _filePath, bool _defaultBrowser) { keyword = _keyword; filePath = _filePath; defaultBrowser = _defaultBrowser; }
    }
}
