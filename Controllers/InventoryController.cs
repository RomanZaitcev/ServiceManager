using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceManager.Data;
using ServiceManager.Models;

namespace ServiceManager.Controllers
{
    [Authorize(Roles = AppRoles.ManagerOrTechnician)]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        public InventoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Список із вкладками, пошуком і пагінацією
        public async Task<IActionResult> Index(string? search, ItemType? type, int page = 1)
        {
            const int pageSize = 10;

            var query = _db.InventoryItems.AsQueryable();

            if (type != null)
                query = query.Where(i => i.ItemType == type);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(i => i.Name.Contains(search) || i.Code.Contains(search));
            }

            var total = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var items = await query
                .OrderBy(i => i.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["Search"] = search;
            ViewData["Type"] = type;
            ViewData["Page"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalPages"] = totalPages;
            ViewData["Total"] = total;
            return View(items);
        }

        // ---------- Створення ----------
        [Authorize(Roles = AppRoles.Manager)]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new InventoryItem());
        }

        [Authorize(Roles = AppRoles.Manager)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Code,ItemType,StockQuantity,Price")] InventoryItem item)
        {
            item.Name = item.Name?.Trim() ?? string.Empty;
            item.Code = item.Code?.Trim() ?? string.Empty;

            // для послуг залишок не ведеться
            if (item.ItemType == ItemType.Service)
            {
                item.StockQuantity = 0;
                ModelState.Remove(nameof(item.StockQuantity));
            }

            if (await _db.InventoryItems.AnyAsync(i => i.Code == item.Code))
                ModelState.AddModelError(nameof(item.Code), "Позиція з таким артикулом уже існує");

            if (!ModelState.IsValid)
                return View(item);

            _db.InventoryItems.Add(item);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Позицію додано";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Редагування ----------
        [Authorize(Roles = AppRoles.Manager)]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var item = await _db.InventoryItems.FindAsync(id);
            if (item == null) return NotFound();

            return View(item);
        }

        [Authorize(Roles = AppRoles.Manager)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, [Bind("Id,Name,Code,ItemType,StockQuantity,Price")] InventoryItem model)
        {
            if (id != model.Id) return NotFound();

            var item = await _db.InventoryItems.FindAsync(id);
            if (item == null) return NotFound();

            model.Name = model.Name?.Trim() ?? string.Empty;
            model.Code = model.Code?.Trim() ?? string.Empty;

            if (model.ItemType == ItemType.Service)
            {
                model.StockQuantity = 0;
                ModelState.Remove(nameof(model.StockQuantity));
            }

            if (await _db.InventoryItems.AnyAsync(i => i.Code == model.Code && i.Id != id))
                ModelState.AddModelError(nameof(model.Code), "Інша позиція з таким артикулом уже існує");

            if (item.ItemType != model.ItemType &&
                await _db.OrderParts.AnyAsync(p => p.InventoryItemId == id))
                ModelState.AddModelError(nameof(model.ItemType),
                    "Не можна змінити тип позиції, яка вже використана в замовленнях");

            if (!ModelState.IsValid)
                return View(model);

            item.Name = model.Name;
            item.Code = model.Code;
            item.ItemType = model.ItemType;
            item.StockQuantity = model.StockQuantity;
            item.Price = model.Price;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Зміни збережено";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Історія використання ----------
        [Authorize(Roles = AppRoles.Manager)]
        public async Task<IActionResult> History(int? id)
        {
            if (id == null) return NotFound();

            var item = await _db.InventoryItems.FindAsync(id);
            if (item == null) return NotFound();

            var rows = await _db.OrderParts
                .Where(p => p.InventoryItemId == id)
                .Include(p => p.Order).ThenInclude(o => o!.Client)
                .OrderByDescending(p => p.Order!.CreatedAt)
                .ToListAsync();

            ViewData["Item"] = item;
            return View(rows);
        }
    }
}