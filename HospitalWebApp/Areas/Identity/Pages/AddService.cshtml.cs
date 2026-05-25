using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using HospitalWebApp.Models;
using HospitalWebApp.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Areas.Identity.Pages
{
    [Authorize(Roles = "Doctor,Admin")]
    public class AddServiceModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AddServiceModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<ApplicationUser> Patients { get; set; } = new();
        public List<ServiceCatalog> Services { get; set; } = new();
        public List<PatientService> AssignedServices { get; set; } = new();

        [BindProperty]
        public string PatientId { get; set; } = string.Empty;

        [BindProperty]
        public int ServiceCatalogId { get; set; }

        [BindProperty]
        public DateTime? ScheduledDate { get; set; }

        [BindProperty]
        public string Notes { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Only doctors and admins can assign services
            if (!await _userManager.IsInRoleAsync(user, "Doctor") && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToPage("/Dashboard");
            }

            // Load all patients
            Patients = (await _userManager.GetUsersInRoleAsync("Patient")).ToList();

            // Load all available services
            Services = await _context.ServiceCatalogs
                .Where(s => s.IsAvailable)
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Name)
                .ToListAsync();

            // Load assigned services for display
            AssignedServices = await _context.PatientServices
                .Include(ps => ps.Patient)
                .Include(ps => ps.Service)
                .Include(ps => ps.AssignedBy)
                .Where(ps => ps.AssignedById == user.Id)
                .OrderByDescending(ps => ps.AssignedAt)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            if (!await _userManager.IsInRoleAsync(user, "Doctor") && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToPage("/Dashboard");
            }

            // Validate service exists
            var service = await _context.ServiceCatalogs.FindAsync(ServiceCatalogId);
            if (service == null)
            {
                Message = "❌ Service not found.";
                IsSuccess = false;
                return await OnGetAsync();
            }

            // Create new patient service assignment
            var patientService = new PatientService
            {
                PatientId = PatientId,
                ServiceCatalogId = ServiceCatalogId,
                AssignedById = user.Id,
                AssignedAt = DateTime.Now,
                ScheduledDate = ScheduledDate,
                Notes = Notes,
                Status = "Pending"
            };

            _context.PatientServices.Add(patientService);
            await _context.SaveChangesAsync();

            Message = $"✅ Service '{service.Name}' successfully assigned to patient!";
            IsSuccess = true;

            // Reset form
            PatientId = string.Empty;
            ServiceCatalogId = 0;
            ScheduledDate = null;
            Notes = string.Empty;

            return await OnGetAsync();
        }
    }
}
