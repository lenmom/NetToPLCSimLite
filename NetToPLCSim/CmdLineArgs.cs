/*********************************************************************
 * NetToPLCsim, Netzwerkanbindung fuer PLCSIM
 * 
 * Copyright (C) 2011-2016 Thomas Wiens, th.wiens@gmx.de
 *
 * This file is part of NetToPLCsim.
 *
 * NetToPLCsim is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 /*********************************************************************/

using System;
using System.IO;
using System.Windows.Forms;

namespace NetToPLCSim
{
    public enum eAutoStopService
    {
        YES,
        NO,
        ASK
    }

    public enum eAutoStart
    {
        YES,
        NO
    }

    internal class CmdLineArgs
    {
        public CmdLineArgs()
        {
            setDefaults();
        }

        public bool ArgsGiven { get; private set; }

        public eAutoStopService AutoStopService { get; private set; }

        public eAutoStart AutoStart { get; private set; }

        public bool Visible { get; private set; } = true;

        public string StartIni { get; private set; }

        private void setDefaults()
        {
            ArgsGiven = false;
            StartIni = string.Empty;
            AutoStopService = eAutoStopService.ASK;
            AutoStart = eAutoStart.NO;
        }

        public void parseCmdLineArgs(string[] args)
        {
            int i = 0;
            string opt;

            foreach (string arg in args)
            {
                i++;
                if (i == 1)
                {
                    continue;
                }

                if (arg == "-help" || arg == "--help" || arg == "-?")
                {
                    MessageBox.Show(getHelpText(), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (arg.StartsWith("-s="))
                {
                    opt = arg.Substring(3, arg.Length - 3).ToUpper();
                    if (opt == "YES")
                    {
                        AutoStopService = eAutoStopService.YES;
                    }
                    else if (opt == "NO")
                    {
                        AutoStopService = eAutoStopService.NO;
                    }
                }
                else if (arg == "-autostart")
                {
                    AutoStart = eAutoStart.YES;
                }
                else if (arg == "-novisible")
                {
                    Visible = false;
                }
                else if (arg.StartsWith("-f=") || i == 2)
                {
                    if (arg.StartsWith("-f="))
                    {
                        opt = arg.Substring(3, arg.Length - 3);
                        StartIni = opt;
                    }
                    else if (i == 2) // When as first parameter a filename is given, an ini file was dropped on nettoplcsim.exe
                    {
                        opt = arg.ToUpper();
                        if (opt.EndsWith(".INI"))
                        {
                            StartIni = arg;
                        }
                    }

                    // Check if full path is given, otherwise extend with working directory
                    if (StartIni.StartsWith(Environment.CurrentDirectory) == false)
                    {
                        StartIni = Environment.CurrentDirectory + "\\" + StartIni;
                    }

                    if (File.Exists(StartIni) == false)
                    {
                        StartIni = string.Empty;
                    }
                }
            }

            if (StartIni == string.Empty) // if no ini file is given, disable autostart as it's not possible
            {
                AutoStart = eAutoStart.NO;
            }
        }

        public string getHelpText()
        {
            string text =
                "NetToPLCSim - A network interface to Plcsim." + Environment.NewLine +
                Environment.NewLine +
                "Command line options:" + Environment.NewLine +
                Environment.NewLine +
                "NetToPLCSim.exe [configuration.ini] [-f=configuration.ini] [-s=Option] [-autostart]" +
                Environment.NewLine +
                "Options:" + Environment.NewLine +
                "-f=configuration.ini\tStart with this station configuration" + Environment.NewLine +
                "-s=Option\t\tAutostop IEPG-Helper service" + Environment.NewLine +
                "\t\tOptions: NO, YES, ASK" + Environment.NewLine +
                "-autostart\t\tAutostart with configuration file" + Environment.NewLine+
                "-novisible \t\tHide main form.";

            return text;
        }
    }
}