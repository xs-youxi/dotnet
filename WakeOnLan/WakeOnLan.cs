using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WakeOnLan
{
    public static class WakeOnLanExtension
    {
        public static void WakeOnLan(this string[] macAddresses)
        {
            macAddresses.ToList().ForEach(async x => await x.WakeOnLanAsync());
        }
        public static async Task WakeOnLanAsync(this string macAddress)
        {
            var macBytes = macAddress
                .Split(new[] { ':', '-' })
                .Select(x => Convert.ToByte(x, 16));

            var magicPacket = magicPacketHeader.Concat(
                Enumerable.Range(1, 16).Select(x => macBytes)
                        .Aggregate((a, b) => a.Concat(b)));

            await magicPacket.BroadcastPacketAsync(3);
            Console.WriteLine($"Send magic packet to {macAddress}");
        }
        private readonly static IEnumerable<byte> magicPacketHeader = Enumerable.Repeat<byte>(0xff, 6);
    }
}
