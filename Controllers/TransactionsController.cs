using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using System.Security.Claims; 

namespace InventoryManagementSystem.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Transactions
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Transactions.Include(t => t.Product).Include(t => t.User);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Transactions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var transaction = await _context.Transactions
                .Include(t => t.Product)
                .Include(t => t.User)
                .FirstOrDefaultAsync(m => m.TransactionId == id);

            if (transaction == null) return NotFound();

            return View(transaction);
        }

        // GET: Transactions/Create
        public IActionResult Create()
        {
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name");
            return View();
        }

        // POST: Transactions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TransactionId,ProductId,TransactionType,Quantity")] Transaction transaction)
        {
            // Bypass paranoid validation
            ModelState.Remove("Product");
            ModelState.Remove("User");
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(transaction.ProductId);

                if (product == null) return NotFound();

                // 1. Math and Dates
                transaction.TotalAmount = product.Price * transaction.Quantity;
                transaction.TransactionDate = DateTime.Now;

                // 2. Read the logged-in user's ID from their secure cookie!
                var loggedInUserId = User.FindFirst("UserId")?.Value;
                if (loggedInUserId != null)
                {
                    transaction.UserId = int.Parse(loggedInUserId);
                }

                // 3. Stock Logic
                if (transaction.TransactionType?.ToLower() == "out")
                {
                    if (transaction.Quantity > product.StockQuantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock! Only {product.StockQuantity} left in inventory.");
                        ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", transaction.ProductId);
                        return View(transaction);
                    }
                    product.StockQuantity -= transaction.Quantity;
                }
                else if (transaction.TransactionType?.ToLower() == "in")
                {
                    product.StockQuantity += transaction.Quantity;
                }

                _context.Add(transaction);
                _context.Update(product);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", transaction.ProductId);
            return View(transaction);
        }

        // GET: Transactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return NotFound();

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", transaction.ProductId);
            return View(transaction);
        }

        // POST: Transactions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TransactionId,ProductId,TransactionType,Quantity")] Transaction transaction)
        {
            if (id != transaction.TransactionId) return NotFound();

            ModelState.Remove("Product");
            ModelState.Remove("User");
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                try
                {
                    var originalTransaction = await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.TransactionId == id);
                    if (originalTransaction == null) return NotFound();

                    // Reverse old math
                    var product = await _context.Products.FindAsync(originalTransaction.ProductId);
                    if (product != null)
                    {
                        if (originalTransaction.TransactionType?.ToLower() == "out") product.StockQuantity += originalTransaction.Quantity;
                        else if (originalTransaction.TransactionType?.ToLower() == "in") product.StockQuantity -= originalTransaction.Quantity;
                    }

                    // Apply new math
                    if (transaction.TransactionType?.ToLower() == "out")
                    {
                        if (transaction.Quantity > product.StockQuantity)
                        {
                            ModelState.AddModelError("Quantity", $"Insufficient stock! Only {product.StockQuantity} left in inventory.");
                            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", transaction.ProductId);
                            return View(transaction);
                        }
                        product.StockQuantity -= transaction.Quantity;
                    }
                    else if (transaction.TransactionType?.ToLower() == "in")
                    {
                        product.StockQuantity += transaction.Quantity;
                    }

                    // Reapply calculated fields AND preserve the original User who made the transaction
                    transaction.TotalAmount = product.Price * transaction.Quantity;
                    transaction.TransactionDate = originalTransaction.TransactionDate;
                    transaction.UserId = originalTransaction.UserId;

                    _context.Update(product);
                    _context.Update(transaction);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TransactionExists(transaction.TransactionId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", transaction.ProductId);
            return View(transaction);
        }

        // GET: Transactions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var transaction = await _context.Transactions
                .Include(t => t.Product)
                .Include(t => t.User)
                .FirstOrDefaultAsync(m => m.TransactionId == id);

            if (transaction == null) return NotFound();

            return View(transaction);
        }

        // POST: Transactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);

            if (transaction != null)
            {
                var product = await _context.Products.FindAsync(transaction.ProductId);

                if (product != null)
                {
                    if (transaction.TransactionType?.ToLower() == "out") product.StockQuantity += transaction.Quantity;
                    else if (transaction.TransactionType?.ToLower() == "in") product.StockQuantity -= transaction.Quantity;

                    _context.Update(product);
                }

                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TransactionExists(int id)
        {
            return _context.Transactions.Any(e => e.TransactionId == id);
        }
    }
}