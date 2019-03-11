using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace DesktopShell
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //Scan through running processes to see if there's already an instance
            Boolean isRunning = false;
            Process[] processList = Process.GetProcessesByName("DesktopShell");
            if(processList.Length > 1)
            {
                foreach(Process p in processList)
                {
                    Console.WriteLine("Process Found: " + p.ProcessName + "\t{" + p.Id + "}");
                }
                Process.Start("Bin\\ToolTipper.exe", "DesktopShell already running!");
                isRunning = true;
            }

            if(!isRunning)
                Application.Run(GlobalVar.shellInstance = new Shell());
        }
    }
}
