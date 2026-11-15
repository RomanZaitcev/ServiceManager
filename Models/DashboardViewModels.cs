namespace ServiceManager.Models
{
    public class HomeDashboardViewModel
    {
        public bool IsManager { get; set; }
        public bool IsTechnician { get; set; }

        public int NewCount { get; set; }
        public int ActiveCount { get; set; }
        public int AwaitingPartsCount { get; set; }
        public int DoneMonthCount { get; set; }
        public decimal CashMonth { get; set; }

        public List<Order> Recent { get; set; } = new();
    }
}