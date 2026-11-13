using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceManager.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Вкажіть назву")]
        [StringLength(150)]
        [Display(Name = "Назва")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть артикул")]
        [StringLength(50)]
        [Display(Name = "Артикул")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Тип")]
        public ItemType ItemType { get; set; } = ItemType.Product;

        // Для послуг залишок не використовується (завжди 0)
        [Range(0, int.MaxValue, ErrorMessage = "Кількість не може бути від'ємною")]
        [Display(Name = "Залишок")]
        public int StockQuantity { get; set; }

        [Range(0.01, 1000000, ErrorMessage = "Ціна має бути від 0.01 до 1 000 000")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Ціна (грн)")]
        public decimal Price { get; set; }
    }
}