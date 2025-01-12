using Microsoft.AspNetCore.Mvc;
using projet_info_finale.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace projet_info_finale.Controllers
{
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StaffController> _logger;

        public StaffController(ApplicationDbContext context, ILogger<StaffController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> StaffPanel(int staffId)
        {
            try
            {
                var staff = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserID == staffId);

                if (staff == null)
                {
                    TempData["ErrorMessage"] = "Staff member not found.";
                    return RedirectToAction("StaffSelection");
                }

                ViewBag.StaffId = staffId;
                ViewBag.StaffName = $"{staff.FirstName} {staff.LastName}";
                ViewBag.StaffRole = staff.UserType;

                var orders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Restaurant)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                    .OrderByDescending(o => o.OrderID)
                    .Where(o =>
                        staff.UserType == UserType.KitchenStaff ?
                            (o.OrderStatus == OrderStatus.Pending || o.OrderStatus == OrderStatus.Preparing) :
                        staff.UserType == UserType.Driver ?
                            (o.OrderStatus == OrderStatus.Preparing || o.OrderStatus == OrderStatus.OutForDelivery) :
                        staff.UserType == UserType.Accepter ?
                            (o.OrderStatus == OrderStatus.Pending) :  // Show only new Pending orders to Accepter
                            false)
                    .ToListAsync();

                return View("StaffPanel", orders);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in StaffPanel: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the staff panel.";
                return RedirectToAction("StaffSelection");
            }
        }

        public async Task<IActionResult> StaffSelection()
        {
            try
            {
                var staffMembers = await _context.Users
                    .Where(u => u.UserType == UserType.Driver || 
                               u.UserType == UserType.KitchenStaff || 
                               u.UserType == UserType.Accepter)
                    .ToListAsync();

                if (!staffMembers.Any())
                {
                    TempData["ErrorMessage"] = "No staff members found.";
                    return RedirectToAction("LoginSignup", "Account");
                }

                return View(staffMembers);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in StaffSelection: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string newStatus)
        {
            try
            {
                // Normalize newStatus to match enum values
                if (!Enum.TryParse<OrderStatus>(newStatus.Replace(" ", ""), true, out var parsedStatus))
                {
                    return Json(new { success = false, message = "Invalid order status value." });
                }

                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                // Update the status and save changes
                order.OrderStatus = parsedStatus;
                _context.Entry(order).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Status updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order {orderId} status to {newStatus}: {ex.Message} | Inner Exception: {ex.InnerException?.Message}");
                return Json(new { success = false, message = $"An error occurred while updating the status: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessOrder(int orderId, string action)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                switch (action)
                {
                    case "accept":
                        order.OrderStatus = OrderStatus.Pending;  // Keep as Pending for KitchenStaff
                        break;
                    case "reject":
                        order.OrderStatus = OrderStatus.Cancelled;
                        break;
                    default:
                        return Json(new { success = false, message = "Invalid action." });
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Order {action}ed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing order {orderId}: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while processing the order." });
            }
        }

        public async Task<IActionResult> GetDriverOrderHistory(int driverId)
        {
            try
            {
                var completedOrders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Restaurant)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                    .Where(o => o.OrderStatus == OrderStatus.Completed)
                    .OrderByDescending(o => o.OrderID)
                    .ToListAsync();

                return PartialView("_DriverOrderHistory", completedOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching driver order history: {ex.Message}");
                return Json(new { success = false, message = "Failed to load order history." });
            }
        }
    }
}
