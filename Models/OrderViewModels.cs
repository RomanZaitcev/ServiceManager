using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ServiceManager.Models
{
    public class CreateOrderViewModel
    {
        [Required(ErrorMessage = "Вкажіть ПІБ")]
        [StringLength(150)]
        [Display(Name = "ПІБ клієнта")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть телефон")]
        [Phone(ErrorMessage = "Некоректний формат телефону")]
        [StringLength(20)]
        [Display(Name = "Телефон")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Адреса")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Вкажіть тип пристрою")]
        [StringLength(50)]
        [Display(Name = "Тип пристрою")]
        public string DeviceType { get; set; } = string.Empty;

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
        [Display(Name = "Несправність (зі слів клієнта)")]
        public string ProblemDescription { get; set; } = string.Empty;
    }

    public class OrderDetailsViewModel
    {
        public Order Order { get; set; } = null!;
        public List<SelectListItem> Technicians { get; set; } = new();
        public List<InventoryItem> InventoryItems { get; set; } = new();
        public decimal PartsTotal => Order.UsedParts.Sum(p => p.QuantityUsed * p.PriceAtTime);
    }
}