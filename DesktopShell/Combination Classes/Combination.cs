using System.Collections.Generic;

namespace DesktopShell
{
    internal class Combination
    {
        public string? keyword;
        public List<string> filePath;
        public List<string> arguments;

        //Constructors
        public Combination(string? _keyword, List<string> _filePath, List<string> _arguments) 
        { 
            keyword = _keyword; 
            filePath = _filePath; 
            arguments = _arguments; 
        }
    }
}
