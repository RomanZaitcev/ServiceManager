using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManager.Models;

namespace ServiceManager.Controllers
{
    [Authorize(Roles = AppRoles.Technician)]
    public class TechnicianController : Controller
    {
        public IActionResult MyRepairs()
        {
            return RedirectToAction("Index", "Orders");
        }
    }
}