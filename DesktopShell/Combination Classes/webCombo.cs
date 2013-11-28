using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DesktopShell
{
    class webCombo
    {
        public string keyword;
        public ArrayList websiteBase;
        public bool searchable;

        //Constructors
        public webCombo(string _keyword, string _websiteBase) { keyword = _keyword; websiteBase.Add(_websiteBase); searchable = false; }
        public webCombo(string _keyword, string _websiteBase, bool _searchable) { keyword = _keyword; websiteBase.Add(_websiteBase); searchable = _searchable; }
        public webCombo(string _keyword, ArrayList _websiteBase, bool _searchable) { keyword = _keyword; websiteBase = _websiteBase; searchable = _searchable; }
    }
}
