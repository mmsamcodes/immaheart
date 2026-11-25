using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using HospitalWebApp.Models;
using HospitalWebApp.Data;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Services;

namespace HospitalWebApp.Areas.Identity.Pages
{
    [Authorize(Roles = "Doctor,Admin")]
    public class AddServiceModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly PatientEncounterService _encounterService;

        public AddServiceModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            PatientEncounterService encounterService)
        {
            _userManager = userManager;
            _context = context;
            _encounterService = encounterService;
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
            if (service == null || !service.IsAvailable || service.Price <= 0)
            {
                Message = "Service is not available.";
                IsSuccess = false;
                return await OnGetAsync();
            }

            var patient = await _userManager.FindByIdAsync(PatientId);
            if (patient == null || !await _userManager.IsInRoleAsync(patient, "Patient"))
            {
                Message = "Select a valid patient account.";
                IsSuccess = false;
                return await OnGetAsync();
            }

            var encounter = await _encounterService.FindActiveAsync(patient.Id);
            if (encounter == null)
            {
                Message = "The patient must be checked in before a service can be assigned.";
                IsSuccess = false;
                return await OnGetAsync();
            }

            var patientService = new PatientService
            {
                PatientId = patient.Id,
                ServiceCatalogId = ServiceCatalogId,
                PatientEncounter = encounter,
                AssignedById = user.Id,
                AssignedAt = DateTime.Now,
                ScheduledDate = ScheduledDate,
                Notes = Notes,
                UnitPriceAtOrder = service.Price,
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

        public async Task<IActionResult> OnPostCompleteAsync(int patientServiceId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (!isAdmin && !await _userManager.IsInRoleAsync(user, "Doctor"))
            {
                return Forbid();
            }

            var patientService = await _context.PatientServices
                .AsNoTracking()
                .Include(item => item.Service)
                .Include(item => item.PatientEncounter)
                .SingleOrDefaultAsync(item => item.Id == patientServiceId);
            if (patientService == null)
            {
                return NotFound();
            }
            if (!isAdmin && patientService.AssignedById != user.Id)
            {
                return Forbid();
            }
            if (patientService.Status != "Pending")
            {
                TempData["ServiceMessage"] = "This service has already been completed or is no longer pending.";
                return RedirectToPage();
            }
            if (patientService.PatientEncounter == null ||
                patientService.PatientEncounter.ClosedAt.HasValue ||
                patientService.UnitPriceAtOrder <= 0)
            {
                TempData["ServiceMessage"] = "This service cannot be completed because its visit or charge is missing. Contact an administrator.";
                return RedirectToPage();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var claimed = await _context.PatientServices
                .Where(item => item.Id == patientService.Id && item.Status == "Pending")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Status, "Completed")
                    .SetProperty(item => item.CompletedDate, DateTime.UtcNow));
            if (claimed != 1)
            {
                await transaction.RollbackAsync();
                TempData["ServiceMessage"] = "Another user has already completed this service. Refresh the worklist.";
                return RedirectToPage();
            }

            var invoice = await _context.BillingInvoices
                .Where(item =>
                    item.PatientEncounterId == patientService.PatientEncounterId &&
                    item.Status != "Paid")
                .OrderBy(item => item.IssuedAt)
                .FirstOrDefaultAsync();

            if (invoice == null)
            {
                invoice = new BillingInvoice
                {
                    PatientId = patientService.PatientId,
                    PatientEncounterId = patientService.PatientEncounterId,
                    IssuedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(14),
                    Status = "Unpaid",
                    Notes = "Charges generated from completed services."
                };
                _context.BillingInvoices.Add(invoice);
            }

            invoice.TotalAmount += patientService.UnitPriceAtOrder;
            invoice.Status = invoice.PaidAmount >= invoice.TotalAmount
                ? "Paid"
                : invoice.PaidAmount > 0 ? "Partial" : "Unpaid";
            invoice.PaymentConfirmed = invoice.Status == "Paid";
            _context.BillingItems.Add(new BillingItem
            {
                Invoice = invoice,
                PatientServiceId = patientService.Id,
                Description = patientService.Service?.Name ?? "Completed clinical service",
                Amount = patientService.UnitPriceAtOrder
            });

            _encounterService.Transition(
                patientService.PatientEncounter,
                PatientEncounter.InProgress,
                "Consultation",
                "A service was completed during your visit. The cashier can provide your updated bill.",
                user.Id);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["ServiceMessage"] = "Service marked complete and added to the patient's visit bill.";
            return RedirectToPage();
        }
    }
}
