using Microsoft.AspNetCore.Mvc;

namespace ZooKeeper.Mgt.Website.Controllers
{
    public class HelpController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}