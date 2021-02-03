using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebSocketServer.SocketsMethods
{
    public static class SocketsMethods
    {
        public static async Task ReceiveMessage(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                handleMessage(result, buffer);
            }
        }

        public static void WriteRequestParam(HttpContext context)
        {
            Console.WriteLine("Request Method: {0}", context.Request.Method);
            Console.WriteLine("Request Protocol: {0}", context.Request.Protocol);

            if (context.Request.Headers == null) return;
            foreach (var (key, value) in context.Request.Headers)
            {
                Console.WriteLine($"--> {key} : {value}");
            }
        }

        public static void WriteResponseParam(HttpContext context)
        {
            Console.WriteLine("Response Status: {0}", context.Response.StatusCode);
            Console.WriteLine("Response Body: {0}", context.Response.Body);

            if (context.Response.Headers == null) return;
            foreach (var (key, value) in context.Response.Headers)
            {
                Console.WriteLine($"<-- {key} : {value}");
            }
        }
    }
}