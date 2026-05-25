using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using HospitalWebApp.Models;
using HospitalWebApp.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Areas.Identity.Pages
{
    [Authorize(Roles = "Nurse,Doctor,Admin")]
    public class AddCareNoteModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AddCareNoteModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<ApplicationUser> Patients { get; set; } = new();
        public List<CareNote> CareNotes { get; set; } = new();
        public string? SelectedPatientId { get; set; }
        public ApplicationUser? SelectedPatient { get; set; }

        [BindProperty]
        public string PatientId { get; set; } = string.Empty;

        [BindProperty]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        public string Content { get; set; } = string.Empty;

        [BindProperty]
        public string Category { get; set; } = "General";

        [BindProperty]
        public string Priority { get; set; } = "Normal";

        [BindProperty]
        public bool IsUrgent { get; set; }

        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync(string? patientId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Only nurses, doctors, and admins can add care notes
            if (!await _userManager.IsInRoleAsync(user, "Nurse") && 
                !await _userManager.IsInRoleAsync(user, "Doctor") && 
                !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToPage("/Dashboard");
            }

            // Load all patients
            Patients = (await _userManager.GetUsersInRoleAsync("Patient")).ToList();

            if (!string.IsNullOrEmpty(patientId))
            {
                SelectedPatientId = patientId;
                SelectedPatient = await _userManager.FindByIdAsync(patientId);

                // Load care notes for selected patient
                CareNotes = await _context.CareNotes
                    .Include(cn => cn.Patient)
                    .Include(cn => cn.AddedBy)
                    .Where(cn => cn.PatientId == patientId)
                    .OrderByDescending(cn => cn.CreatedAt)
                    .ToListAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            if (!await _userManager.IsInRoleAsync(user, "Nurse") && 
                !await _userManager.IsInRoleAsync(user, "Doctor") && 
                !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToPage("/Dashboard");
            }

            if (string.IsNullOrEmpty(PatientId) || string.IsNullOrEmpty(Title) || string.IsNullOrEmpty(Content))
            {
                Message = "❌ Please fill in all required fields.";
                IsSuccess = false;
                return await OnGetAsync(PatientId);
            }

            // Create new care note
            var careNote = new CareNote
            {
                PatientId = PatientId,
                AddedById = user.Id,
                Title = Title,
                Content = Content,
                Category = Category,
                Priority = Priority,
                IsUrgent = IsUrgent,
                CreatedAt = DateTime.Now
            };

            _context.CareNotes.Add(careNote);
            await _context.SaveChangesAsync();

            Message = $"✅ Care note added successfully!";
            IsSuccess = true;

            // Reset form
            Title = string.Empty;
            Content = string.Empty;
            Category = "General";
            Priority = "Normal";
            IsUrgent = false;

            return await OnGetAsync(PatientId);
        }
    }
}
