using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projet_info_finale.Models;

namespace projet_info_finale.Controllers
{
    public class RestaurantController : Controller
    {
        private readonly ApplicationDbContext _context;
        public RestaurantController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: /Restaurant/Orders
        public async Task<IActionResult> Orders()
        {
            // Replace with the current restaurant's ID (e.g., from authentication or session)
            int restaurantId = 1;

            var orders = await _context.Orders
                .Where(o => o.RestaurantID == restaurantId)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .ToListAsync();

            return View(orders);
        }

        // POST: /Restaurant/UpdateOrderStatus
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            // Trim the status to ensure no extra spaces
            status = status.Trim();

            // Validate the status value against allowed enum values
            var allowedStatuses = new[] { "Pending", "Preparing", "Out for Delivery", "Completed", "Cancelled" };
            if (!allowedStatuses.Contains(status))
            {
                TempData["Error"] = "Invalid order status.";
                return RedirectToAction("Orders");
            }

            var order = await _context.Orders.FindAsync(orderId);

            if (order == null)
            {
                return NotFound();
            }

            order.OrderStatus = status;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Message"] = "Order status updated successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating OrderStatus: {ex.Message}");
                throw;
            }

            return RedirectToAction("Orders");
        }
    }
}
