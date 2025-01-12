using Microsoft.AspNetCore.Authentication.Cookies;
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

            // Update connection string with increased timeouts and optimized settings
            var connectionString = "Server=localhost;" +
                                  "Database=projet;" +
                                  "User=root;" +
                                  "Password=Habibah7rm;" +
                                  "Port=3306;" +
                                  "Connection Timeout=60;" +
                                  "Default Command Timeout=60;" +
                                  "SslMode=none;" +
                                  "AllowPublicKeyRetrieval=True;" +
                                  "Pooling=true;" +
                                  "MinimumPoolSize=5;" +
                                  "MaximumPoolSize=100;" +
                                  "ConnectionLifeTime=30;" +
                                  "ConnectionReset=true";

            // Register ApplicationDbContext with MySQL with optimized settings
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString),
                    mySqlOptions =>
                    {
                        mySqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null
                        );
                        mySqlOptions.CommandTimeout(60);
                        mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        mySqlOptions.MigrationsAssembly(typeof(Program).Assembly.FullName);
                    }
                );
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Update Authentication settings
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/LoginSignup";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.Cookie.Name = "LebEatsAuth";
                    options.Cookie.HttpOnly = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(24);
                    options.SlidingExpiration = true;
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = context =>
                        {
                            if (context.Request.Path.StartsWithSegments("/Staff") && context.Response.StatusCode == 200)
                            {
                                context.Response.StatusCode = 401;
                            }
                            else
                            {
                                context.Response.Redirect(context.RedirectUri);
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            // Add Authorization
            builder.Services.AddAuthorization();

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles(); // Make sure this is before authentication
            app.UseRouting();    // Make sure this is before authentication

            // Make sure these are in the correct order
            app.UseAuthentication();
            app.UseAuthorization();

            // Update the default route to include area
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=LoginSignup}/{id?}");

            app.Run();
        }
    }
}
