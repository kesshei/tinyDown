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
        public async Task WS()   
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                try
                {
                    var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    await new WebSocketHelper().WebSocketReceive(socket);
                }
                catch (Exception)
                {
                }
            }
        }
    }
    public class WebSocketHelper
    {
        public async Task WebSocketReceive(WebSocket webSocket)
        {
            var id = Guid.NewGuid().ToString("N");
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
                    var sendStr = Encoding.UTF8.GetBytes($"服务端 {id} : {text}  -{DateTime.Now}");
                    await webSocket.SendAsync(sendStr, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
