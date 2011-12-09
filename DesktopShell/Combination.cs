using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DesktopShell
{
    class Combination
    {
        public string keyword;
        public string filePath;
        public string arguments;

        //Constructors
        public Combination() { keyword = filePath = arguments = ""; }
        public Combination(string _keyword, string _arguments) { keyword = _keyword; arguments = _arguments; } 
    }
}
