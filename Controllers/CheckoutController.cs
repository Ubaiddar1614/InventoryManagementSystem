using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using InventoryManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession(int transactionId)
        {
            // 1. Find the transaction and include the Product details
            var transaction = await _context.Transactions
                .Include(t => t.Product)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transaction == null) return NotFound();

            // IMPORTANT: Make sure this matches your exact localhost port from your browser!
            var domain = "https://localhost:7198";

            // 2. Build the Stripe Invoice
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            // Stripe does all math in CENTS. So $50.00 = 5000. 
                            // We multiply your price by 100 to convert it.
                            UnitAmount = (long)(transaction.Product.Price * 100),
                            Currency = "usd", // Or "pkr" if Stripe supports it in your test region!
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = transaction.Product.Name,
                                Description = $"Transaction ID: {transaction.TransactionId}"
                            },
                        },
                        // We pass the exact quantity from your database
                        Quantity = transaction.Quantity,
                    },
                },
                Mode = "payment",
                // Where Stripe sends the user after they pay (or cancel)
                SuccessUrl = domain + "/Checkout/Success?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = domain + "/Checkout/Cancel",
            };

            // 3. Generate the secure URL and Redirect the user
            var service = new SessionService();
            Session session = service.Create(options);

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        // GET: /Checkout/Success
        public IActionResult Success(string session_id)
        {
            // If you wanted to be super advanced, you would verify the session_id here 
            // and mark the transaction as "Paid" in your DB. 
            // For now, we just show a success screen!
            return View();
        }

        // GET: /Checkout/Cancel
        public IActionResult Cancel()
        {
            return View();
        }
    }
}