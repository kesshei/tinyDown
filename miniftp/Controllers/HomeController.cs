using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Diagnostics;

namespace miniftp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Down(string file)
        {
            var fileName = Path.GetFileName(file);
            if (provider.TryGetContentType(fileName, out var contentType) && System.IO.File.Exists(file))
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
