namespace ServiceManager.Models
{
    public class ManagerDashboardViewModel
    {
        public int NewToday { get; set; }
        public int InWork { get; set; }
        public int Queued { get; set; }
        public decimal CashMonth { get; set; }
        public List<Order> Recent { get; set; } = new();
    }

    public class TechnicianDashboardViewModel
    {
        public int Queued { get; set; }
        public int InWork { get; set; }
        public int AwaitingParts { get; set; }
        public int DoneMonth { get; set; }
        public List<Order> Recent { get; set; } = new();
    }
}