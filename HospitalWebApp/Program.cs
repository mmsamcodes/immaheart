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
builder.Services.AddScoped<PatientEncounterService>();

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
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; img-src 'self' data: https://immaculatehearthospitalkereita.org https://cdn-icons-png.flaticon.com; connect-src 'self'; frame-src 'self' https://www.google.com; frame-ancestors 'none';";
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
    await EnsurePharmacySchemaAsync(dbContext);
    await EnsureErpSchemaAsync(dbContext);

    async Task EnsureRoleAsync(string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    async Task EnsureUserAsync(string email, string password, string? role = null, string? fullName = null)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            if (role != null && !await userManager.IsInRoleAsync(existingUser, role))
            {
                var existingRoleResult = await userManager.AddToRoleAsync(existingUser, role);
                if (!existingRoleResult.Succeeded)
                {
                    Console.WriteLine($"Failed to assign {role} to {email}: {string.Join(", ", existingRoleResult.Errors.Select(e => e.Description))}");
                }
            }
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
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                Console.WriteLine($"Failed to assign {role} to {email}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
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
    await EnsureUserAsync("patient1@immaculateheart.org", "Patient@2025!", "Patient", "Jane Mwangi");
    await EnsureUserAsync("accounts@immaculateheart.org", "Accounts@2025!", "Accounts", "Sarah Accounts Manager");
}

static async Task EnsurePharmacySchemaAsync(ApplicationDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var closeConnection = connection.State != System.Data.ConnectionState.Open;
    if (closeConnection)
    {
        await connection.OpenAsync();
    }

    try
    {
        await EnsureColumnAsync(connection, "Drugs", "RequiresPrescription", "INTEGER NOT NULL DEFAULT 0");

        await using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = """
            CREATE TABLE IF NOT EXISTS PharmacyOrders (
                Id INTEGER NOT NULL CONSTRAINT PK_PharmacyOrders PRIMARY KEY AUTOINCREMENT,
                OrderNumber TEXT NOT NULL,
                PatientId TEXT NOT NULL,
                PlacedAt TEXT NOT NULL,
                Status TEXT NOT NULL,
                PatientNote TEXT NOT NULL,
                PrescriptionDocument BLOB NULL,
                PrescriptionContentType TEXT NULL,
                PatientEncounterId INTEGER NULL,
                BillingInvoiceId INTEGER NULL,
                PrescriberId TEXT NULL,
                PrescriptionText TEXT NULL,
                CONSTRAINT FK_PharmacyOrders_AspNetUsers_PatientId
                    FOREIGN KEY (PatientId) REFERENCES AspNetUsers (Id) ON DELETE RESTRICT,
                CONSTRAINT FK_PharmacyOrders_PatientEncounters_PatientEncounterId
                    FOREIGN KEY (PatientEncounterId) REFERENCES PatientEncounters (Id) ON DELETE SET NULL,
                CONSTRAINT FK_PharmacyOrders_BillingInvoices_BillingInvoiceId
                    FOREIGN KEY (BillingInvoiceId) REFERENCES BillingInvoices (Id) ON DELETE SET NULL,
                CONSTRAINT FK_PharmacyOrders_AspNetUsers_PrescriberId
                    FOREIGN KEY (PrescriberId) REFERENCES AspNetUsers (Id) ON DELETE SET NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_PharmacyOrders_OrderNumber ON PharmacyOrders (OrderNumber);
            CREATE INDEX IF NOT EXISTS IX_PharmacyOrders_PatientId ON PharmacyOrders (PatientId);
            CREATE INDEX IF NOT EXISTS IX_PharmacyOrders_Status ON PharmacyOrders (Status);
            CREATE TABLE IF NOT EXISTS PharmacyOrderItems (
                Id INTEGER NOT NULL CONSTRAINT PK_PharmacyOrderItems PRIMARY KEY AUTOINCREMENT,
                PharmacyOrderId INTEGER NOT NULL,
                DrugId INTEGER NOT NULL,
                DrugName TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                UnitPrice TEXT NOT NULL,
                RequiresPrescription INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT FK_PharmacyOrderItems_PharmacyOrders_PharmacyOrderId
                    FOREIGN KEY (PharmacyOrderId) REFERENCES PharmacyOrders (Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_PharmacyOrderItems_PharmacyOrderId
                ON PharmacyOrderItems (PharmacyOrderId);
            """;
        await schemaCommand.ExecuteNonQueryAsync();
        await EnsureColumnAsync(connection, "PharmacyOrderItems", "RequiresPrescription", "INTEGER NOT NULL DEFAULT 0");
    }
    finally
    {
        if (closeConnection)
        {
            await connection.CloseAsync();
        }
    }
}

static async Task EnsureErpSchemaAsync(ApplicationDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var closeConnection = connection.State != System.Data.ConnectionState.Open;
    if (closeConnection)
    {
        await connection.OpenAsync();
    }

    try
    {
        await EnsureColumnAsync(connection, "BillingInvoices", "PatientEncounterId", "INTEGER NULL");
        await EnsureColumnAsync(connection, "BillingItems", "PatientServiceId", "INTEGER NULL");
        await EnsureColumnAsync(connection, "BillingItems", "LabOrderId", "INTEGER NULL");
        await EnsureColumnAsync(connection, "LabOrders", "PatientEncounterId", "INTEGER NULL");
        await EnsureColumnAsync(connection, "QueueEntries", "PatientEncounterId", "INTEGER NULL");
        await EnsureColumnAsync(connection, "TriageEntries", "PatientEncounterId", "INTEGER NULL");
        await EnsureColumnAsync(connection, "PharmacyOrders", "PatientEncounterId", "INTEGER NULL");
        await EnsureColumnAsync(connection, "PharmacyOrders", "BillingInvoiceId", "INTEGER NULL");
        await EnsureColumnAsync(connection, "PharmacyOrders", "PrescriberId", "TEXT NULL");
        await EnsureColumnAsync(connection, "PharmacyOrders", "PrescriptionText", "TEXT NULL");

        await using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = """
            CREATE TABLE IF NOT EXISTS PatientEncounters (
                Id INTEGER NOT NULL CONSTRAINT PK_PatientEncounters PRIMARY KEY AUTOINCREMENT,
                VisitNumber TEXT NOT NULL,
                PatientId TEXT NOT NULL,
                Status TEXT NOT NULL,
                CurrentDepartment TEXT NOT NULL,
                Priority TEXT NOT NULL,
                StartedAt TEXT NOT NULL,
                LastUpdatedAt TEXT NOT NULL,
                ClosedAt TEXT NULL,
                AssignedClinicianId TEXT NULL,
                CONSTRAINT FK_PatientEncounters_AspNetUsers_PatientId
                    FOREIGN KEY (PatientId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
                CONSTRAINT FK_PatientEncounters_AspNetUsers_AssignedClinicianId
                    FOREIGN KEY (AssignedClinicianId) REFERENCES AspNetUsers (Id) ON DELETE SET NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_PatientEncounters_VisitNumber
                ON PatientEncounters (VisitNumber);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_PatientEncounters_PatientId
                ON PatientEncounters (PatientId)
                WHERE ClosedAt IS NULL AND Status <> 'Cancelled';
            CREATE TABLE IF NOT EXISTS PatientEncounterEvents (
                Id INTEGER NOT NULL CONSTRAINT PK_PatientEncounterEvents PRIMARY KEY AUTOINCREMENT,
                PatientEncounterId INTEGER NOT NULL,
                Status TEXT NOT NULL,
                Department TEXT NOT NULL,
                PatientMessage TEXT NOT NULL,
                OccurredAt TEXT NOT NULL,
                ChangedById TEXT NULL,
                CONSTRAINT FK_PatientEncounterEvents_PatientEncounters_PatientEncounterId
                    FOREIGN KEY (PatientEncounterId) REFERENCES PatientEncounters (Id) ON DELETE CASCADE,
                CONSTRAINT FK_PatientEncounterEvents_AspNetUsers_ChangedById
                    FOREIGN KEY (ChangedById) REFERENCES AspNetUsers (Id) ON DELETE SET NULL
            );
            CREATE INDEX IF NOT EXISTS IX_PatientEncounterEvents_PatientEncounterId
                ON PatientEncounterEvents (PatientEncounterId);
            CREATE INDEX IF NOT EXISTS IX_PatientEncounterEvents_ChangedById
                ON PatientEncounterEvents (ChangedById);
            CREATE TABLE IF NOT EXISTS CareNotes (
                Id INTEGER NOT NULL CONSTRAINT PK_CareNotes PRIMARY KEY AUTOINCREMENT,
                PatientId TEXT NOT NULL,
                AddedById TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL,
                Category TEXT NOT NULL,
                Priority TEXT NOT NULL,
                IsUrgent INTEGER NOT NULL,
                CONSTRAINT FK_CareNotes_AspNetUsers_PatientId
                    FOREIGN KEY (PatientId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
                CONSTRAINT FK_CareNotes_AspNetUsers_AddedById
                    FOREIGN KEY (AddedById) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_CareNotes_PatientId
                ON CareNotes (PatientId);
            CREATE INDEX IF NOT EXISTS IX_CareNotes_AddedById
                ON CareNotes (AddedById);
            CREATE TABLE IF NOT EXISTS PatientServices (
                Id INTEGER NOT NULL CONSTRAINT PK_PatientServices PRIMARY KEY AUTOINCREMENT,
                PatientId TEXT NOT NULL,
                ServiceCatalogId INTEGER NOT NULL,
                PatientEncounterId INTEGER NULL,
                AssignedById TEXT NOT NULL,
                AssignedAt TEXT NOT NULL,
                Status TEXT NOT NULL,
                Notes TEXT NOT NULL,
                ScheduledDate TEXT NULL,
                CompletedDate TEXT NULL,
                UnitPriceAtOrder TEXT NOT NULL,
                CONSTRAINT FK_PatientServices_AspNetUsers_PatientId
                    FOREIGN KEY (PatientId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
                CONSTRAINT FK_PatientServices_AspNetUsers_AssignedById
                    FOREIGN KEY (AssignedById) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
                CONSTRAINT FK_PatientServices_ServiceCatalogs_ServiceCatalogId
                    FOREIGN KEY (ServiceCatalogId) REFERENCES ServiceCatalogs (Id) ON DELETE CASCADE,
                CONSTRAINT FK_PatientServices_PatientEncounters_PatientEncounterId
                    FOREIGN KEY (PatientEncounterId) REFERENCES PatientEncounters (Id)
            );
            CREATE INDEX IF NOT EXISTS IX_PatientServices_PatientId
                ON PatientServices (PatientId);
            CREATE INDEX IF NOT EXISTS IX_PatientServices_AssignedById
                ON PatientServices (AssignedById);
            CREATE INDEX IF NOT EXISTS IX_PatientServices_ServiceCatalogId
                ON PatientServices (ServiceCatalogId);
            CREATE INDEX IF NOT EXISTS IX_PatientServices_PatientEncounterId
                ON PatientServices (PatientEncounterId);
            CREATE TABLE IF NOT EXISTS BillingPayments (
                Id INTEGER NOT NULL CONSTRAINT PK_BillingPayments PRIMARY KEY AUTOINCREMENT,
                InvoiceId INTEGER NOT NULL,
                ReceiptNumber TEXT NOT NULL,
                Amount TEXT NOT NULL,
                BalanceAfter TEXT NOT NULL,
                Method TEXT NOT NULL,
                Reference TEXT NOT NULL,
                GatewayReference TEXT NULL,
                RequestKey TEXT NULL,
                Status TEXT NOT NULL,
                ReceivedAt TEXT NOT NULL,
                ReceivedById TEXT NULL,
                CONSTRAINT FK_BillingPayments_BillingInvoices_InvoiceId
                    FOREIGN KEY (InvoiceId) REFERENCES BillingInvoices (Id) ON DELETE CASCADE,
                CONSTRAINT FK_BillingPayments_AspNetUsers_ReceivedById
                    FOREIGN KEY (ReceivedById) REFERENCES AspNetUsers (Id) ON DELETE SET NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_BillingPayments_ReceiptNumber
                ON BillingPayments (ReceiptNumber);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_BillingPayments_GatewayReference
                ON BillingPayments (GatewayReference)
                WHERE GatewayReference IS NOT NULL;
            CREATE INDEX IF NOT EXISTS IX_BillingPayments_InvoiceId
                ON BillingPayments (InvoiceId);
            CREATE INDEX IF NOT EXISTS IX_BillingPayments_ReceivedById
                ON BillingPayments (ReceivedById);
            CREATE INDEX IF NOT EXISTS IX_BillingInvoices_PatientEncounterId
                ON BillingInvoices (PatientEncounterId);
            DROP INDEX IF EXISTS IX_BillingItems_PatientServiceId;
            CREATE UNIQUE INDEX IF NOT EXISTS IX_BillingItems_PatientServiceId
                ON BillingItems (PatientServiceId)
                WHERE PatientServiceId IS NOT NULL;
            CREATE UNIQUE INDEX IF NOT EXISTS IX_BillingItems_LabOrderId
                ON BillingItems (LabOrderId)
                WHERE LabOrderId IS NOT NULL;
            CREATE INDEX IF NOT EXISTS IX_LabOrders_PatientEncounterId
                ON LabOrders (PatientEncounterId);
            CREATE INDEX IF NOT EXISTS IX_PatientServices_PatientEncounterId
                ON PatientServices (PatientEncounterId);
            CREATE INDEX IF NOT EXISTS IX_QueueEntries_PatientEncounterId
                ON QueueEntries (PatientEncounterId);
            CREATE INDEX IF NOT EXISTS IX_TriageEntries_PatientEncounterId
                ON TriageEntries (PatientEncounterId);
            CREATE INDEX IF NOT EXISTS IX_PharmacyOrders_PatientEncounterId
                ON PharmacyOrders (PatientEncounterId);
            CREATE INDEX IF NOT EXISTS IX_PharmacyOrders_BillingInvoiceId
                ON PharmacyOrders (BillingInvoiceId);
            CREATE INDEX IF NOT EXISTS IX_PharmacyOrders_PrescriberId
                ON PharmacyOrders (PrescriberId);
            """;
        await schemaCommand.ExecuteNonQueryAsync();
        await EnsureColumnAsync(connection, "PatientServices", "PatientEncounterId", "INTEGER NULL");
        await EnsureColumnAsync(connection, "PatientServices", "UnitPriceAtOrder", "TEXT NOT NULL DEFAULT '0'");
        await EnsureColumnAsync(connection, "BillingPayments", "RequestKey", "TEXT NULL");
        await using var paymentKeyIndexCommand = connection.CreateCommand();
        paymentKeyIndexCommand.CommandText = """
            CREATE UNIQUE INDEX IF NOT EXISTS IX_BillingPayments_RequestKey
                ON BillingPayments (RequestKey)
                WHERE RequestKey IS NOT NULL;
            """;
        await paymentKeyIndexCommand.ExecuteNonQueryAsync();
    }
    finally
    {
        if (closeConnection)
        {
            await connection.CloseAsync();
        }
    }
}

static async Task EnsureColumnAsync(
    System.Data.Common.DbConnection connection,
    string table,
    string column,
    string definition)
{
    await using var command = connection.CreateCommand();
    command.CommandText = $"PRAGMA table_info('{table}');";
    await using var reader = await command.ExecuteReaderAsync();
    var columnExists = false;
    while (await reader.ReadAsync())
    {
        if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
        {
            columnExists = true;
            break;
        }
    }

    await reader.DisposeAsync();
    if (!columnExists)
    {
        await using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition};";
        await alterCommand.ExecuteNonQueryAsync();
    }
}

app.Run();
