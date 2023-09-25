using System;
using System.Net;
using System.Timers;

using log4net;

using S7PROSIMLib;

namespace NetToPLCSimLite.Models
{
    public enum StationCpu
    {
        S200 = 0,

        S300 = 10,

        S400 = 20,

        S1200 = 30,

        S1300 = 40,

        S1500 = 50,
    }
}
