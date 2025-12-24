using System.Text;

namespace DesktopShell.Tests.TestHelpers
{
    public static class TestHelpers
    {
        public static string CreateTempSettingsFile(string content)
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, content, Encoding.UTF8);
            return tempFile;
        }
        
        public static string CreateTempShortcutsFile(string content)
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, content, Encoding.UTF8);
            return tempFile;
        }
        
        public static string CreateTempWebsitesFile(string content)
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, content, Encoding.UTF8);
            return tempFile;
        }
        
        public static string CreateTempWebBrowsersFile(string content)
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, content, Encoding.UTF8);
            return tempFile;
        }
        
        public static void CleanupTempFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    // Log cleanup failure but don't fail tests
                    Console.WriteLine($"Failed to cleanup temp file {filePath}: {ex.Message}");
                }
            }
        }
        
        public static void CleanupTempFiles(params string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                CleanupTempFile(filePath);
            }
        }
        
        public static string GetSampleShortcutsContent()
        {
            return @"notepad
C:\Windows\System32\notepad.exe
-
calc
C:\Windows\System32\calc.exe
calculator
chrome
C:\Program Files\Google\Chrome\Application\chrome.exe
--new-window";
        }
        
        public static string GetSampleWebsitesContent()
        {
            return @"google
True
https://www.google.com/search?q=
youtube
True
https://www.youtube.com/results?search_query=
github
False
https://github.com/";
        }
        
        public static string GetSampleWebBrowsersContent()
        {
            return @"chrome
C:\Program Files\Google\Chrome\Application\chrome.exe
firefox
C:\Program Files\Mozilla Firefox\firefox.exe
edge
C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
        }
        
        public static string GetSampleSettingsContent()
        {
            return @"fontColor=#FF0000
backgroundColor=#0000FF
hourlyChime=true
screensEnabled=true,false
updateCheck=false
musicDirectory=C:\Music
gamesDirectory=C:\Games
moviesDirectory=C:\Movies
showsDirectory=C:\Shows
videoPlayer=C:\Program Files\VLC\vlc.exe
positionSave=100,200";
        }
    }
}
