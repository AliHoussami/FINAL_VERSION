using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projet_info_finale.Models;

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

            var cartItem = new CartItem
            {
                MenuItemID = menuItemId,
                Customizations = customizations,
                Quantity = quantity,
                TotalPrice = menuItem.Price * quantity
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
            // Fetch the current user's ID (Replace with actual authenticated user logic)
            int userId = 1; // Assume 1 for testing, replace with actual logged-in user's ID
            int restaurantId = 1; // Replace with logic to fetch restaurant ID based on cart items

            // Fetch cart items
            var cartItems = await _context.CartItems.Include(ci => ci.MenuItem).ToListAsync();

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

                // Simulate successful card payment
                TempData["Message"] = $"Payment successful! Your total amount of {totalAmount:C} has been charged to your card.";
            }
            else
            {
                // Simulate cash on delivery
                TempData["Message"] = $"Order placed successfully! Please pay {totalAmount:C} on delivery.";
            }

            // Create a new order
            var order = new Order
            {
                UserID = userId,
                RestaurantID = restaurantId,
                OrderStatus = "Pending", // Initial order status
                TotalPrice = totalAmount,
                OrderItems = new List<OrderItem>()
            };

            // Add order items
            foreach (var cartItem in cartItems)
            {
                var orderItem = new OrderItem
                {
                    MenuItemID = cartItem.MenuItemID,
                    Quantity = cartItem.Quantity,
                    ItemPrice = cartItem.TotalPrice
                };

                order.OrderItems.Add(orderItem);
            }

            _context.Orders.Add(order);

            // Clear the cart
            _context.CartItems.RemoveRange(cartItems);

            // Save changes
            await _context.SaveChangesAsync();

            // Redirect to confirmation page
            return RedirectToAction("OrderConfirmation", new { orderId = order.OrderID });
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
