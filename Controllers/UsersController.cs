using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceManager.Models;

namespace ServiceManager.Controllers
{
    [Authorize(Roles = AppRoles.Manager)]
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public UsersController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var list = new List<UserListItem>();
            foreach (var user in _userManager.Users.OrderBy(u => u.Email).ToList())
            {
                var roles = await _userManager.GetRolesAsync(user);
                list.Add(new UserListItem
                {
                    Email = user.Email ?? user.UserName ?? "",
                    Role = string.Join(", ", roles)
                });
            }
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (model.Role != AppRoles.Manager && model.Role != AppRoles.Technician)
                ModelState.AddModelError(nameof(model.Role), "Невідома роль");

            if (!ModelState.IsValid)
                return View(model);

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);
            return RedirectToAction(nameof(Index));
        }
    }
}