using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IsoOnTcp;

namespace PLCSimConnector
{
    internal class MainModel
    {
        /// <summary>
        /// Get(s) or set(s) self mananged.
        /// 1. <c>true</c>, the application controls all the pre running condition it self;
        /// 2. <c>false</c>, the pre-running condition would be controlled by external.
        /// </summary>
        internal bool IsSelfManaged { get; set; }

        internal readonly Config m_Conf = new Config();
        internal readonly List<IsoToS7online> m_servers = new List<IsoToS7online>();

        internal readonly CmdLineArgs startArgs = new CmdLineArgs();

        internal string m_ConfigName = "";
        internal string m_IEPGhelperServiceName = string.Empty;
        internal bool m_S7DOSServiceStopped;
    }
}
