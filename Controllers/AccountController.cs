using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Loads the login page
        public IActionResult Login()
        {
            return View();
        }

        // POST: Processes the login form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Check if the user exists in your custom database
                // Note: We are doing a direct match here. In a production app, you would hash the password!
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.PasswordHash == model.Password);

                if (user != null)
                {
                    // 2. Create the security ID card (Claims) for this user
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role), // 'Admin' or 'StandardUser'
                        new Claim("UserId", user.UserId.ToString()) // Store their ID to link to transactions later
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    // 3. Hand them the secure cookie
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Home"); // Send them to the dashboard
                }

                // If user isn't found
                ModelState.AddModelError("", "Invalid username or password.");
            }

            return View(model);
        }

        // GET: Logs the user out
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}