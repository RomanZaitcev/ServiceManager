using System.ComponentModel.DataAnnotations;

namespace ServiceManager.Models
{
    public enum OrderStatus
    {
        [Display(Name = "Прийнято")]
        Received,

        [Display(Name = "В черзі")]
        Queued,

        [Display(Name = "Діагностика")]
        Diagnostics,

        [Display(Name = "Очікує запчастини")]
        AwaitingParts,

        [Display(Name = "Відремонтовано")]
        Repaired,

        [Display(Name = "Видано")]
        Released
    }
}