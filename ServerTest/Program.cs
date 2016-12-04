using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Protocol;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;

namespace ServerTest
{
    [System.Runtime.InteropServices.Guid("90B67B8F-AA63-4CBA-B271-57302F600691")]
    class Program
    {
        static void Main(string[] args)
        {
            var addr = "http://127.0.0.1:12345/";

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(addr);
            listener.Start();
            Console.WriteLine($"Listen on {addr}");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                if (context.Request.IsWebSocketRequest)
                {
                    Console.WriteLine($"Accepted");
                    OnAccept(context);
                }
                else
                {
                    context.Response.Close();
                }
            }
        }

        private static ConcurrentDictionary<string, PlayerInfo> _playerMap = new ConcurrentDictionary<string, PlayerInfo>();
        private static ConcurrentDictionary<int, RoomInfo> _roomMap = new ConcurrentDictionary<int, RoomInfo>();

        public class PlayerInfo
        {
            public WebSocket webSocket { get; set; }
            public string playerId { get; set; }
            public string deviceId { get; set; } = "";
            public string playerName { get; set; } = "";
            public int roomId { get; set; } = 0;
        }
        public class RoomInfo
        {
            public int roomId { get; set; }
            public Dictionary<string, bool> playerList = new Dictionary<string, bool>();
        }
        private static void OnAccept(HttpListenerContext context)
        {
            var task = Task.Run(async () =>
            {
                var wsContext = await context.AcceptWebSocketAsync(null);
                var webSocket = wsContext.WebSocket;

                var playerInfo = new PlayerInfo
                {
                    webSocket = webSocket,
                    playerId = webSocket.GetHashCode().ToString(),
                };
                var playerId = webSocket.GetHashCode().ToString();
                _playerMap.TryAdd(playerId, new PlayerInfo { webSocket = webSocket, playerId = playerId });
                while (webSocket.State == WebSocketState.Open)
                {
                    var buffer = WebSocket.CreateServerBuffer(4096);
                    WebSocketReceiveResult received = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    if (received.MessageType == WebSocketMessageType.Close)
                        break;
                    var ms = new MemoryStream(buffer.Array, buffer.Offset, received.Count, true);
                    while (false == received.EndOfMessage)
                    {
                        received = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                        ms.WriteAsync(buffer.Array, buffer.Offset, received.Count).Wait();
                    }
                    switch (received.MessageType)
                    {
                        case WebSocketMessageType.Binary:
                            await OnBinary(playerInfo.playerId, ms.ToArray());
                            break;
                        case WebSocketMessageType.Text:
                            var json = Encoding.UTF8.GetString(ms.ToArray());
                            await OnText(playerInfo.playerId, json);
                            break;
                    }
                }
                Console.WriteLine("Closed");

                PlayerInfo temp;
                _playerMap.TryRemove(playerId, out temp);

                //await webSocket.CloseAsync(received.CloseStatus.Value, received.CloseStatusDescription, CancellationToken.None);
                webSocket.Dispose();
            });
        }
        private static async Task OnBinary(string playerId, byte[] recvBuffer)
        {
            await _playerMap.Values.Select(p => p.webSocket).ToList().Send(recvBuffer);
        }
        private static MainPacket MakePacket(int uid, object value)
        {
            var packetName = value.GetType().Name.Substring(6);
            var packetId = (ePacketType)Enum.Parse(typeof(ePacketType), packetName);
            var packet = new MainPacket(packetId);
            packet.SetUID(uid);
            ((FieldInfo)(typeof(MainPacket).GetMember($"packet{packetName}").First()))
                .SetValue(packet, value);
            return packet;
        }
        private static async Task OnText(string playerId, string json)
        {
            PlayerInfo playerInfo;
            if (false == _playerMap.TryGetValue(playerId, out playerInfo))
                return;
            var recved = JsonHelper.Deserialize<MainPacket>(json);
            Console.WriteLine($"{recved.packetType} {playerInfo.playerId} {playerInfo.roomId}");
            switch (recved.packetType)
            {
                case ePacketType.Login:
                    playerInfo.playerName = recved.packetLogin.playerName;
                    playerInfo.deviceId = recved.packetLogin.deviceID;

                    await playerInfo.webSocket.Send(MakePacket(recved.UID, new PacketLoginRes
                    {
                        playerId = playerInfo.playerId
                    }));
                    break;
                case ePacketType.QuickStart:
                    var availableRoom = _playerMap.Values.Where(p => p.roomId != 0).FirstOrDefault();
                    if (availableRoom != null)
                    {
                        playerInfo.roomId = availableRoom.roomId;
                    }
                    else
                    {
                        var rnd = new Random(DateTime.Now.Millisecond);
                        playerInfo.roomId = rnd.Next(9999);
                        _roomMap.TryAdd(playerInfo.roomId, new RoomInfo { roomId = playerInfo.roomId, });
                    }
                    {
                        RoomInfo ri;
                        if (_roomMap.TryGetValue(playerInfo.roomId, out ri))
                        {
                            var roomPlayers = _playerMap.Values.Where(p => p.roomId == playerInfo.roomId);
                            await roomPlayers.Select(p => p.webSocket).Send(MakePacket(recved.UID, new PacketEnterRoomRes
                            {
                                playerId = playerInfo.playerId,
                                roomId = playerInfo.roomId,
                                standByList = roomPlayers.Select(q => q.playerId).ToArray(),
                                playerList = ri.playerList.Keys.ToArray(),
                            }));
                        }
                    }
                    break;
                case ePacketType.LeaveRoom:
                    {
                        RoomInfo ri;
                        if (_roomMap.TryGetValue(playerInfo.roomId, out ri))
                        {
                            var roomPlayers = _playerMap.Values.Where(p => p.roomId == playerInfo.roomId);

                            await roomPlayers.Select(p => p.webSocket).Send(MakePacket(recved.UID, new PacketLeaveRoomRes
                            {
                                playerId = playerInfo.playerId,
                                roomId = playerInfo.roomId,
                            }));
                        }
                        {
                            var roomPlayers = _playerMap.Values.Where(p => p.roomId == playerInfo.roomId);
                            if (roomPlayers.ToList().Count() <= 0)
                                _roomMap.TryRemove(playerInfo.roomId, out ri);
                        }
                        playerInfo.roomId = 0;
                    }
                    break;
                case ePacketType.StartGame:
                    {
                        var roomPlayers = _playerMap.Values.Where(p => p.roomId == playerInfo.roomId);

                        RoomInfo ri;
                        if (_roomMap.TryGetValue(playerInfo.roomId, out ri))
                        {
                            if (ri.playerList.Count() == 0) // if not started
                            {
                                ri.playerList = roomPlayers.Select(p => p.playerId).ToDictionary(q => q, q => false);

                                await roomPlayers.Select(p => p.webSocket).Send(MakePacket(recved.UID, new PacketStartGameRes
                                {
                                    roomId = playerInfo.roomId,
                                    playerList = ri.playerList.Keys.ToArray(),
                                }));
                            }
                        }
                    }
                    break;
                case ePacketType.LoadingComplete:
                    {
                        var roomPlayers = _playerMap.Values.Where(p => p.roomId == playerInfo.roomId);
                        RoomInfo ri;
                        if (_roomMap.TryGetValue(playerInfo.roomId, out ri))
                        {
                            bool ready;
                            if (ri.playerList.TryGetValue(playerInfo.playerId, out ready))
                            {
                                ready = true;
                                // echo
                                await roomPlayers.Select(p => p.webSocket).Send(json);

                                if (ri.playerList.All(p => p.Value))
                                {
                                    await roomPlayers.Select(p => p.webSocket).Send(MakePacket(recved.UID, new PacketStartBattleRes
                                    {
                                        roomId = playerInfo.roomId,
                                        playerList = ri.playerList.Keys.ToArray(),
                                    }));
                                }
                            }
                        }
                    }
                    break;
                default:
                    {
                        var roomPlayers = _playerMap.Values.Where(p => p.roomId == playerInfo.roomId);
                        await roomPlayers.Select(p => p.webSocket).ToList().Send(json);
                    }
                    break;
            }
        }
    }
}
