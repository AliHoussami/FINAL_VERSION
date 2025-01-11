using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projet_info_finale.Models;
using System.Diagnostics;

namespace projet_info_finale.Controllers
{
    public class HomeController : Controller
    {
        //private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Home/Index
        public async Task<IActionResult> Index()
        {
            var restaurants = await _context.Restaurants.ToListAsync();
            return View(restaurants);
        }

        // GET: /Home/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.MenuItems)
                .FirstOrDefaultAsync(r => r.RestaurantID == id);

            if (restaurant == null)
            {
                return NotFound();
            }

            return View(restaurant);
        }
    }
}
