using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace msmon
{
    internal partial class MainForm : Form
    {
        private readonly string simConnectorRootDir;

        internal const string CONFIG_FILE = "config.ini";
        internal const string PLC_SIM_CONNECTOR_PROCESS_NAME = "PLCSimConnector";
        internal const string INDICATOR_FLAG_FILE = "indicator.dat";


        /// <summary>
        /// PLCSim v15 & v16 & v17 process name.
        /// </summary>
        internal const string SIEMENS_SIMATIC_PLCSIM_COMPACT_PROCESS_NAME = "Siemens.Simatic.PlcSim.Compact";

        /// <summary>
        /// /// <summary>
        /// PLCSimAdvance v3.0  process name.
        /// </summary>
        /// </summary>
        internal const string SIEMENS_SIMATIC_PLCSIM_ADVANCE_V30_PROCESS_NAME = "Siemens.Simatic.PlcSim.Advanced.UserInterface";

        /// <summary>
        /// /// <summary>
        /// PLCSim v18 process name.
        /// </summary>
        /// </summary>
        internal const string SIEMENS_SIMATIC_PLCSIM_V18_PROCESS_NAME = "S7-PLCSIM";

        internal readonly string PLC_SIM_CONNECTOR_EXE_NAME;
        private bool m_visible = false;

        public MainForm(bool visible)
        {
            InitializeComponent();
            PLC_SIM_CONNECTOR_EXE_NAME = string.Format("{0}.exe", PLC_SIM_CONNECTOR_PROCESS_NAME);
            simConnectorRootDir = new System.IO.FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            this.m_visible = visible;

            this.Opacity = 0;
            this.ShowInTaskbar = false;
        }


        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            EnsureSimConnectorClosed();

            string iniFilePath = Path.Combine(simConnectorRootDir, CONFIG_FILE);
            string exeFullPath = Path.Combine(simConnectorRootDir, PLC_SIM_CONNECTOR_EXE_NAME);

            string indicatorFile = Path.Combine(simConnectorRootDir, INDICATOR_FLAG_FILE);
            if (File.Exists(indicatorFile))
            {
                try
                {
                    File.Delete(indicatorFile);
                }
                catch
                {
                    // TODO.
                }
            }

            if (File.Exists(exeFullPath) &&
                (Process.GetProcessesByName(SIEMENS_SIMATIC_PLCSIM_COMPACT_PROCESS_NAME).Any() ||
                Process.GetProcessesByName(SIEMENS_SIMATIC_PLCSIM_V18_PROCESS_NAME).Any() ||
                Process.GetProcessesByName(SIEMENS_SIMATIC_PLCSIM_ADVANCE_V30_PROCESS_NAME).Any()) &&
                Tools.GetPlcsimIpAddressList().Any())
            {
                LaunchProcessNormal(exeFullPath,
                                    simConnectorRootDir,
                                    string.Format("  -f=\"{0}\"  -s=YES -autostart {1} ",
                                                  iniFilePath,
                                                  m_visible ? "-visible" : string.Empty));
            }
            else
            {
                try
                {
                    File.Create(indicatorFile);
                }
                catch
                {
                    // TODO.
                }
            }
        }

        /// <summary>
        /// Launch(s) process using provided parameter(s).
        /// </summary>
        /// <param name="exeFullPath"></param>
        /// <param name="workDir"></param>
        /// <param name="arguments"></param>
        private void LaunchProcessNormal(string exeFullPath, string workDir, string arguments)
        {
            if (!File.Exists(exeFullPath))
            {
                return;
            }

            Process processToLaunch = new Process();
            processToLaunch.StartInfo.FileName = exeFullPath;
            processToLaunch.StartInfo.WorkingDirectory = workDir;
            processToLaunch.StartInfo.Arguments = arguments;
            processToLaunch.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            processToLaunch.StartInfo.CreateNoWindow = true;
            //processToLaunch.StartInfo.RedirectStandardError = true;
            //processToLaunch.StartInfo.RedirectStandardOutput = true;
            //processToLaunch.StartInfo.UseShellExecute = false;

            try
            {
                processToLaunch.Start();
                //logger.Info(processToLaunch.StandardOutput.ReadToEnd());
            }
            catch (Exception)
            {
                // TODO
            }
        }

        /// <summary>
        /// Make sure the SimConnector process has been killed.
        /// </summary>
        private static void EnsureSimConnectorClosed()
        {
            Process[] processes = Process.GetProcessesByName(PLC_SIM_CONNECTOR_PROCESS_NAME);
            if (processes != null && processes.Length > 0)
            {
                foreach (Process process in processes)
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
        }
    }
}
