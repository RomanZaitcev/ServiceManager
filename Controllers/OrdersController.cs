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
        public async Task<IActionResult> Index(string? search, OrderStatus? status, string? technicianId)
        {
            var isManager = User.IsInRole(AppRoles.Manager);

            var query = _db.Orders
                .Include(o => o.Client)
                .Include(o => o.Technician)
                .AsQueryable();

            if (!isManager)
            {
                var currentUserId = _userManager.GetUserId(User);
                query = query.Where(o => o.TechnicianId == currentUserId);
            }
            else if (!string.IsNullOrEmpty(technicianId))
            {
                query = query.Where(o => o.TechnicianId == technicianId);
            }

            if (status != null)
                query = query.Where(o => o.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                var hasId = int.TryParse(search, out var searchId);
                query = query.Where(o =>
                    (hasId && o.Id == searchId) ||
                    o.Client!.FullName.Contains(search) ||
                    o.DeviceInfo.Contains(search));
            }

            if (isManager)
            {
                var technicians = await _userManager.GetUsersInRoleAsync(AppRoles.Technician);
                ViewData["Technicians"] = technicians
                    .OrderBy(t => t.Email)
                    .Select(t => new SelectListItem { Value = t.Id, Text = t.Email ?? t.UserName ?? t.Id })
                    .ToList();
            }

            ViewData["Search"] = search;
            ViewData["StatusFilter"] = status;
            ViewData["TechnicianFilter"] = technicianId;

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
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
                    model.Address = client.Address;
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
            model.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
            model.DeviceType = model.DeviceType?.Trim() ?? string.Empty;
            model.DeviceInfo = model.DeviceInfo?.Trim() ?? string.Empty;
            model.SerialNumber = string.IsNullOrWhiteSpace(model.SerialNumber) ? null : model.SerialNumber.Trim();
            model.Accessories = string.IsNullOrWhiteSpace(model.Accessories) ? null : model.Accessories.Trim();
            model.ExternalCondition = string.IsNullOrWhiteSpace(model.ExternalCondition) ? null : model.ExternalCondition.Trim();
            model.ProblemDescription = model.ProblemDescription?.Trim() ?? string.Empty;

            // Крок 2: перевірка коректності полів
            if (!ModelState.IsValid)
                return View(model);

            // Крок 3: шукаємо клієнта за телефоном, якщо немає, створюємо нового
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.Phone == model.Phone);
            if (client == null)
            {
                client = new Client
                {
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Address = model.Address
                };
                _db.Clients.Add(client);
            }
            else
            {
                // адресу дописуємо лише якщо в картці клієнта її ще немає
                if (string.IsNullOrEmpty(client.Address) && model.Address != null)
                    client.Address = model.Address;

                if (!string.Equals(client.FullName, model.FullName, StringComparison.OrdinalIgnoreCase))
                    TempData["Info"] = $"Клієнта знайдено за телефоном: {client.FullName}. Замовлення додано до його картки.";
            }

            // Крок 4: створюємо замовлення зі статусом Received і поточним часом
            var order = new Order
            {
                Client = client,
                DeviceType = model.DeviceType,
                DeviceInfo = model.DeviceInfo,
                SerialNumber = model.SerialNumber,
                Accessories = model.Accessories,
                ExternalCondition = model.ExternalCondition,
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

            vm.InventoryItems = await _db.InventoryItems
                .OrderBy(i => i.ItemType).ThenBy(i => i.Name)
                .ToListAsync();

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
        // ---------- Додати роботу або запчастину ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPart(int id, int inventoryItemId, int quantity)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (!CanAccess(order)) return Forbid();

            if (order.Status == OrderStatus.Released)
            {
                TempData["Error"] = "Замовлення видано, змінювати роботи й запчастини не можна";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (quantity < 1 || quantity > 10000)
            {
                TempData["Error"] = "Кількість має бути від 1 до 10000";
                return RedirectToAction(nameof(Details), new { id });
            }

            var item = await _db.InventoryItems.FindAsync(inventoryItemId);
            if (item == null)
            {
                TempData["Error"] = "Позицію не знайдено";
                return RedirectToAction(nameof(Details), new { id });
            }

            await using var tx = await _db.Database.BeginTransactionAsync();

            if (item.ItemType == ItemType.Product)
            {
                // атомарне списання: спрацює лише якщо на складі достатньо
                var affected = await _db.InventoryItems
                    .Where(i => i.Id == inventoryItemId && i.StockQuantity >= quantity)
                    .ExecuteUpdateAsync(s => s.SetProperty(i => i.StockQuantity, i => i.StockQuantity - quantity));

                if (affected == 0)
                {
                    TempData["Error"] = $"Недостатньо на складі: «{item.Name}» (доступно {item.StockQuantity} шт)";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            _db.OrderParts.Add(new OrderPart
            {
                OrderId = id,
                InventoryItemId = item.Id,
                QuantityUsed = quantity,
                PriceAtTime = item.Price
            });
            order.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["Success"] = "Додано: " + item.Name;
            return RedirectToAction(nameof(Details), new { id });
        }

        // ---------- Видалити рядок (залишок повертається) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePart(int id, int partId)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (!CanAccess(order)) return Forbid();

            if (order.Status == OrderStatus.Released)
            {
                TempData["Error"] = "Замовлення видано, змінювати роботи й запчастини не можна";
                return RedirectToAction(nameof(Details), new { id });
            }

            var part = await _db.OrderParts
                .Include(p => p.InventoryItem)
                .FirstOrDefaultAsync(p => p.Id == partId && p.OrderId == id);
            if (part == null) return NotFound();

            await using var tx = await _db.Database.BeginTransactionAsync();

            if (part.InventoryItem?.ItemType == ItemType.Product)
            {
                var itemId = part.InventoryItemId;
                var qty = part.QuantityUsed;
                await _db.InventoryItems
                    .Where(i => i.Id == itemId)
                    .ExecuteUpdateAsync(s => s.SetProperty(i => i.StockQuantity, i => i.StockQuantity + qty));
            }

            _db.OrderParts.Remove(part);
            order.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["Success"] = "Рядок видалено, залишок на складі повернуто";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ---------- Друк квитанції про прийом ----------
        [Authorize(Roles = AppRoles.Manager)]
        public async Task<IActionResult> PrintReceipt(int? id)
        {
            if (id == null) return NotFound();

            var order = await _db.Orders
                .Include(o => o.Client)
                .Include(o => o.Technician)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            return View(order);
        }

        // ---------- Друк акта виконаних робіт ----------
        [Authorize(Roles = AppRoles.Manager)]
        public async Task<IActionResult> PrintAct(int? id)
        {
            if (id == null) return NotFound();

            var order = await _db.Orders
                .Include(o => o.Client)
                .Include(o => o.Technician)
                .Include(o => o.UsedParts).ThenInclude(p => p.InventoryItem)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            if (order.Status != OrderStatus.Repaired && order.Status != OrderStatus.Released)
            {
                TempData["Error"] = "Акт можна друкувати після статусу «Виконано»";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(order);
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