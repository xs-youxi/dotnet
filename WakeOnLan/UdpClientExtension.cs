using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace WakeOnLan
{
    public static class UdpClientExtension
    {
        public static async Task BroadcastPacketAsync(this IEnumerable<byte> packet, int port, IPAddress bcAddress = null)
        {
            var sendBytes = packet.ToArray();
            try
            {
                var endPoint = new IPEndPoint(bcAddress ?? IPAddress.Broadcast, port);
                await new UdpClient().SendAsync(sendBytes, sendBytes.Length, endPoint);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public static IPAddress GetLocalAddress()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("192.168.100.200", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address;
            }
        }
        public static bool IsNetworkAvailable() => NetworkInterface.GetIsNetworkAvailable();
        public static IPHostEntry GetHostEntry() => Dns.GetHostEntry(Dns.GetHostName());
        public static IEnumerable<IPInterfaceProperties> GetAllNetworkInterfaces(NetworkInterfaceType interfaceType) => NetworkInterface.GetAllNetworkInterfaces()
                .Where(d => d.NetworkInterfaceType == interfaceType)
                .Where(e => e.OperationalStatus == OperationalStatus.Up)
                .Select(f => f.GetIPProperties());
        public static IEnumerable<UnicastIPAddressInformation> GetAllAddressInfo(this IEnumerable<IPInterfaceProperties> interfaces)
        {
            return interfaces.SelectMany(x =>
                x.UnicastAddresses
                      .Where(q => q.Address.AddressFamily == AddressFamily.InterNetwork)
                      .Where(r => r.IsDnsEligible == true)
                      .Select(s => s));
        }
        public static UnicastIPAddressInformation GetAddressInfo(this IEnumerable<IPInterfaceProperties> interfaces, IPAddress address)
        {
            return interfaces.GetAllAddressInfo()
                .Where(k => address.Equals(k.Address))
                .FirstOrDefault();
        }
    }
}
