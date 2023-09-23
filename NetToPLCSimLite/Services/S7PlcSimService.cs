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
        public event EventHandler<string> PlcSimErr;

        private readonly ILog log = LogExt.log;
        private readonly ConcurrentDictionary<string, IsoToS7online> s7ServerList = new ConcurrentDictionary<string, IsoToS7online>();

        #endregion

        #region Property

        public List<S7Protocol> PlcSimList { get; } = new List<S7Protocol>();
        
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
                    throw new NullReferenceException();
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

        public List<S7Protocol> SetStation(IReadOnlyCollection<S7Protocol> list)
        {
            try
            {
                log.Debug($"=== Received S7 PLCSim List ===");
                List<S7Protocol> adding = new List<S7Protocol>();
                List<S7Protocol> original = new List<S7Protocol>();
                if (list != null)
                {
                    foreach (S7Protocol plc in list)
                    {
                        original.Add(plc);
                        S7Protocol exist = PlcSimList.FirstOrDefault(x => x.Ip == plc.Ip);
                        if (exist == null)
                        {
                            adding.Add(plc);
                        }

                        log.Debug(plc.ToString());
                    }
                }
                log.Debug("==============================");

                log.Debug($"=== Before S7 PLCSim List ===");
                List<S7Protocol> removing = new List<S7Protocol>();
                foreach (S7Protocol plc in PlcSimList)
                {
                    S7Protocol exist = original.FirstOrDefault(x => x.Ip == plc.Ip);
                    if (exist == null)
                    {
                        removing.Add(plc);
                    }

                    log.Debug(plc.ToString());
                }
                log.Debug("==============================");

                if (adding.Count > 0)
                {
                    List<S7Protocol> ret = AddStation(adding);
                    PlcSimList.AddRange(ret);
                }

                if (removing.Count > 0)
                {
                    List<S7Protocol> ret = RemoveStation(removing);
                    for (int i = 0; i < ret.Count; i++)
                    {
                        S7Protocol item = ret[i];
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
                foreach (S7Protocol item in PlcSimList)
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

        private List<S7Protocol> AddStation(IReadOnlyCollection<S7Protocol> adding)
        {
            List<S7Protocol> ret = new List<S7Protocol>();
            if (adding == null)
            {
                return ret;
            }

            try
            {
                log.Info($"=== Adding S7 PLCSim List ===");
                foreach (S7Protocol item in adding)
                {
                    List<byte[]> tsaps = new List<byte[]>();
                    byte tsap2 = (byte)((item.Rack << 4) | item.Slot);
                    tsaps.Add(new byte[] { 0x01, tsap2 });
                    tsaps.Add(new byte[] { 0x02, tsap2 });
                    tsaps.Add(new byte[] { 0x03, tsap2 });

                    IsoToS7online srv = null;
                    try
                    {
                        srv = new IsoToS7online(false);
                        IPAddress ip = IPAddress.Parse(item.Ip);
                        string err = string.Empty;
                        bool srvStart = srv.start(item.Name, ip, tsaps, ip, item.Rack, item.Slot, ref err);
                        if (srvStart)
                        {
                            bool conn = item.Connect();
                            if (conn)
                            {
                                srv.DataReceived = item.DataReceived;
                                s7ServerList.TryAdd(item.Ip, srv);

                                ret.Add(item);
                                item.ErrorHandler = new Action<string>((ipp) => ErrorHandler(ipp));

                                log.Info($"OK, {item.ToString()}");
                            }
                            else
                            {
                                srv?.Dispose();
                                srv = null;
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
                        srv?.Dispose();
                        srv = null;
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

        private List<S7Protocol> RemoveStation(IReadOnlyCollection<S7Protocol> removing)
        {
            List<S7Protocol> ret = new List<S7Protocol>();
            if (removing == null)
            {
                return ret;
            }

            try
            {
                log.Info($"=== Removing S7 PLCSim List ===");
                foreach (S7Protocol item in removing)
                {
                    try
                    {
                        if (s7ServerList.TryRemove(item.Ip, out IsoToS7online srv))
                        {
                            srv?.Dispose();
                            srv = null;
                        }
                        item?.Dispose();

                        ret.Add(item);
                        log.Info($"OK, {item.ToString()}");
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

        private void ErrorHandler(string ip)
        {
            // Return
            if (string.IsNullOrEmpty(ip))
            {
                return;
            }

            lock (PlcSimList)
            {
                S7Protocol error = PlcSimList.FirstOrDefault(x => !x.IsConnected && x.Ip == ip);
                if (error != null && s7ServerList.TryRemove(error.Ip, out IsoToS7online srv))
                {
                    srv?.Dispose();
                    srv = null;
                }
                error?.Dispose();
                PlcSimList.Remove(error);
                log.Info($"STOPPED, {error.ToString()}");
                PlcSimErr?.Invoke(this, error.Ip);
                error = null;
            }
        }
        
        #endregion

        #region IDisposable Support

        private bool disposedValue = false; // 중복 호출을 검색하려면

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리되는 상태(관리되는 개체)를 삭제합니다.
                    SetStation(null);
                }

                // TODO: 관리되지 않는 리소스(관리되지 않는 개체)를 해제하고 아래의 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.

                disposedValue = true;
            }
        }

        // TODO: 위의 Dispose(bool disposing)에 관리되지 않는 리소스를 해제하는 코드가 포함되어 있는 경우에만 종료자를 재정의합니다.
        // ~S7PlcSimService() {
        //   // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
        //   Dispose(false);
        // }

        // 삭제 가능한 패턴을 올바르게 구현하기 위해 추가된 코드입니다.
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
