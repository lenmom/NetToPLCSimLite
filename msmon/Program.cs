using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace calc
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // for single instance launch.
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);

            foreach (Process process in processes)
            {
                if (process.Id == current.Id)
                {
                    continue;
                }
                else
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // TODO
                    }
                }
            }

            Application.Run(new MainForm((args == null || args.Length == 0) ? false : true));
        }
    }
}
