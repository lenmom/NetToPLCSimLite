using System;
using System.Net;
using System.Timers;

using log4net;

using S7PROSIMLib;

namespace NetToPLCSimLite.Models
{
    public class S7Protocol : IS7Protocol
    {
        #region Const

        private const byte S7COMM_TRANSPORT_SIZE_BIT = 1;
        private const byte S7COMM_TRANSPORT_SIZE_BYTE = 2;

        #endregion

        #region Field

        private readonly ILog log = LogExt.log;
        private S7ProSim s7proSim = new S7ProSim();
        private readonly Timer timer = new Timer();

        #endregion

        #region Event

        public event Action<string> OnError;

        #endregion

        #region Property

        public string Name { get; set; } = string.Empty;

        public string Ip { get; set; } = string.Empty;

        public bool IsConnected { get; set; } = false;

        public StationCpu Cpu { get; set; } = StationCpu.S400;

        /// <summary>
        ///  S300/s400/S1200/S1500,0.
        /// </summary>
        public int Rack { get; set; } = 0;

        /// <summary>
        /// S300/s400,2;
        /// S1200/S1500,1.
        /// </summary>
        public int Slot { get; set; } = 3;

        public string PlcPath { get; set; } = string.Empty;

        public int Instance { get; set; } = -1;

        #endregion

        #region Public Method

        public override string ToString()
        {
            return $"Name:{Name}, IP:{Ip}, Connected:{IsConnected}, Instance:{Instance}";
        }

        public bool Connect()
        {
            try
            {
                if (Instance < 1 || Instance > 8)
                {
                    return false;
                }

                Disconnect();

                s7proSim.ConnectionError -= PlcSim_ConnectionError;
                s7proSim.ConnectionError += PlcSim_ConnectionError;
                s7proSim.ConnectExt(Instance);
                s7proSim.SetState("RUN_P");
                string state = s7proSim.GetState();
                IsConnected = state == "RUN_P" ? true : false;

                if (IsConnected)
                {
                    s7proSim.SetScanMode(ScanModeConstants.ContinuousScan);
                    timer.Elapsed -= Timer_Elapsed;
                    timer.Elapsed += Timer_Elapsed;
                    timer.Interval = 1000;
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                log.Error($"ERR, Name:{Name}, IP:{Ip}, INS:{PlcPath}", ex);
                Disconnect();
            }

            return IsConnected;
        }

        public void Disconnect()
        {
            try
            {
                timer.Elapsed -= Timer_Elapsed;
                timer.Stop();

                if (IsConnected)
                {
                    s7proSim.ConnectionError -= PlcSim_ConnectionError;
                    s7proSim.Disconnect();
                    log.Info($"DISCONNECTED, Name:{Name}, IP:{Ip}, INS:{Instance}");
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                IsConnected = false;
            }
        }

        public void DataReceived(byte[] data)
        {
            if (!IsConnected || data == null)
            {
                return;
            }

            try
            {
                // INPUT, WRITE
                int lenth = data.Length - 28;
                if (lenth > 0 && data[0] == 0x32 && data[1] == 0x01 && data[10] == 0x05 && data[14] == 0x10 && data[20] == 0x81)
                {
                    // address
                    byte[] aa = new byte[4] { 0, data[21], data[22], data[23] };
                    int addr = BitConverter.ToInt32(aa, 0);
                    addr = BitConverter.IsLittleEndian ? IPAddress.HostToNetworkOrder(addr) : addr;
                    int bytepos = addr / 8;
                    int bitpos = addr % 8;

                    byte t_size = data[15];
                    short len = BitConverter.ToInt16(data, 16);
                    len = BitConverter.IsLittleEndian ? IPAddress.HostToNetworkOrder(len) : len;

                    if (t_size == S7COMM_TRANSPORT_SIZE_BIT)
                    {
                        bool set = data[28] == 0 ? false : true;
                        s7proSim.WriteInputPoint(bytepos, bitpos, set);
                    }
                    else if (t_size == S7COMM_TRANSPORT_SIZE_BYTE)
                    {
                        object set = new byte[len];
                        Array.Copy(data, 28, (byte[])set, 0, len);
                        s7proSim.WriteInputImage(bytepos, ref set);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(nameof(DataReceived), ex);
                Disconnect();
            }
        }

        #endregion

        #region Event Handler

        private void PlcSim_ConnectionError(string ControlEngine, int Error)
        {
            try
            {
                log.Error($"PROSIM ERROR({Error}), Name:{Name}, IP:{Ip}, INS:{PlcPath}");
                Disconnect();
                OnError?.Invoke(Ip);
            }
            catch (Exception ex)
            {
                log.Error(nameof(PlcSim_ConnectionError), ex);
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                bool st = s7proSim?.GetState() == "RUN_P" ? true : false;
                if (!st)
                {
                    s7proSim?.SetState("RUN_P");
                    s7proSim?.SetScanMode(ScanModeConstants.ContinuousScan);
                }
            }
            catch (Exception ex)
            {
                timer.Elapsed -= Timer_Elapsed;
                timer.Stop();
                log.Error(nameof(Timer_Elapsed), ex);
            }
        }

        #endregion

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disconnect();
                    s7proSim = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
