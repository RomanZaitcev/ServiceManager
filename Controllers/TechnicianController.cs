using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceManager.Data;
using ServiceManager.Models;

namespace ServiceManager.Controllers
{
    [Authorize(Roles = AppRoles.Technician)]
    public class TechnicianController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public TechnicianController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var uid = _userManager.GetUserId(User);
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var mine = _db.Orders.Where(o => o.TechnicianId == uid);

            var vm = new TechnicianDashboardViewModel
            {
                Queued = await mine.CountAsync(o => o.Status == OrderStatus.Queued),
                InWork = await mine.CountAsync(o => o.Status == OrderStatus.Diagnostics),
                AwaitingParts = await mine.CountAsync(o => o.Status == OrderStatus.AwaitingParts),
                DoneMonth = await mine.CountAsync(o =>
                    (o.Status == OrderStatus.Repaired || o.Status == OrderStatus.Released)
                    && o.UpdatedAt >= monthStart),
                Recent = await mine.OrderByDescending(o => o.CreatedAt).Take(5).ToListAsync()
            };

            return View(vm);
        }
    }
}