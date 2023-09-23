using System;
using System.Net;
using System.Timers;

using log4net;

using S7PROSIMLib;

namespace NetToPLCSimLite.Models
{
    public class S7Protocol : IDisposable
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

        internal Action<string> OnError;

        #endregion

        #region Property

        internal string Name { get; set; } = string.Empty;

        internal string Ip { get; set; } = string.Empty;

        internal bool IsConnected { get; set; } = false;

        internal StationCpu Cpu { get; set; } = StationCpu.S400;

        /// <summary>
        ///  S300/s400/S1200/S1500,0.
        /// </summary>
        internal int Rack { get; set; } = 0;

        /// <summary>
        /// S300/s400,2;
        /// S1200/S1500,1.
        /// </summary>
        internal int Slot { get; set; } = 3;

        internal string PlcPath { get; set; } = string.Empty;

        internal int Instance { get; set; } = -1;

        #endregion

        #region Nested Object

        internal enum StationCpu
        {
            S200 = 0,

            S300 = 10,

            S400 = 20,

            S1200 = 30,

            S1300 = 40,

            S1500 = 50,
        }

        #endregion

        #region Public Method

        public override string ToString()
        {
            return $"Name:{Name}, IP:{Ip}, Connected:{IsConnected}, Instance:{Instance}";
        }

        internal bool Connect()
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
                string st = s7proSim.GetState();
                IsConnected = st == "RUN_P" ? true : false;

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

        internal void Disconnect()
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

        internal void DataReceived(byte[] data)
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
                    // TODO: 관리되는 상태(관리되는 개체)를 삭제합니다.
                    Disconnect();
                    s7proSim = null;
                }

                // TODO: 관리되지 않는 리소스(관리되지 않는 개체)를 해제하고 아래의 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(true);
            // TODO: 위의 종료자가 재정의된 경우 다음 코드 줄의 주석 처리를 제거합니다.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
