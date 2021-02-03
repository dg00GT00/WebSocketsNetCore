using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebSocketServer.SocketsMethods;
using static WebSocketServer.SocketsMethods.SocketsMethods;

namespace WebSocketServer.Middleware
{
    public class WebSocketServerMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly WebSocketServerConnectionManager _manager = new WebSocketServerConnectionManager();

        public WebSocketServerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        private static async Task SendConnIdAsync(WebSocket socket, string connId)
        {
            var buffer = Encoding.UTF8.GetBytes($"ConnId: {connId}");
            await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }


        private async Task RouteJsonMessageAsync(string message)
        {
            var routeObj = JsonSerializer.Deserialize<JsonWebSocketMessage>(message);
            if (Guid.TryParse(routeObj.To, out var guid))
            {
                Console.WriteLine("Targeted");
                var (_, value) = _manager.GetAllSockets().FirstOrDefault(pair => pair.Key == routeObj.To.Trim());
                if (value != null)
                {
                    if (value.State == WebSocketState.Open)
                    {
                        await value.SendAsync(
                            new ArraySegment<byte>(Encoding.UTF8.GetBytes(routeObj.Message)),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                    }
                }
                else
                {
                    Console.WriteLine("Invalid recipient");
                }
            }
            else
            {
                Console.WriteLine("Broadcast");
                foreach (var (key, value) in _manager.GetAllSockets())
                {
                    if (value.State == WebSocketState.Open)
                    {
                        await value.SendAsync(
                            new ArraySegment<byte>(Encoding.UTF8.GetBytes(routeObj.Message)),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                    }
                }
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            WriteRequestParam(context);
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                Console.WriteLine("WebSocket Connected");

                await SendConnIdAsync(webSocket, _manager.AddSocket(webSocket));

                await ReceiveMessage(webSocket, async (result, bytes) =>
                {
                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Text:
                            var message = Encoding.UTF8.GetString(bytes, 0, result.Count);
                            Console.WriteLine($"Message received: {message}");
                            await RouteJsonMessageAsync(message);
                            break;
                        case WebSocketMessageType.Close:
                            Console.WriteLine("Received close message");

                            var connId = _manager.GetAllSockets().FirstOrDefault(pair => pair.Value == webSocket).Key;
                            _manager.GetAllSockets().TryRemove(connId, out var ws);
                            if (ws != null)
                            {
                                await ws.CloseAsync(
                                    result.CloseStatus ?? WebSocketCloseStatus.Empty,
                                    result.CloseStatusDescription,
                                    CancellationToken.None);
                            }

                            break;
                        case WebSocketMessageType.Binary:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
            }
            else
            {
                Console.WriteLine("Coming from the request");
                await _next(context);
                Console.WriteLine("Coming from the response");
                WriteResponseParam(context);
            }
        }
    }
}