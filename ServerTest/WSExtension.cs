using Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerTest
{
    public static class WebSocketExtension
    {
        public static async Task Send(this IEnumerable<WebSocket> sockets, string recvBuffer)
        {
            foreach (var s in sockets)
                await s.Send(recvBuffer);
        }
        public static async Task Send(this IEnumerable<WebSocket> sockets, byte[] recvBuffer)
        {
            foreach( var s in sockets)
                await s.Send(recvBuffer);
        }
        public static async Task Send(this IEnumerable<WebSocket> sockets, MainPacket packet)
        {
            Console.WriteLine($"\t>>>>>>> {packet.packetType}");
            var sendText = JsonHelper.SerializeToClient(packet);
            foreach (var ws in sockets)
                await ws.Send(sendText);
        }
        public static async Task Send(this WebSocket ws, MainPacket packet)
        {
            Console.WriteLine($"\t>>>>>>> {packet.packetType}");
            var sendText = JsonHelper.SerializeToClient(packet);
            await ws.Send(sendText);
        }
        public static async Task Send(this WebSocket ws, string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            var sendBuffer = new ArraySegment<byte>(buffer, 0, buffer.Count());
            await ws.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        public static async Task Send(this WebSocket ws, byte[] buffer)
        {
            var sendBuffer = new ArraySegment<byte>(buffer, 0, buffer.Count());
            await ws.SendAsync(sendBuffer, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }
}
