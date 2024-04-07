using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace msmon
{
    internal class Tools
    {
        public static bool IsTcpPortAvailable(int port)
        {
            bool isAvailable = true;

            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            System.Net.IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            foreach (System.Net.IPEndPoint endpoint in tcpConnInfoArray)
            {
                if (endpoint.Port == port)
                {
                    isAvailable = false;
                    break;
                }
            }

            return isAvailable;
        }

        public static bool IsValidIP(string addr)
        {
            const string pattern = @"^(([01]?\d\d?|2[0-4]\d|25[0-5])\.){3}([01]?\d\d?|25[0-5]|2[0-4]\d)$";
            Regex check = new Regex(pattern);
            bool valid = false;
            if (addr == string.Empty)
            {
                valid = false;
            }
            else
            {
                valid = check.IsMatch(addr, 0);
            }

            return valid;
        }

        public static List<string> GetLocalIpAddressList()
        {
            List<string> result = new List<string>();

            NetworkInterface[] intf = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface device in intf)
            {
                if ((device.NetworkInterfaceType.Equals(NetworkInterfaceType.Ethernet) ||
                     device.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                     device.NetworkInterfaceType == NetworkInterfaceType.Loopback) &&
                    device.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation addressInformation in device.GetIPProperties().UnicastAddresses)
                    {
                        if (addressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            result.Add(addressInformation.Address.ToString());
                        }
                    }
                }
            }

            return result;
        }

        public static List<string> GetPlcsimIpAddressList()
        {
            List<string> result = new List<string>();

            IsoOnTcp.PlcsimS7online.PlcS7onlineMsgPumpS7 s7o =
                new IsoOnTcp.PlcsimS7online.PlcS7onlineMsgPumpS7(IPAddress.None, 0, 0);
            try
            {

                List<string> partners;
                int n = 0;

                partners = s7o.ListReachablePartners();
                foreach (string p in partners)
                {
                    n++;
                    result.Add(p);
                }

                if (n == 0)
                {
                    //MessageBox.Show("No TCP/IP reachable PLC detected." + Environment.NewLine +
                    //                "Please check if Plcsim is running.", "Info", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                    //ChosenIPaddress = "";
                    //DialogResult = DialogResult.Cancel;
                    //Close();
                    LoggerManager.Error("No TCP/IP reachable PLC detected.");
                }
            }
            catch
            {
                //MessageBox.Show("Error when connecting S7online interface!", "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                //ChosenIPaddress = "";
                //DialogResult = DialogResult.Cancel;
                //Close();

                LoggerManager.Error("Error when connecting S7online interface");
            }
            finally
            {
                s7o.Close();
            }

            return result;
        }
    }
}