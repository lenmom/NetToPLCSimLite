﻿using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ServiceProcess;

using log4net;

namespace NetToPLCSimLite.Helpers
{
    internal class S7ServiceHelper
    {
        #region Field

        private readonly ILog log = LogExt.log;
        private TcpListener tcp;

        #endregion

        #region Public Method

        /// <summary>
        /// Find s7 service which is installed according to the installation of TIA with service name: s7oiehsx/s7oiehsx64.
        /// this operation requires administrator's priviledge.
        /// </summary>
        /// <returns><c>ServiceController</c> of service s7oiehsx/s7oiehsx64.</returns>
        internal ServiceController FindS7Service()
        {
            ServiceController[] services = ServiceController.GetServices();
            ServiceController s7svc = services.FirstOrDefault(x => x.ServiceName == "s7oiehsx" || x.ServiceName == "s7oiehsx64");

            if (s7svc == null)
            {
                log.Warn("NG, Can not find S7 online service.");
            }
            else
            {
                log.Info("OK, Find S7 online service.");
            }

            return s7svc;
        }

        internal bool StartService(ServiceController svc, int timeout)
        {
            if (svc == null)
            {
                return false;
            }

            if (svc.Status == ServiceControllerStatus.Running)
            {
                return true;
            }

            svc.Start();
            svc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(timeout));
            svc.Refresh();

            if (svc.Status == ServiceControllerStatus.Running)
            {
                log.Info("OK, Start S7 online service.");
                return true;
            }
            else
            {
                log.Warn("NG, Can not start S7 online service.");
                return false;
            }
        }

        internal bool StopService(ServiceController svc, int timeout)
        {
            if (svc == null)
            {
                return false;
            }

            if (svc.Status == ServiceControllerStatus.Stopped)
            {
                return true;
            }

            svc.Stop();
            svc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(timeout));
            svc.Refresh();

            if (svc.Status == ServiceControllerStatus.Stopped)
            {
                log.Info("OK, Stop S7 online service.");
                return true;
            }
            else
            {
                log.Warn("NG, Can not stop S7 online service.");
                return false;
            }
        }

        internal bool StartTcpServer(int port)
        {
            try
            {
                tcp = new TcpListener(IPAddress.Any, port);
                tcp.Start();
                if (tcp.Server.IsBound)
                {
                    log.Info("OK, Start TCP Server.");
                    return true;
                }
                else
                {
                    log.Warn("NG, Start TCP Server.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.Error(nameof(StartTcpServer), ex);
                StopTcpServer();
                return false;
            }
        }

        internal bool StopTcpServer()
        {
            try
            {
                if (tcp != null)
                {
                    tcp.Stop();
                }

                if (!tcp.Server.IsBound)
                {
                    log.Info("OK, Stop TCP Server.");
                    return true;
                }
                else
                {
                    log.Warn("NG, Stop TCP Server.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.Error(nameof(StartTcpServer), ex);
                return false;
            }
        }

        internal bool IsPortAvailable(int port)
        {
            bool isAvailable = true;

            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endpoint in tcpConnInfoArray)
            {
                if (endpoint.Port == port)
                {
                    isAvailable = false;
                    break;
                }
            }

            if (isAvailable)
            {
                log.Info($"OK, Port({port}) avilable.");
                return true;
            }
            else
            {
                log.Warn($"NG, Port({port}) avilable.");
                return false;
            }
        }

        #endregion
    }
}
