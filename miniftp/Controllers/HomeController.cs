using DownModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using miniftp.Model;
using System.Buffers;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

namespace miniftp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();
        private IConfiguration Configuration;
        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            this.Configuration = configuration;
        }
        public string GetConfigValue(string key)
        {
            return Configuration[key];
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Down(string file, string key)
        {
            var path2 = this.Configuration.GetSection("pathconfig").Get<PathConfig>();
            var fileName = Path.GetFileName(file);
            if (!path2.allow.Any(t => file.StartsWith(t)))
            {
                return NotFound();
            }
            if (!path2.keys.Any(t => t == key))
            {
                return NotFound();
            }
            if (provider.TryGetContentType(fileName, out var contentType) && System.IO.File.Exists(file))
            {
                return PhysicalFile(file, contentType, fileName);
            }
            else
            {
                return NotFound();
            }
        }
        public IActionResult Upload()
        {
            try
            {
                var rootDir = Path.Combine(AppContext.BaseDirectory, "file");
                var rootDirTemp = Path.Combine(AppContext.BaseDirectory, "temp");
                if (!Directory.Exists(rootDir))
                {
                    Directory.CreateDirectory(rootDir);
                }
                if (!Directory.Exists(rootDirTemp))
                {
                    Directory.CreateDirectory(rootDirTemp);
                }
                if (this.HttpContext.Request.Form.Files.Any())
                {
                    foreach (var item in this.HttpContext.Request.Form.Files)
                    {
                        var filename = item.FileName;

                        var path = Path.Combine(rootDirTemp, filename);
                        var truePath = Path.Combine(rootDir, filename);
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }
                        using (var stream = System.IO.File.Create(path))
                        {
                            item.CopyTo(stream);
                        }
                        if (System.IO.File.Exists(truePath))
                        {
                            System.IO.File.Delete(truePath);
                        }
                        System.IO.File.Move(path, truePath);
                    }
                    return Json(new { status = "success" });
                }
                else
                {
                    throw new Exception("文件不存在!");
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", msg = ex.Message });
            }
        }
        [HttpGet("/ws")]
        public async Task WS(string token, string local, string server)
        {
            var tokens = this.Configuration.GetSection("tokens").Get<List<string>>();
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                try
                {
                    var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    if (!tokens.Contains(token))
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.ProtocolError, "token error", CancellationToken.None);
                    }
                    else
                    {
                        await WebSocketReceive(socket, local, server);
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        public async Task WebSocketReceive(WebSocket webSocket, string local, string server)
        {
            var down = !string.IsNullOrEmpty(local) && !string.IsNullOrEmpty(server);
            if (down)
            {
                var result = await WebSocketServer.UpdateFileAsync(webSocket, server, local);
                //if (System.IO.File.Exists(server))
                //{
                //    using var file = System.IO.File.OpenRead(server);
                //    var hash256 = file.Hash256();

                //    RequestInfo requestInfo = new RequestInfo() { FileLength = file.Length, ServerPath = server, LoalPath = local, Hash256 = hash256 };
                //    var bytes = Encoding.UTF8.GetBytes(requestInfo.ToJson());
                //    var HeadBytes = BitConverter.GetBytes(bytes.Length);
                //    await webSocket.SendAsync(HeadBytes.Concat(bytes).ToArray(), WebSocketMessageType.Binary, false, CancellationToken.None);
                //    var buffer2 = ArrayPool<byte>.Shared.Rent(1024);
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
                //            Console.WriteLine($"已发送:{file.Position / file.Length}");
                //        }
                //        Console.WriteLine("发送完毕!");
                //    }
                //    catch (Exception)
                //    {
                //        throw;
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
                //var buffer = ArrayPool<byte>.Shared.Rent(1 * 1024 * 1024);
                //var listBytes = new List<byte>();
                //var length = -1;
                //RequestInfo requestInfo = null;
                //try
                //{
                //    while (webSocket.State == WebSocketState.Open)
                //    {
                //        var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                //        if (result.MessageType == WebSocketMessageType.Close)
                //        {
                //            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, result.CloseStatusDescription);
                //        }
                //        else if (result.MessageType == WebSocketMessageType.Binary)
                //        {
                //            if (length == -1)
                //            {
                //                length = BitConverter.ToInt32(buffer, 0);
                //            }
                //            if (requestInfo == null)
                //            {
                //                listBytes.AddRange(buffer.Take(result.Count));
                //                if (listBytes.Count >= length + 4)
                //                {
                //                    requestInfo = Encoding.UTF8.GetString(listBytes.Skip(4).Take(length).ToArray()).ToObj<RequestInfo>();
                //                    if (System.IO.File.Exists(requestInfo.ServerPath))
                //                    {
                //                        System.IO.File.Delete(requestInfo.ServerPath);
                //                    }
                //                    using var write = System.IO.File.OpenWrite(requestInfo.ServerPath);
                //                    var byteinfo = listBytes.Skip(4 + length).ToArray();
                //                    if (byteinfo.Length > 0)
                //                    {
                //                        write.Write(byteinfo);
                //                    }
                //                }
                //            }
                //            else
                //            {
                //                using var write = System.IO.File.OpenWrite(requestInfo.ServerPath);
                //                write.Seek(0, SeekOrigin.End);
                //                write.Write(buffer.Take(result.Count).ToArray());
                //            }
                //        }
                //        else
                //        {
                //            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bad type", CancellationToken.None);
                //            throw new Exception("接收文件的类型不正确!");
                //        }
                //        if (result.EndOfMessage)
                //        {
                //            using var read = System.IO.File.OpenRead(requestInfo.ServerPath);
                //            read.Position = 0;
                //            var hash256 = read.Hash256();
                //            if (hash256 == requestInfo.Hash256)
                //            {
                //                Console.WriteLine($"上传并验证成功:{requestInfo.ServerPath}");
                //            }
                //            else
                //            {
                //                Console.WriteLine($"上传并验证失败:{requestInfo.ServerPath}");
                //            }

                //        }
                //    }
                //}
                //catch (Exception ex)
                //{
                //    throw;
                //}
                //finally
                //{
                //    ArrayPool<byte>.Shared.Return(buffer);
                //}
            }
        }
    }
}
