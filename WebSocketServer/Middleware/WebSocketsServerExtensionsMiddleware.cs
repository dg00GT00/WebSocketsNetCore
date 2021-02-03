using Microsoft.AspNetCore.Builder;

namespace WebSocketServer.Middleware
{
    public static class WebSocketsServerExtensionsMiddleware
    {
        public static IApplicationBuilder UseWebSocketServer(this IApplicationBuilder app)
        {
            return app.UseMiddleware<WebSocketServerMiddleware>();
        }
    }
}