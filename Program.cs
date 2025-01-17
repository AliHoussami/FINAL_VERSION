using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using projet_info_finale.Models;

namespace projet_info_finale
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Logging.AddConsole(); // Log to the console
            builder.Logging.AddDebug();

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSession();
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // Register ApplicationDbContext with MySQL
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString)
                )
            );

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
.AddCookie(options =>
{
    options.LoginPath = "/Account/LoginSignup"; // Redirect to login
    options.LogoutPath = "/Account/Logout"; // Redirect to logout
    options.ExpireTimeSpan = TimeSpan.FromDays(7); // Set cookie expiration to 7 days
    options.SlidingExpiration = true; // Sliding expiration
})
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    googleOptions.CallbackPath = "/signin-google"; // Matches Google Console redirect URI
});

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            //builder.Services.AddScoped<IPasswordHasher<Users>, PasswordHasher<User>>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=LoginSignUp}/{id?}");

            app.Run();
        }
    }
}