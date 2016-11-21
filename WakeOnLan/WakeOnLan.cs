using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace WakeOnLan
{
    public static class WakeOnLanExtension
    {
        public static void WakeOnLan(this string[] macAddresses, IPAddress bcAddress = null)
        {
            macAddresses.ToList().ForEach(async x => await x.WakeOnLanAsync(bcAddress));
        }
        public static async Task WakeOnLanAsync(this string macAddress, IPAddress bcAddress = null)
        {
            var macBytes = macAddress
                .Split(new[] { ':', '-' })
                .Select(x => Convert.ToByte(x, 16));

            var magicPacket = magicPacketHeader.Concat(
                Enumerable.Range(1, 16).Select(x => macBytes)
                        .Aggregate((a, b) => a.Concat(b)));

            await magicPacket.BroadcastPacketAsync(3, bcAddress);
            Console.WriteLine($"Send magic packet to {macAddress}");
        }
        private readonly static IEnumerable<byte> magicPacketHeader = Enumerable.Repeat<byte>(0xff, 6);
    }
}
