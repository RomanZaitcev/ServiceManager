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

        [Required(ErrorMessage = "Вкажіть пристрій")]
        [StringLength(200)]
        [Display(Name = "Пристрій (модель)")]
        public string DeviceInfo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Опишіть несправність")]
        [StringLength(1000)]
        [Display(Name = "Опис несправності")]
        public string ProblemDescription { get; set; } = string.Empty;
    }

    public class OrderDetailsViewModel
    {
        public Order Order { get; set; } = null!;
        public List<SelectListItem> Technicians { get; set; } = new();
        public decimal PartsTotal => Order.UsedParts.Sum(p => p.QuantityUsed * p.PriceAtTime);
    }
}