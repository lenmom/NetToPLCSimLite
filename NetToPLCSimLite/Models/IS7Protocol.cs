using System;
using System.Net;
using System.Timers;

using log4net;

using S7PROSIMLib;

namespace NetToPLCSimLite.Models
{
    public interface IS7Protocol : IDisposable
    {
        event Action<string> OnError;

        string Name { get; set; }

        string Ip { get; set; }

        bool IsConnected { get; set; }

        StationCpu Cpu { get; set; }

        /// <summary>
        ///  S300/s400/S1200/S1500,0.
        /// </summary>
        int Rack { get; set; }

        /// <summary>
        /// S300/s400,2;
        /// S1200/S1500,1.
        /// </summary>
        int Slot { get; set; }

        string PlcPath { get; set; }

        int Instance { get; set; }

        bool Connect();

        void Disconnect();

        void DataReceived(byte[] data);
    }
}
