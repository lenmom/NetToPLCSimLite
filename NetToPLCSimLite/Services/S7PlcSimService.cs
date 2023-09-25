using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

using ConsoleTableExt;

using IsoOnTcp;

using log4net;

using NetToPLCSimLite.Helpers;
using NetToPLCSimLite.Models;

namespace NetToPLCSimLite.Services
{
    public class S7PlcSimService : IDisposable
    {
        #region Field

        private readonly ILog log = LogExt.log;
        private readonly ConcurrentDictionary<string, IsoToS7online> s7ServerList =
                                        new ConcurrentDictionary<string, IsoToS7online>();
        private readonly List<IS7Protocol> m_PlcSimList = new List<IS7Protocol>();

        #endregion

        #region Property

        public List<IS7Protocol> PlcSimList
        {
            get
            {
                return m_PlcSimList;
            }
        }

        #endregion

        #region Event

        public event EventHandler<string> OnPlcSimError;

        #endregion

        #region Public Method

        public bool GetS7Port()
        {
            bool ret = false;
            try
            {
                log.Info($"START, Get S7 Port.");
                S7ServiceHelper service = new S7ServiceHelper();
                System.ServiceProcess.ServiceController s7svc = service.FindS7Service();
                if (s7svc == null)
                {

                    return false;
                }

                ret = service.IsPortAvailable(CONST.S7_PORT);
                if (!ret)
                {
                    service.StopService(s7svc, CONST.SVC_TIMEOUT);
                    Thread.Sleep(100);

                    service.StartTcpServer(CONST.S7_PORT);
                    Thread.Sleep(100);

                    if (!service.StartService(s7svc, CONST.SVC_TIMEOUT))
                    {
                        service.StopTcpServer();
                        return false;
                    }
                    Thread.Sleep(100);

                    service.StopTcpServer();
                    Thread.Sleep(100);

                    ret = service.IsPortAvailable(CONST.S7_PORT);
                }
            }
            catch (Exception)
            {
                ret = false;
                throw;
            }
            finally
            {
                if (ret)
                {
                    log.Info($"OK, Get S7 Port.");
                }
                else
                {
                    log.Warn($"NG, Get S7 Port.");
                }
            }
            return ret;
        }

