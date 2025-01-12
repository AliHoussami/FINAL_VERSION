using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using projet_info_finale.Models;
using System.Security.Claims;

namespace projet_info_finale.Controllers
{
    [Authorize]  // Add authorization
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StaffController> _logger;

        public StaffController(ApplicationDbContext context, ILogger<StaffController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Authorize(Roles = "KitchenStaff")]
        public IActionResult KitchenPanel()
        {
            try
            {
                _logger.LogInformation($"User {User.Identity.Name} accessing KitchenPanel");
                _logger.LogInformation($"User roles: {string.Join(", ", User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value))}");

                var orders = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                    .Where(o => o.OrderStatus == "Pending" || 
                              o.OrderStatus == "Preparing")
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                _logger.LogInformation($"Found {orders.Count} orders for kitchen panel");
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in KitchenPanel: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }

        [Authorize(Roles = "Driver")]
        public IActionResult DriverPanel()
        {
            try
            {
                _logger.LogInformation($"Driver {User.Identity.Name} accessing DriverPanel");
                _logger.LogInformation($"User roles: {string.Join(", ", User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value))}");

                var orders = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                    .Where(o => o.OrderStatus == "Out for Delivery")
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                _logger.LogInformation($"Found {orders.Count} orders for driver panel");
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DriverPanel: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        [Authorize(Roles = "KitchenStaff")]
        public IActionResult UpdateOrderStatus(int orderId, string status)
        {
            try
            {
                var order = _context.Orders.Find(orderId);
                if (order != null)
                {
                    order.OrderStatus = status;
                    _context.SaveChanges();
                }
                return RedirectToAction("KitchenPanel");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order status: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Driver")]
        public IActionResult UpdateDeliveryStatus(int orderId, string status)
        {
            try
            {
                var order = _context.Orders.Find(orderId);
                if (order != null)
                {
                    order.OrderStatus = status;
                    _context.SaveChanges();
                }
                return RedirectToAction("DriverPanel");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating delivery status: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }
    }
}
