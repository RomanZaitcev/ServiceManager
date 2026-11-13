using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManager.Models;
using System.Diagnostics;

namespace ServiceManager.Controllers
{
    public class HomeController : Controller
    {
        [Authorize] // неавторизованого відправить на сторінку входу
        public IActionResult Index()
        {
            if (User.IsInRole(AppRoles.Manager))
                return RedirectToAction("Dashboard", "Manager");

            if (User.IsInRole(AppRoles.Technician))
                return RedirectToAction("Dashboard", "Technician");

            return View(); // користувач без ролі бачить стандартну сторінку
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}