using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SnapSearch.Application.Common.Helpers
{
    public static class NetworkHelper
    {
        #region Public Methods

        public static string GetLocalIpAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        public static string GetMacAddress()
        {
            try
            {
                var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(nic =>
                        nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                return networkInterface?.GetPhysicalAddress().ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        #endregion Public Methods
    }
}