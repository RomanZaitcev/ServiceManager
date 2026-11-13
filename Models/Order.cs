using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ServiceManager.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Display(Name = "Клієнт")]
        public int ClientId { get; set; }
        public Client? Client { get; set; }

        // Id користувача (майстра) з таблиці AspNetUsers; string, бо так в Identity
        [Display(Name = "Майстер")]
        public string? TechnicianId { get; set; }
        public IdentityUser? Technician { get; set; }

        [Required(ErrorMessage = "Вкажіть пристрій")]
        [StringLength(200)]
        [Display(Name = "Пристрій")]
        public string DeviceInfo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Опишіть несправність")]
        [StringLength(1000)]
        [Display(Name = "Опис несправності")]
        public string ProblemDescription { get; set; } = string.Empty;

        [Display(Name = "Статус")]
        public OrderStatus Status { get; set; } = OrderStatus.Received;

        [StringLength(1000)]
        [Display(Name = "Коментар")]
        public string? Comment { get; set; }

        [Display(Name = "Створено")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Оновлено")]
        public DateTime? UpdatedAt { get; set; }

        public List<OrderPart> UsedParts { get; set; } = new();
    }
}