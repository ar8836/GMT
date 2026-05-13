using Microsoft.AspNetCore.Mvc;

namespace GMT.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Landing() => View();
        public IActionResult Index() => View();
    }
}