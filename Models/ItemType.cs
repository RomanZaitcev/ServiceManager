using System.ComponentModel.DataAnnotations;

namespace ServiceManager.Models
{
    public enum ItemType
    {
        [Display(Name = "Товар")]
        Product,

        [Display(Name = "Послуга")]
        Service
    }
}