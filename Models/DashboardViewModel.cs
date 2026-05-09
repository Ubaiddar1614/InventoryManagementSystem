namespace InventoryManagementSystem.Models
{
    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int LowStockItems { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public decimal TotalSalesRevenue { get; set; }
    }
}