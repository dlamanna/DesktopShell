using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DesktopShell
{
    class Combination
    {
        public string keyword;
        public ArrayList filePath;
        public ArrayList arguments;

        //Constructors
        public Combination(string _keyword, ArrayList _filePath, ArrayList _arguments) { keyword = _keyword; filePath = _filePath; arguments = _arguments; } 
    }
}
