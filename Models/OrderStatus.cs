using System.ComponentModel.DataAnnotations;

namespace ServiceManager.Models
{
    public enum OrderStatus
    {
        [Display(Name = "Прийнято")]
        Received,

        [Display(Name = "У черзі")]
        Queued,

        [Display(Name = "В роботі")]
        Diagnostics,

        [Display(Name = "Очікує запчастин")]
        AwaitingParts,

        [Display(Name = "Виконано")]
        Repaired,

        [Display(Name = "Видано")]
        Released
    }
}