        public List<IS7Protocol> SetStation(IReadOnlyCollection<IS7Protocol> list)
        {
            try
            {
                log.Debug($"=== Received S7 PLCSim List ===");
                List<IS7Protocol> adding = new List<IS7Protocol>();
                List<IS7Protocol> original = new List<IS7Protocol>();
                if (list != null)
                {
                    foreach (IS7Protocol plc in list)
                    {
                        original.Add(plc);
                        IS7Protocol exist = PlcSimList.FirstOrDefault(x => x.Ip == plc.Ip);
                        if (exist == null)
                        {
                            adding.Add(plc);
                        }

                        log.Debug(plc.ToString());
                    }
                }
                log.Debug("==============================");

                log.Debug($"=== Before S7 PLCSim List ===");
                List<IS7Protocol> removing = new List<IS7Protocol>();
                foreach (IS7Protocol plc in PlcSimList)
                {
                    IS7Protocol exist = original.FirstOrDefault(x => x.Ip == plc.Ip);
                    if (exist == null)
                    {
                        removing.Add(plc);
                    }

                    log.Debug(plc.ToString());
                }
                log.Debug("==============================");

                if (adding.Count > 0)
                {
                    List<IS7Protocol> ret = AddStation(adding);
                    PlcSimList.AddRange(ret);
                }

                if (removing.Count > 0)
                {
                    List<IS7Protocol> ret = RemoveStation(removing);
                    for (int i = 0; i < ret.Count; i++)
                    {
                        IS7Protocol item = ret[i];
                        PlcSimList.Remove(item);
                        item = null;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                log.Debug($"=== After S7 PLCSim List ===");
                foreach (IS7Protocol item in PlcSimList)
                {
                    log.Debug(item.ToString());
                }
                log.Debug("==============================");
            }

            return PlcSimList;
        }

        public override string ToString()
        {
            try
            {
                lock (PlcSimList)
                {
                    StringBuilder sb = new StringBuilder($"{Environment.NewLine}CURRENT [PLCSIM] LIST ( {PlcSimList.Count} ):{Environment.NewLine}");
                    if (PlcSimList.Count > 0)
                    {
                        sb.Append(ConsoleTableBuilder.From(PlcSimList).WithFormat(ConsoleTableBuilderFormat.MarkDown).Export().ToString());
                    }
                    return sb.AppendLine().ToString();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region Private Method

        private List<IS7Protocol> AddStation(IReadOnlyCollection<IS7Protocol> adding)
        {
            List<IS7Protocol> ret = new List<IS7Protocol>();
            if (adding == null)
            {
                return ret;
            }

            try
            {
                log.Info($"=== Adding S7 PLCSim List ===");
                foreach (IS7Protocol item in adding)
                {
                    List<byte[]> tsaps = new List<byte[]>();
                    byte tsap2 = (byte)((item.Rack << 4) | item.Slot);
                    tsaps.Add(new byte[] { 0x01, tsap2 });
                    tsaps.Add(new byte[] { 0x02, tsap2 });
                    tsaps.Add(new byte[] { 0x03, tsap2 });

                    IsoToS7online isoToS7onlineService = null;
                    try
                    {
                        isoToS7onlineService = new IsoToS7online(false);
                        IPAddress ip = IPAddress.Parse(item.Ip);
                        string err = string.Empty;
                        bool srvStart = isoToS7onlineService.Start(item.Name, ip, tsaps, ip, item.Rack, item.Slot, ref err);
                        if (srvStart)
                        {
                            bool conn = item.Connect();
                            if (conn)
                            {
                                isoToS7onlineService.DataReceived = item.DataReceived;
                                s7ServerList.TryAdd(item.Ip, isoToS7onlineService);

                                ret.Add(item);
                                //item.OnError += new Action<string>((ipp) => OnS7ProtocalError_Handler(ipp));
                                item.OnError += OnS7ProtocalError_Handler;
                                log.Info($"OK, {item.ToString()}");
                            }
                            else
                            {
                                isoToS7onlineService?.Dispose();
                                isoToS7onlineService = null;
                                item?.Dispose();
                                log.Warn($"NG, {item.ToString()}");
                            }
                        }
                        else
                        {
                            log.Warn($"NG({err}), {item.ToString()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        isoToS7onlineService?.Dispose();
                        isoToS7onlineService = null;
                        item?.Dispose();
                        log.Error($"ERR, {item.ToString()}", ex);
                    }
                }
                log.Debug("==============================");
            }
            catch (Exception)
            {
                throw;
            }
            return ret;
        }

        private List<IS7Protocol> RemoveStation(IReadOnlyCollection<IS7Protocol> removing)
        {
            List<IS7Protocol> ret = new List<IS7Protocol>();
            if (removing == null)
            {
                return ret;
            }

            try
            {
                log.Info($"=== Removing S7 PLCSim List ===");
                foreach (IS7Protocol item in removing)
                {
                    try
                    {
                        if (s7ServerList.TryRemove(item.Ip, out IsoToS7online srv))
                        {
                            srv?.Dispose();
                            srv = null;
                        }

                        if (item != null)
                        {
                            item.OnError -= OnS7ProtocalError_Handler;
                            item.Dispose();

                            ret.Add(item);
                            log.Info($"OK, {item.ToString()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"ERR, {item.ToString()}", ex);
                    }
                }
                log.Debug("==============================");
            }
            catch (Exception)
            {
                throw;
            }
            return ret;
        }

        #endregion

        #region Event Handler

        private void OnS7ProtocalError_Handler(string ip)
        {
            // Return
            if (string.IsNullOrEmpty(ip))
            {
                return;
            }

            lock (PlcSimList)
            {
                IS7Protocol protocolOnError = PlcSimList.FirstOrDefault(x => !x.IsConnected && x.Ip == ip);
                if (protocolOnError != null && s7ServerList.TryRemove(protocolOnError.Ip, out IsoToS7online isoToS7onlineSvc))
                {
                    isoToS7onlineSvc?.Dispose();
                    isoToS7onlineSvc = null;
                }

                if (protocolOnError != null)
                {
                    protocolOnError.Dispose();
                    PlcSimList.Remove(protocolOnError);
                    log.Info($"STOPPED, {protocolOnError.ToString()}");
                    OnPlcSimError?.Invoke(this, protocolOnError.Ip);
                    protocolOnError = null;
                }
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
                    SetStation(null);
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
