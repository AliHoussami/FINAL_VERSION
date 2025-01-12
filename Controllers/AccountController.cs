using projet_info_finale.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Add this at the top with other using statements

namespace projet_info_finale.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult LoginSignup(bool isAdmin = false)
        {
            ViewBag.IsAdmin = isAdmin;
            ViewBag.ActiveTab = isAdmin ? "login" : string.Empty; // Default to login for Admin mode
            return View(new LoginSignupModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            try
            {
                _logger.LogInformation($"Starting login process for username: {model.Username}");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid");
                    return View("LoginSignup", new LoginSignupModel { Login = model });
                }

                // More efficient query
                var user = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Username == model.Username)
                    .Select(u => new { 
                        u.UserID, 
                        u.Username, 
                        u.PasswordHash, 
                        u.UserType 
                    })
                    .FirstOrDefaultAsync();
                
                if (user == null)
                {
                    _logger.LogWarning($"No user found with username: {model.Username}");
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return View("LoginSignup", new LoginSignupModel { Login = model });
                }

                _logger.LogInformation($"Found user with type: {user.UserType}");

                if (!VerifyPassword(model.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"Invalid password for username: {model.Username}");
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return View("LoginSignup", new LoginSignupModel { Login = model });
                }

                // Create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.UserType)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation($"Successfully signed in user. Redirecting based on type: {user.UserType}");

                // Explicit redirect logic with logging
                IActionResult redirectResult;
                switch (user.UserType.Trim())  // Add Trim() to handle any whitespace
                {
                    case "Driver":
                        _logger.LogInformation("Redirecting to Driver Panel");
                        redirectResult = RedirectToAction("DriverPanel", "Staff");
                        break;
                    case "KitchenStaff":
                        _logger.LogInformation("Redirecting to Kitchen Panel");
                        redirectResult = RedirectToAction("KitchenPanel", "Staff");
                        break;
                    case "Admin":
                        _logger.LogInformation("Redirecting to Admin Panel");
                        redirectResult = RedirectToAction("RestaurantManagement", "Admin");
                        break;
                    default:
                        _logger.LogInformation("Redirecting to Home Index");
                        redirectResult = RedirectToAction("Index", "Home");
                        break;
                }

                _logger.LogInformation($"Redirect result type: {redirectResult.GetType().Name}");
                return redirectResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception during login: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred during login.");
                return View("LoginSignup", new LoginSignupModel { Login = model });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Signup(SignupModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ActiveTab = "signup";
                return View("LoginSignup", new LoginSignupModel { Signup = model });
            }

            try
            {
                // Hash the password
                var passwordHash = HashPassword(model.Password);

                // Create and save the new user
                var newUser = new Users
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    Username = model.Username,
                    Phone = model.Phone ?? "Not provided",
                    Address = model.Address ?? "Not provided",
                    City = model.City ?? "Not provided",
                    PostalCode = model.PostalCode ?? "Not provided",
                    UserType = "Customer"
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                // Redirect to Home/Index after successful signup
                return RedirectToAction("Index", "Home");
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error saving user: {ex.Message}");
                ModelState.AddModelError(string.Empty, "An error occurred while saving your data.");
                ViewBag.ActiveTab = "signup";
                return View("LoginSignup", new LoginSignupModel { Signup = model });
            }
        }

        // Google Login action
        [HttpGet("LoginWithGoogle")]
        public IActionResult LoginWithGoogle()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // Google Signup action
        [HttpGet("SignupWithGoogle")]
        public IActionResult SignupWithGoogle()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // Google response callback after authentication
        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            _logger.LogInformation("Entering GoogleResponse method");

            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (result == null || !result.Succeeded)
            {
                _logger.LogWarning("Google authentication failed or returned null info.");
                return RedirectToAction("LoginSignup");
            }

            var email = result.Principal.FindFirstValue(ClaimTypes.Email) ?? "unknown";
            var firstName = result.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "Unknown";
            var lastName = result.Principal.FindFirstValue(ClaimTypes.Surname) ?? "User";

            _logger.LogInformation($"Retrieved Google user info - Email: {email}, First Name: {firstName}, Last Name: {lastName}");

            var existingUser = _context.Users.FirstOrDefault(u => u.Email == email);
            if (existingUser != null)
            {
                _logger.LogInformation("User already exists in the database. Redirecting to homepage.");
                return RedirectToAction("Index", "Home");
            }

            var newUser = new Users
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                UserType = "Customer",
                Address = "Not provided",
                City = "Not provided",
                PostalCode = "Not provided",
                Phone = "Not provided",
                PasswordHash = "GoogleOAuth",
                Username = firstName + lastName
            };

            _context.Users.Add(newUser);
            _logger.LogInformation("Attempting to save new user to database.");

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("User saved successfully.");
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error saving user to the database: {ex.Message}");
                ModelState.AddModelError(string.Empty, "An error occurred while saving the user data.");
                return RedirectToAction("LoginSignup");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LoginSignup");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private bool VerifyPassword(string inputPassword, string storedHashedPassword)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedInput = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(inputPassword)));
                return hashedInput == storedHashedPassword;
            }
        }
    }
}
