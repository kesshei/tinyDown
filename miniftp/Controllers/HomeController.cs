using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using miniftp.Model;
using System.Diagnostics;

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
        public IActionResult Down(string file,string key)
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
            if (provider.TryGetContentType(fileName, out var contentType) && System.IO.File.Exists(file) )
            {
                return PhysicalFile(file, contentType, fileName);
            }
            else
            {
                return NotFound();
            }
        }
    }
}
