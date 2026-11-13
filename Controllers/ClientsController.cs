using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceManager.Data;
using ServiceManager.Models;

namespace ServiceManager.Controllers
{
    [Authorize(Roles = AppRoles.Manager)]
    public class ClientsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ClientsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Список із пошуком за ПІБ або телефоном
        public async Task<IActionResult> Index(string? search)
        {
            var query = _db.Clients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(c => c.FullName.Contains(search) || c.Phone.Contains(search));
            }

            var clients = await query
                .OrderBy(c => c.FullName)
                .Select(c => new ClientListItem
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Phone = c.Phone,
                    OrdersCount = c.Orders.Count
                })
                .ToListAsync();

            ViewData["Search"] = search;
            return View(clients);
        }

        // Картка клієнта з історією ремонтів
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var client = await _db.Clients
                .Include(c => c.Orders).ThenInclude(o => o.Technician)
                .Include(c => c.Orders).ThenInclude(o => o.UsedParts)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null) return NotFound();

            client.Orders = client.Orders.OrderByDescending(o => o.CreatedAt).ToList();
            return View(client);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Client());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Phone")] Client client)
        {
            client.FullName = client.FullName?.Trim() ?? string.Empty;
            client.Phone = client.Phone?.Trim() ?? string.Empty;

            if (await _db.Clients.AnyAsync(c => c.Phone == client.Phone))
                ModelState.AddModelError(nameof(client.Phone), "Клієнт з таким телефоном уже існує");

            if (!ModelState.IsValid)
                return View(client);

            _db.Clients.Add(client);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = client.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var client = await _db.Clients.FindAsync(id);
            if (client == null) return NotFound();

            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Phone")] Client model)
        {
            if (id != model.Id) return NotFound();

            var client = await _db.Clients.FindAsync(id);
            if (client == null) return NotFound();

            model.FullName = model.FullName?.Trim() ?? string.Empty;
            model.Phone = model.Phone?.Trim() ?? string.Empty;

            if (await _db.Clients.AnyAsync(c => c.Phone == model.Phone && c.Id != id))
                ModelState.AddModelError(nameof(model.Phone), "Інший клієнт з таким телефоном уже існує");

            if (!ModelState.IsValid)
                return View(model);

            client.FullName = model.FullName;
            client.Phone = model.Phone;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}