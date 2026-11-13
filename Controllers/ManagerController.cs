using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceManager.Data;
using ServiceManager.Models;

namespace ServiceManager.Controllers
{
    [Authorize(Roles = AppRoles.Manager)]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ManagerController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var vm = new ManagerDashboardViewModel
            {
                NewToday = await _db.Orders.CountAsync(o => o.CreatedAt >= today),
                InWork = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Diagnostics),
                Queued = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Queued),
                CashMonth = await _db.OrderParts
                    .Where(p => (p.Order!.Status == OrderStatus.Repaired || p.Order.Status == OrderStatus.Released)
                                && p.Order.UpdatedAt >= monthStart)
                    .SumAsync(p => (decimal?)(p.QuantityUsed * p.PriceAtTime)) ?? 0,
                Recent = await _db.Orders
                    .Include(o => o.Client)
                    .Include(o => o.Technician)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View(vm);
        }
    }
}