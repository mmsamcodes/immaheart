using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using HospitalWebApp.Models;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;

namespace HospitalWebApp.Areas.Identity.Pages
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminDashboardModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public int TotalUsersCount { get; set; }
        public int DoctorsCount { get; set; }
        public int PatientsCount { get; set; }
        public int TotalInvoicesCount { get; set; }
        public int OutstandingInvoicesCount { get; set; }
        public decimal OutstandingBalance { get; set; }
        public int AdmissionsCount { get; set; }
        public int LabOrdersCount { get; set; }
        public int DrugCatalogCount { get; set; }
        public int ServiceCatalogCount { get; set; }
        public List<DrugUsageSummary> TopDrugs { get; set; } = new();
        public List<ConditionSummary> TopConditions { get; set; } = new();
        public List<ApplicationUser> RecentDoctors { get; set; } = new();
        public List<ApplicationUser> RecentPatients { get; set; } = new();
        public string Message { get; set; } = "";

        [BindProperty]
        [Required]
        public string FullName { get; set; } = "";

        [BindProperty]
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [BindProperty]
        [Required]
        public string Password { get; set; } = "";

        [BindProperty]
        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToPage("/Dashboard");

            await LoadDashboardData();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Message = "Please correct the form errors and try again.";
                await LoadDashboardData();
                return Page();
            }

            if (!await _roleManager.RoleExistsAsync("Doctor"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Doctor"));
            }

            if (await _userManager.FindByEmailAsync(Email) != null)
            {
                Message = "A user with that email already exists.";
                await LoadDashboardData();
                return Page();
            }

            var newDoctor = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                FullName = FullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(newDoctor, Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(newDoctor, "Doctor");
                Message = "Doctor added successfully!";
            }
            else
            {
                Message = "Error: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            await LoadDashboardData();
            return Page();
        }

        private async Task LoadDashboardData()
        {
            var users = await _userManager.Users.ToListAsync();
            TotalUsersCount = users.Count;

            var doctors = new List<ApplicationUser>();
            var patients = new List<ApplicationUser>();

            foreach (var u in users)
            {
                if (await _userManager.IsInRoleAsync(u, "Doctor"))
                {
                    DoctorsCount++;
                    doctors.Add(u);
                }
                else
                {
                    PatientsCount++;
                    patients.Add(u);
                }
            }

            RecentDoctors = doctors.OrderByDescending(u => u.Id).Take(10).ToList();
            RecentPatients = patients.OrderByDescending(u => u.Id).Take(10).ToList();

            TotalInvoicesCount = await _context.BillingInvoices.CountAsync();
            OutstandingInvoicesCount = await _context.BillingInvoices
                .CountAsync(i => i.Status == "Unpaid" || i.PaidAmount < i.TotalAmount);
            OutstandingBalance = await _context.BillingInvoices
                .Where(i => i.Status == "Unpaid" || i.PaidAmount < i.TotalAmount)
                .SumAsync(i => (i.TotalAmount - i.PaidAmount));
            AdmissionsCount = await _context.Admissions.CountAsync();
            LabOrdersCount = await _context.LabOrders.CountAsync();
            DrugCatalogCount = await _context.Drugs.CountAsync();
            ServiceCatalogCount = await _context.ServiceCatalogs.CountAsync();
            TopDrugs = await _context.BillingItems
                .GroupBy(i => i.Description)
                .Select(g => new DrugUsageSummary
                {
                    Name = g.Key,
                    UsageCount = g.Count(),
                    TotalRevenue = g.Sum(i => i.Amount)
                })
                .OrderByDescending(d => d.UsageCount)
                .Take(5)
                .ToListAsync();
            TopConditions = await _context.TriageEntries
                .Where(t => !string.IsNullOrEmpty(t.PrimaryConcern))
                .GroupBy(t => t.PrimaryConcern)
                .Select(g => new ConditionSummary
                {
                    Concern = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(c => c.Count)
                .Take(5)
                .ToListAsync();
        }

        public class DrugUsageSummary
        {
            public string Name { get; set; } = string.Empty;
            public int UsageCount { get; set; }
            public decimal TotalRevenue { get; set; }
        }

        public class ConditionSummary
        {
            public string Concern { get; set; } = string.Empty;
            public int Count { get; set; }
        }
    }
}
