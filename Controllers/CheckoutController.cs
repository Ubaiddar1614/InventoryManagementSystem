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
            var transaction = await _context.Transactions
                .Include(t => t.Product)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transaction == null) return NotFound();

            var domain = $"{Request.Scheme}://{Request.Host}";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(transaction.Product.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = transaction.Product.Name,
                                Description = $"Transaction ID: {transaction.TransactionId}"
                            },
                        },
                        Quantity = transaction.Quantity,
                    },
                },
                Mode = "payment",
                // Notice this line! We are passing the transactionId back to our Success page
                SuccessUrl = domain + $"/Checkout/Success?session_id={{CHECKOUT_SESSION_ID}}&transactionId={transaction.TransactionId}",
                CancelUrl = domain + "/Checkout/Cancel",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        // GET: /Checkout/Success
        public async Task<IActionResult> Success(string session_id, int transactionId)
        {
            // 1. Find the transaction that Stripe just successfully processed
            var transaction = await _context.Transactions.FindAsync(transactionId);

            if (transaction != null)
            {
                // 2. Flip the switch in the database!
                transaction.PaymentStatus = "Paid";
                _context.Update(transaction);
                await _context.SaveChangesAsync();
            }

            return View();
        }

        // GET: /Checkout/Cancel
        public IActionResult Cancel()
        {
            return View();
        }
    }
}