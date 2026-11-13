using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServiceManager.Models;

namespace ServiceManager.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<OrderPart> OrderParts { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // обов'язково, інакше зламаються таблиці Identity

            // Статус зберігаємо як текст, а не число (зручніше читати в БД)
            builder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion<string>();

            // Телефон клієнта унікальний (за ним шукаємо клієнта при створенні замовлення)
            builder.Entity<Client>()
                .HasIndex(c => c.Phone)
                .IsUnique();

            // Код запчастини унікальний
            builder.Entity<InventoryItem>()
                .HasIndex(i => i.Code)
                .IsUnique();

            // Не можна видалити запчастину зі складу, якщо вона вже використана в ремонті
            builder.Entity<OrderPart>()
                .HasOne(op => op.InventoryItem)
                .WithMany()
                .HasForeignKey(op => op.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}