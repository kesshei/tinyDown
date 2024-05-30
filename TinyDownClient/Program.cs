using DownModel;
using Microsoft.Extensions.Configuration;
using System.Net.WebSockets;

namespace TinyDownClient
{
    internal class Program
    {
        /*
         上传  本地文件路径，远程目标路径 (直接 参数+内容 )
         下载  远程目标路径，本地文件路径 (直接 url参数 + 参数+内容)
         */
        static IConfiguration config;
        static async Task Main(string[] args)
        {
            config = new ConfigurationBuilder()
              .AddCommandLine(args)
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("settings.json", optional: true, reloadOnChange: true)
              .Build();

            Console.WriteLine($"urls: {config["urls"]}");
            Console.WriteLine($"token: {config["token"]}");
            var argsStr = string.Join("", args);
            var UPorDown = argsStr.IndexOf("local") < argsStr.IndexOf("server");
            if (UPorDown)
            {
                Console.WriteLine($"上传:{config["local"]} -> {config["server"]}");
            }
            else
            {
                Console.WriteLine($"下载:{config["server"]} -> {config["local"]}");
            }
            var downUrl = "";
            Dictionary<string, string> keys = new Dictionary<string, string>
            {
                { "token", config["token"] }
            };
            if (!UPorDown)
            {
                keys.Add("server", config["server"]);
                keys.Add("local", config["local"]);
            }
            downUrl = string.Join("&", keys.Select(t => $"{t.Key}={t.Value}"));
            var webSocket = await CreateAsync($"ws://{config["urls"]}/ws?{downUrl}");
            if (webSocket != null)
            {
                Console.WriteLine("服务开始执行!");
                var local = config["local"];
                var server = config["server"];

                if (UPorDown)
                {
                   await WebSocketServer.UpdateFileAsync(webSocket, local, server);
                }
                else
                {
                    await WebSocketServer.DownFileAsync(webSocket);
                }
            }
            else
            {
                Console.WriteLine("服务连接失败!");
            }
            Console.WriteLine("服务执行完毕!");
            Console.ReadLine();
        }
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
