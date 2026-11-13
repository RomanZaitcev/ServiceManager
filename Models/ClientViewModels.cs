namespace ServiceManager.Models
{
    public class ClientListItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
    }
}