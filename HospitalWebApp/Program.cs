using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using HospitalWebApp.Services;


var builder = WebApplication.CreateBuilder(args);

// Ensure environment variables and user secrets can override sensitive values.
builder.Configuration.AddEnvironmentVariables();

// Configure the SQLite database connection.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=Hospital.db";

// Register the EF Core context for the application database.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Configure identity and user authentication with stronger defaults.
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.User.RequireUniqueEmail = true;

    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
});

// Add support for controllers, views, and Razor Pages.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); 
// Register application services.
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddHttpClient();
builder.Services.AddTransient<IPaymentService, PaymentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; style-src 'self' https://fonts.googleapis.com https://cdnjs.cloudflare.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data: https://immaculatehearthospitalkereita.org https://cdn-icons-png.flaticon.com; connect-src 'self'; frame-ancestors 'none';";
    await next();
});
app.UseStaticFiles(); // Serve static files from wwwroot
app.UseCookiePolicy();
app.UseRouting();

app.UseAuthentication(); // Run authentication before authorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // Enable Razor Pages, including Identity pages

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // dbContext.Database.EnsureDeleted(); // UNCOMMENT to wipe DB every time you restart
    dbContext.Database.EnsureCreated();

    async Task EnsureRoleAsync(string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    async Task EnsureUserAsync(string email, string password, string? role = null, string? fullName = null)
    {
        if (await userManager.FindByEmailAsync(email) != null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName ?? email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded && role != null)
        {
            await userManager.AddToRoleAsync(user, role);
        }

        if (!result.Succeeded)
        {
            Console.WriteLine($"Failed to create {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    await EnsureRoleAsync("Admin");
    await EnsureRoleAsync("Doctor");
    await EnsureRoleAsync("Accounts");
    await EnsureRoleAsync("Cashier");
    await EnsureRoleAsync("Reception");
    await EnsureRoleAsync("Nurse");
    await EnsureRoleAsync("LabTech");
    await EnsureRoleAsync("Pharmacy");
    await EnsureRoleAsync("Patient");

    await EnsureUserAsync("ihearthospitalkereita@gmail.com", "Admin@2025!", "Admin", "Hospital Admin");
    await EnsureUserAsync("doctor1@immaculateheart.org", "Doctor@2025!", "Doctor", "Dr. Faith Njuguna");
    await EnsureUserAsync("nurse1@immaculateheart.org", "Nurse@2025!", "Nurse", "Nurse Mercy Mwangi");
    await EnsureUserAsync("labtech1@immaculateheart.org", "LabTech@2025!", "LabTech", "Lab Technician James");
    await EnsureUserAsync("pharmacy1@immaculateheart.org", "Pharmacy@2025!", "Pharmacy", "Pharmacy Manager Jane");
    await EnsureUserAsync("reception@immaculateheart.org", "Reception@2025!", "Reception", "Reception Desk");
    await EnsureUserAsync("patient1@immaculateheart.org", "Patient@2025!", null, "Jane Mwangi");
    await EnsureUserAsync("accounts@immaculateheart.org", "Accounts@2025!", "Accounts", "Sarah Accounts Manager");
}

app.Run();
