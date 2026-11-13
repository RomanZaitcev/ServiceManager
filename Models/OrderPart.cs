using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceManager.Models
{
    public class OrderPart
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int InventoryItemId { get; set; }
        public InventoryItem? InventoryItem { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Кількість має бути більшою за 0")]
        [Display(Name = "Кількість")]
        public int QuantityUsed { get; set; }

        // Ціна на момент ремонту (фіксується окремо від поточної ціни на складі)
        [Range(0.01, 1000000)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Ціна на момент ремонту")]
        public decimal PriceAtTime { get; set; }
    }
}