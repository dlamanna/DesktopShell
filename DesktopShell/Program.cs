using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DesktopShell;

static class Program
{
    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// 

    [STAThread]
    static void Main()
    {
        try
        {
            // Set DPI awareness before enabling visual styles
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }

            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.SetCompatibleTextRenderingDefault(false);

            //Scan through running processes to see if there's already an instance
            Boolean isRunning = false;
            Process[] processList = Process.GetProcessesByName("DesktopShell");
            if (processList.Length > 1)
            {
                foreach (Process p in processList)
                {
                    Console.WriteLine($"Process Found: {p.ProcessName}\t{p.Id}");
                }
                GlobalVar.ToolTip("Error", "DesktopShell already running!");
                isRunning = true;
            }

            if (!isRunning)
                Application.Run(GlobalVar.ShellInstance = new Shell());
        }
        catch (Exception ex)
        {
            string errorMsg = $"DesktopShell Fatal Error:\n\n{ex.GetType().Name}: {ex.Message}\n\nStackTrace:\n{ex.StackTrace}";

            // Try to show message box
            try
            {
                MessageBox.Show(errorMsg, "DesktopShell Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                // If MessageBox fails, write to console and file
                Console.Error.WriteLine(errorMsg);
            }

            // Also write to a log file for debugging
            try
            {
                System.IO.File.AppendAllText("DesktopShell_error.log",
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {errorMsg}\n\n");
            }
            catch { }

            Environment.Exit(1);
        }
    }
}
