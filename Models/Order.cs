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

        [Display(Name = "Майстер")]
        public string? TechnicianId { get; set; }
        public IdentityUser? Technician { get; set; }

        [StringLength(50)]
        [Display(Name = "Тип пристрою")]
        public string? DeviceType { get; set; }

        // Бренд/модель (поле лишається під старою назвою, щоб не втратити дані)
        [Required(ErrorMessage = "Вкажіть бренд/модель")]
        [StringLength(200)]
        [Display(Name = "Бренд/Модель")]
        public string DeviceInfo { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Серійний номер")]
        public string? SerialNumber { get; set; }

        [StringLength(300)]
        [Display(Name = "Комплектація")]
        public string? Accessories { get; set; }

        [StringLength(300)]
        [Display(Name = "Зовнішній стан")]
        public string? ExternalCondition { get; set; }

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