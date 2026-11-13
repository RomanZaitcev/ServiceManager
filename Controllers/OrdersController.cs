using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ServiceManager.Data;
using ServiceManager.Models;

namespace ServiceManager.Controllers
{
    [Authorize(Roles = AppRoles.ManagerOrTechnician)]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public OrdersController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Менеджер може все, майстер лише зі своїми замовленнями
        private bool CanAccess(Order order) =>
            User.IsInRole(AppRoles.Manager) || order.TechnicianId == _userManager.GetUserId(User);

        // ---------- Список ----------
        public async Task<IActionResult> Index(OrderStatus? status)
        {
            var query = _db.Orders
                .Include(o => o.Client)
                .Include(o => o.Technician)
                .AsQueryable();

            if (!User.IsInRole(AppRoles.Manager))
            {
                var currentUserId = _userManager.GetUserId(User);
                query = query.Where(o => o.TechnicianId == currentUserId);
            }

            if (status != null)
                query = query.Where(o => o.Status == status);

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            ViewData["StatusFilter"] = status;
            return View(orders);
        }

        // ---------- Створення (алгоритм 1.2) ----------
        [Authorize(Roles = AppRoles.Manager)]
        [HttpGet]
        public async Task<IActionResult> Create(int? clientId)
        {
            var model = new CreateOrderViewModel();

            if (clientId != null)
            {
                var client = await _db.Clients.FindAsync(clientId);
                if (client != null)
                {
                    model.FullName = client.FullName;
                    model.Phone = client.Phone;
                }
            }

            return View(model);
        }

        [Authorize(Roles = AppRoles.Manager)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderViewModel model)
        {
            model.FullName = model.FullName?.Trim() ?? string.Empty;
            model.Phone = model.Phone?.Trim() ?? string.Empty;
            model.DeviceInfo = model.DeviceInfo?.Trim() ?? string.Empty;
            model.ProblemDescription = model.ProblemDescription?.Trim() ?? string.Empty;

            // Крок 2: перевірка коректності полів
            if (!ModelState.IsValid)
                return View(model);

            // Крок 3: шукаємо клієнта за телефоном, якщо немає, створюємо нового
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.Phone == model.Phone);
            if (client == null)
            {
                client = new Client { FullName = model.FullName, Phone = model.Phone };
                _db.Clients.Add(client);
            }
            else if (!string.Equals(client.FullName, model.FullName, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Info"] = $"Клієнта знайдено за телефоном: {client.FullName}. Замовлення додано до його картки.";
            }

            // Крок 4: створюємо замовлення зі статусом Received і поточним часом
            var order = new Order
            {
                Client = client,
                DeviceInfo = model.DeviceInfo,
                ProblemDescription = model.ProblemDescription,
                Status = OrderStatus.Received,
                CreatedAt = DateTime.Now
            };
            _db.Orders.Add(order);

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Замовлення №{order.Id} створено";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // ---------- Картка замовлення ----------
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _db.Orders
                .Include(o => o.Client)
                .Include(o => o.Technician)
                .Include(o => o.UsedParts).ThenInclude(p => p.InventoryItem)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            if (!CanAccess(order)) return Forbid();

            var vm = new OrderDetailsViewModel { Order = order };

            if (User.IsInRole(AppRoles.Manager))
            {
                var technicians = await _userManager.GetUsersInRoleAsync(AppRoles.Technician);
                vm.Technicians = technicians
                    .OrderBy(t => t.Email)
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id,
                        Text = t.Email ?? t.UserName ?? t.Id
                    })
                    .ToList();
            }

            return View(vm);
        }

        // ---------- Зміна статусу (псевдокод 5.4) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status, string? comment)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (!CanAccess(order)) return Forbid();

            if (!Enum.IsDefined(status))
            {
                TempData["Error"] = "Невідомий статус";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (comment != null && comment.Length > 1000)
            {
                TempData["Error"] = "Коментар занадто довгий (максимум 1000 символів)";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.Status = status;
            order.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
            order.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Статус оновлено";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ---------- Призначення майстра ----------
        [Authorize(Roles = AppRoles.Manager)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTechnician(int id, string? technicianId)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (string.IsNullOrEmpty(technicianId))
            {
                order.TechnicianId = null;
            }
            else
            {
                var technician = await _userManager.FindByIdAsync(technicianId);
                if (technician == null || !await _userManager.IsInRoleAsync(technician, AppRoles.Technician))
                {
                    TempData["Error"] = "Обраного майстра не знайдено";
                    return RedirectToAction(nameof(Details), new { id });
                }
                order.TechnicianId = technician.Id;
            }

            order.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Майстра призначено";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}