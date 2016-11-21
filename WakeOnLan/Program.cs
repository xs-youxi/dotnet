using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace WakeOnLan
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() < 1)
            {
                Console.WriteLine("WakeOnLan [mac-address] [mac-address] ...");
                return;
            }

            var address = UdpClientExtension.GetAllNetworkInterfaces(NetworkInterfaceType.Ethernet)
                .GetAddressInfo(UdpClientExtension.GetLocalAddress());
            var broadcastAddress = address.Address.GetAddressBytes()
                                .Zip(address.IPv4Mask.GetAddressBytes(), (p, q) => (byte)(p | (byte)~q))
                                .ToArray();
            var bcAddress = new IPAddress(broadcastAddress);
            Console.WriteLine($"Local {address.Address}， Broadcast {bcAddress}");

            args.WakeOnLan(bcAddress);
        }
    }
}
