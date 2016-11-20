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
        public static async Task BroadcastPacketAsync(this IEnumerable<byte> packet, int port, string address = "255.255.255.255")
        {
            var sendBytes = packet.ToArray();
            try
            {
                //IPAddress localAddress = GetLocalAddress();
                //IPAddress mask = IPAddress.Parse("255.255.255.0");
                //var broadCastAddress = new IPAddress((localAddress.ScopeId | ~mask.ScopeId) & 0x0ffffffff);

                await new UdpClient().SendAsync(sendBytes, sendBytes.Length, address /* broadCastAddress.ToString() */, port);
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
        public static List<IPAddress> GetAddress(this IEnumerable<IPInterfaceProperties> interfaces)
        {
            var addressList = new List<IPAddress>();
            interfaces.ToList().ForEach(x =>
            {
                addressList.AddRange(x.UnicastAddresses
                      .Where(q => q.Address.AddressFamily == AddressFamily.InterNetwork)
                      .Where(r => r.IsDnsEligible == true)
                      .Select(s => s.Address));
            });
            return addressList;
        }
    }
}
