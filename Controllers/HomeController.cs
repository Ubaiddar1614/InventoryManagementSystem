using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using System.Diagnostics;

namespace InventoryManagementSystem.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize] 
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var totalProducts = await _context.Products.CountAsync();

            var lowStockItems = await _context.Products
                .Where(p => p.StockQuantity <= p.ReorderLevel)
                .CountAsync();

            var totalInventoryValue = await _context.Products
                .SumAsync(p => p.Price * p.StockQuantity);

            var totalSalesRevenue = await _context.Transactions
                .Where(t => t.TransactionType == "Out")
                .SumAsync(t => t.TotalAmount);

            var dashboardData = new DashboardViewModel
            {
                TotalProducts = totalProducts,
                LowStockItems = lowStockItems,
                TotalInventoryValue = totalInventoryValue,
                TotalSalesRevenue = totalSalesRevenue
            };

            return View(dashboardData);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}