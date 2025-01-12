using projet_info_finale.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
            if (!ModelState.IsValid)
            {
                ViewBag.ActiveTab = "login";
                return View("LoginSignup", new LoginSignupModel { Login = model });
            }

            try
            {
                // Fetch the user from the database by username
                var user = _context.Users.FirstOrDefault(u => u.Username == model.Username);

                // Check if the user exists and the password is valid
                if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    ViewBag.ActiveTab = "login";
                    return View("LoginSignup", new LoginSignupModel { Login = model });
                }

                // Set up authentication claims
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()), // UserID claim
            new Claim(ClaimTypes.Name, user.Username) // Username claim
        };

                // Create claims identity and sign the user in
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                // Redirect to the home page after successful login
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // Log the error and display a generic error message
                _logger.LogError($"Error during login: {ex.Message}");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
            }

            // Return to the login page with an active tab
            ViewBag.ActiveTab = "login";
            return View("LoginSignup", new LoginSignupModel { Login = model });
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
                    UserType = UserType.Customer // Use enum instead of string
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
                UserType = UserType.Customer, // Use enum instead of string
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdminLogin(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ActiveTab = "login"; // Set active tab to login
                return View("LoginSignup", new LoginSignupModel { Login = model });
            }

            try
            {
                // Check if the user exists and is an admin
                var adminUser = _context.Users.FirstOrDefault(u => u.Username == model.Username && u.UserType == UserType.Admin);

                if (adminUser == null || !VerifyPassword(model.Password, adminUser.PasswordHash))
                {
                    ModelState.AddModelError(string.Empty, "Invalid admin username or password.");
                    ViewBag.ActiveTab = "login"; // Set active tab to login
                    return View("LoginSignup", new LoginSignupModel { Login = model });
                }

                // Successful admin login
                // You can set admin-specific logic here if needed, like redirecting to an Admin Dashboard
                return RedirectToAction("AdminDashboard", "Admin"); // Assuming there's an AdminDashboard action
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during admin login: {ex.Message}");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
            }

            ViewBag.ActiveTab = "login"; // Set active tab to login
            return View("LoginSignup", new LoginSignupModel { Login = model });
        }        
    }
}
