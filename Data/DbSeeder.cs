using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceManager.Models;

namespace ServiceManager.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
            if (!await db.InventoryItems.AnyAsync())
            {
                db.InventoryItems.AddRange(
                    new InventoryItem { Code = "ART-1002", Name = "Дисплейний модуль iPhone 13", ItemType = ItemType.Product, StockQuantity = 2, Price = 3200m },
                    new InventoryItem { Code = "SRV-2001", Name = "Заміна екрана / тачскріна", ItemType = ItemType.Service, StockQuantity = 0, Price = 600m },
                    new InventoryItem { Code = "ART-1015", Name = "Ролик захвату паперу Canon", ItemType = ItemType.Product, StockQuantity = 14, Price = 250m },
                    new InventoryItem { Code = "SRV-2005", Name = "Діагностика та профілактика", ItemType = ItemType.Service, StockQuantity = 0, Price = 200m },
                    new InventoryItem { Code = "ART-1020", Name = "Акумулятор LiFePO4 100Ah", ItemType = ItemType.Product, StockQuantity = 5, Price = 4500m },
                    new InventoryItem { Code = "SRV-2012", Name = "Перепайка роз'ємів / портів", ItemType = ItemType.Service, StockQuantity = 0, Price = 400m });
                await db.SaveChangesAsync();
            }

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

            foreach (var role in new[] { AppRoles.Manager, AppRoles.Technician })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var r = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!r.Succeeded)
                        throw new Exception("Не вдалося створити роль " + role + ": " +
                            string.Join("; ", r.Errors.Select(e => e.Description)));
                }
            }

            await EnsureUserAsync(userManager, "manager@service.local", "Manager123!", AppRoles.Manager);
            await EnsureUserAsync(userManager, "tech@service.local", "Tech123!", AppRoles.Technician);
        }

        private static async Task EnsureUserAsync(
            UserManager<IdentityUser> userManager, string email, string password, string role)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var create = await userManager.CreateAsync(user, password);
                if (!create.Succeeded)
                    throw new Exception("Не вдалося створити " + email + ": " +
                        string.Join("; ", create.Errors.Select(e => e.Description)));
            }
            else
            {
                // користувач уже є: гарантуємо відомий пароль і підтверджений email
                user.EmailConfirmed = true;
                await userManager.UpdateAsync(user);
                await userManager.ResetAccessFailedCountAsync(user);
                await userManager.SetLockoutEndDateAsync(user, null);

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var reset = await userManager.ResetPasswordAsync(user, token, password);
                if (!reset.Succeeded)
                    throw new Exception("Не вдалося скинути пароль " + email + ": " +
                        string.Join("; ", reset.Errors.Select(e => e.Description)));
            }

            if (!await userManager.IsInRoleAsync(user, role))
                await userManager.AddToRoleAsync(user, role);
        }
    }
}