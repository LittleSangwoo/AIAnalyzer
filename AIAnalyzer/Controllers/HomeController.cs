using AIAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AIAnalyzer.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
