using System.ComponentModel.DataAnnotations;

namespace ServiceManager.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Вкажіть ПІБ")]
        [StringLength(150)]
        [Display(Name = "ПІБ")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть телефон")]
        [Phone(ErrorMessage = "Некоректний формат телефону")]
        [StringLength(20)]
        [Display(Name = "Телефон")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Адреса")]
        public string? Address { get; set; }

        public List<Order> Orders { get; set; } = new();
    }
}