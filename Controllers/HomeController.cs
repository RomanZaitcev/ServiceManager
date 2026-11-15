using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceManager.Data;
using ServiceManager.Models;
using System.Diagnostics;

namespace ServiceManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [Authorize] // неавторизованого відправить на сторінку входу
        public async Task<IActionResult> Index()
        {
            var isManager = User.IsInRole(AppRoles.Manager);
            var isTech = User.IsInRole(AppRoles.Technician);

            var vm = new HomeDashboardViewModel
            {
                IsManager = isManager,
                IsTechnician = isTech
            };

            // користувач без ролі бачить лише повідомлення
            if (!isManager && !isTech)
                return View(vm);

            // менеджер бачить усі замовлення, майстер лише свої
            var orders = _db.Orders.AsQueryable();
            if (!isManager)
            {
                var currentUserId = _userManager.GetUserId(User);
                orders = orders.Where(o => o.TechnicianId == currentUserId);
            }

            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            vm.NewCount = await orders.CountAsync(o => o.Status == OrderStatus.Received);
            vm.ActiveCount = await orders.CountAsync(o => o.Status != OrderStatus.Released);
            vm.AwaitingPartsCount = await orders.CountAsync(o => o.Status == OrderStatus.AwaitingParts);
            vm.DoneMonthCount = await orders.CountAsync(o =>
                (o.Status == OrderStatus.Repaired || o.Status == OrderStatus.Released)
                && o.UpdatedAt >= monthStart);

            if (isManager)
            {
                vm.CashMonth = await _db.OrderParts
                    .Where(p => (p.Order!.Status == OrderStatus.Repaired || p.Order.Status == OrderStatus.Released)
                                && p.Order.UpdatedAt >= monthStart)
                    .SumAsync(p => (decimal?)(p.QuantityUsed * p.PriceAtTime)) ?? 0;
            }

            vm.Recent = await orders
                .Include(o => o.Client)
                .Include(o => o.Technician)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(vm);
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