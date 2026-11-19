using System.ComponentModel.DataAnnotations;

namespace ServiceManager.Models
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Вкажіть email")]
        [EmailAddress(ErrorMessage = "Некоректний email")]
        [Display(Name = "Email (логін)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть пароль")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль щонайменше 8 символів")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Роль")]
        public string Role { get; set; } = AppRoles.Technician;
    }

    public class UserListItem
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

}