using DownModel;
using Microsoft.Extensions.Configuration;
using System.Buffers;
using System.Linq;
using System.Net.WebSockets;
using System.Text;

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
                    var result = await WebSocketServer.UpdateFileAsync(webSocket, local, server);
                    //if (System.IO.File.Exists(local))
                    //{
                    //    using var file = System.IO.File.OpenRead(local);
                    //    var hash256 = file.Hash256();

                    //    RequestInfo requestInfo = new RequestInfo() { FileLength = file.Length, ServerPath = server, LoalPath = local, Hash256 = hash256 };
                    //    var bytes = Encoding.UTF8.GetBytes(requestInfo.ToJson());
                    //    var HeadBytes = BitConverter.GetBytes(bytes.Length);
                    //    await webSocket.SendAsync(HeadBytes.Concat(bytes).ToArray(), WebSocketMessageType.Binary, false, CancellationToken.None);
                    //    var buffer2 = ArrayPool<byte>.Shared.Rent(5 * 1024 * 1024);
                    //    try
                    //    {
                    //        file.Position = 0;
                    //        var endOfMessage = false;
                    //        while (!endOfMessage)
                    //        {
                    //            var count = file.Read(buffer2);
                    //            if (count < buffer2.Length)
                    //            {
                    //                endOfMessage = true;
                    //            }
                    //            await webSocket.SendAsync(buffer2.Take(count).ToArray(), WebSocketMessageType.Binary, endOfMessage, CancellationToken.None);
                    //            Console.WriteLine($"已发送:{(double)file.Position / (double)file.Length}%");
                    //        }
                    //        Console.WriteLine("发送完毕!");
                    //        try
                    //        {
                    //            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "ok", CancellationToken.None);
                    //        }
                    //        catch (Exception)
                    //        {
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Console.WriteLine($"异常:{ex.Message}");
                    //    }
                    //    finally
                    //    {
                    //        ArrayPool<byte>.Shared.Return(buffer2);
                    //    }
                    //}
                }
                else
                {
                    var result = await WebSocketServer.DownFileAsync(webSocket);
                    //    var buffer = ArrayPool<byte>.Shared.Rent(1024);
                    //    var listBytes = new List<byte>();
                    //    var length = -1;
                    //    RequestInfo requestInfo = null;
                    //    try
                    //    {
                    //        while (webSocket.State == WebSocketState.Open)
                    //        {
                    //            var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    //            if (result.MessageType == WebSocketMessageType.Close)
                    //            {
                    //                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, result.CloseStatusDescription);
                    //            }
                    //            else if (result.MessageType == WebSocketMessageType.Binary)
                    //            {
                    //                if (length == -1)
                    //                {
                    //                    length = BitConverter.ToInt32(buffer, 0);
                    //                }
                    //                if (requestInfo == null)
                    //                {
                    //                    listBytes.AddRange(buffer.Take(result.Count));
                    //                    if (listBytes.Count >= length + 4)
                    //                    {
                    //                        requestInfo = Encoding.UTF8.GetString(listBytes.Skip(4).Take(length).ToArray()).ToObj<RequestInfo>();
                    //                        using var write = File.OpenWrite(local);
                    //                        var byteinfo = listBytes.Skip(4 + length).ToArray();
                    //                        if (byteinfo.Length > 0)
                    //                        {
                    //                            write.Write(byteinfo);
                    //                        }
                    //                    }
                    //                }
                    //                else
                    //                {
                    //                    using var write = File.OpenWrite(local);
                    //                    write.Seek(0, SeekOrigin.End);
                    //                    write.Write(buffer.Take(result.Count).ToArray());
                    //                }
                    //            }
                    //            else
                    //            {
                    //                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bad type", CancellationToken.None);
                    //                throw new Exception("接收文件的类型不正确!");
                    //            }
                    //            if (result.EndOfMessage)
                    //            {
                    //                using var read = File.OpenRead(local);
                    //                var hash256 = read.Hash256();
                    //                if (hash256 == requestInfo.Hash256)
                    //                {
                    //                    Console.WriteLine($"下载并验证成功:{local}");
                    //                }
                    //                else
                    //                {
                    //                    Console.WriteLine($"下载并验证失败:{local}");
                    //                }
                    //            }
                    //        }
                    //    }
                    //    finally
                    //    {
                    //        ArrayPool<byte>.Shared.Return(buffer);
                    //    }
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
