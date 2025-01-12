using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projet_info_finale.Models;
using System.Security.Claims;

namespace projet_info_finale.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Cart/Index
        public async Task<IActionResult> Index()
        {
            var cartItems = await _context.CartItems
                .Include(ci => ci.MenuItem)
                .ToListAsync();

            return View(cartItems);
        }

        // GET: /Cart/Add/{menuItemId}using Microsoft.EntityFrameworkCore;

        public async Task<IActionResult> Add(int menuItemId)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);

            if (menuItem == null)
            {
                return NotFound();
            }

            return View(menuItem);
        }

        // POST: /Cart/Add
        [HttpPost]
        public async Task<IActionResult> Add(int menuItemId, string customizations, int quantity)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);

            if (menuItem == null)
            {
                return NotFound();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "You must be logged in to add items to the cart.";
                return RedirectToAction("LoginSignup", "Account");
            }

            int parsedUserId = int.Parse(userId);

            var cartItem = new CartItem
            {
                MenuItemID = menuItemId,
                Customizations = customizations,
                Quantity = quantity,
                TotalPrice = menuItem.Price * quantity,
                UserID = parsedUserId // Ensure UserID is set
            };

            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }


        // POST: /Cart/Remove/{cartItemId}
        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);

            if (cartItem == null)
            {
                return NotFound();
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // POST: /Cart/Checkout
        [HttpPost]
        public async Task<IActionResult> Checkout(string paymentMethod, string? cardNumber, string? expiryDate, string? cvv)
        {
            // Fetch the current user's ID from authentication claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "You must be logged in to place an order.";
                return RedirectToAction("LoginSignup", "Account");
            }

            int parsedUserId = int.Parse(userId);

            // Fetch cart items for the logged-in user
            var cartItems = await _context.CartItems
                .Where(ci => ci.UserID == parsedUserId) // Ensure cart items belong to the logged-in user
                .Include(ci => ci.MenuItem)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Message"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            decimal totalAmount = cartItems.Sum(ci => ci.TotalPrice);

            // Simulate payment logic
            if (paymentMethod == "Card")
            {
                if (string.IsNullOrWhiteSpace(cardNumber) || string.IsNullOrWhiteSpace(expiryDate) || string.IsNullOrWhiteSpace(cvv))
                {
                    TempData["Message"] = "Card details are incomplete.";
                    return RedirectToAction("ProceedToPayment");
                }

                TempData["Message"] = $"Payment successful! Your total amount of {totalAmount:C} has been charged to your card.";
            }
            else
            {
                TempData["Message"] = $"Order placed successfully! Please pay {totalAmount:C} on delivery.";
            }

            try
            {
                var order = new Order
                {
                    UserID = parsedUserId, // Set the correct UserID
                    RestaurantID = 1, // Replace with actual logic to determine the restaurant ID
                    OrderStatus = OrderStatus.Pending, // Changed from string to enum
                    TotalPrice = totalAmount,
                    OrderItems = cartItems.Select(ci => new OrderItem
                    {
                        MenuItemID = ci.MenuItemID,
                        Quantity = ci.Quantity,
                        ItemPrice = ci.TotalPrice,
                        Customization = ci.Customizations ?? string.Empty
                    }).ToList()
                };

                _context.Orders.Add(order);

                // Clear the user's cart
                _context.CartItems.RemoveRange(cartItems);

                // Save changes
                await _context.SaveChangesAsync();

                // Redirect to confirmation page
                return RedirectToAction("OrderConfirmation", new { orderId = order.OrderID });
            }
            catch (Exception ex)
            {
                // Log the error
                TempData["Error"] = "An error occurred while processing your order.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Restaurant)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
        public async Task<IActionResult> ProceedToPayment()
        {
            var cartItems = await _context.CartItems.Include(ci => ci.MenuItem).ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Message"] = "Your cart is empty. Please add items before proceeding to payment.";
                return RedirectToAction("Index");
            }

            // Calculate the total amount
            decimal totalAmount = cartItems.Sum(ci => ci.TotalPrice);

            // Pass the total amount to the Payment view
            ViewData["TotalAmount"] = totalAmount;

            return View("Payment");
        }

    }
}
