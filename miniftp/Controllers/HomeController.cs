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
                await WebSocketServer.UpdateFileAsync(webSocket, server, local);
            }
            else
            {
                await WebSocketServer.DownFileAsync(webSocket);
            }
        }
    }
}
