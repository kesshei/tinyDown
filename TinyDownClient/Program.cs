using Microsoft.Extensions.Configuration;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;

namespace TinyDownClient
{
    internal class Program
    {
        static IConfiguration config;
        static async Task Main(string[] args)
        {
            config = new ConfigurationBuilder()
              .AddCommandLine(args)
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("settings.json", optional: true, reloadOnChange: true)
              .Build();

            Console.WriteLine($"path: {config["path"]}");
            Console.WriteLine($"other: {config["other"]}");
            Console.WriteLine($"urls: {config["urls"]}");

            var webSocket = await CreateAsync($"ws://{config["urls"]}/ws");
            if (webSocket != null)
            {
                Console.WriteLine("服务开始执行!");
                _ = Task.Run(async () =>
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(1024);
                    try
                    {
                        while (webSocket.State == WebSocketState.Open)
                        {
                            var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, result.CloseStatusDescription);
                            }
                            var text = Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count));
                            Console.WriteLine(text);
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                });
                Console.WriteLine("开始输入：");
                var text = string.Empty;
                while (text != "exit")
                {
                    ;
                    text = Console.ReadLine();
                    var sendStr = Encoding.UTF8.GetBytes(text);
                    await webSocket.SendAsync(sendStr, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            else
            {
                Console.WriteLine("服务连接失败!");
            }
            Console.WriteLine("服务执行完毕!");
            Console.ReadLine();
        }
        /// <summary>
        /// 创建客户端实例
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<ClientWebSocket> CreateAsync(string ServerUri)
        {
            var webSocket = new ClientWebSocket();
            webSocket.Options.RemoteCertificateValidationCallback = delegate { return true; };

            await webSocket.ConnectAsync(new Uri(ServerUri), CancellationToken.None);
            if (webSocket.State == WebSocketState.Open)
            {
                return webSocket;
            }
            return null;
        }
    }
}
