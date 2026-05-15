using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Pages.Triage
{
    [Authorize(Roles = "Doctor,Nurse,Admin")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public TriageEntry? Entry { get; set; }
        public IList<CarePlanEntry> CarePlans { get; set; } = new List<CarePlanEntry>();
        public IList<HealthRecord> HealthRecords { get; set; } = new List<HealthRecord>();
        [BindProperty]
        public HealthRecordInputModel RecordInput { get; set; } = new HealthRecordInputModel();
        public string Message { get; set; } = string.Empty;

        public class HealthRecordInputModel
        {
            [Required]
            public string Diagnosis { get; set; } = string.Empty;
            public string Prescription { get; set; } = string.Empty;
            public string DoctorNotes { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Entry = await _context.TriageEntries
                .Include(t => t.Patient)
                .Include(t => t.Nurse)
                .Include(t => t.AssignedDoctor)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (Entry == null)
                return NotFound();

            CarePlans = await _context.CarePlans
                .Include(c => c.CreatedBy)
                .Where(c => c.PatientId == Entry.PatientId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            HealthRecords = await _context.HealthRecords
                .Include(hr => hr.Doctor)
                .Where(hr => hr.PatientId == Entry.PatientId)
                .OrderByDescending(hr => hr.RecordDate)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string actionType)
        {
            Entry = await _context.TriageEntries
                .Include(t => t.Patient)
                .Include(t => t.Nurse)
                .Include(t => t.AssignedDoctor)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (Entry == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Forbid();

            if (!await _userManager.IsInRoleAsync(user, "Doctor") && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return Forbid();
            }

            if (actionType == "SendToLab")
            {
                Entry.Status = "PendingLab";
                Entry.NextStep = "Lab";
                Entry.AssignedDoctorId = user.Id;
                _context.QueueEntries.Add(new QueueEntry
                {
                    PatientId = Entry.PatientId,
                    AssignedById = user.Id,
                    QueueType = "Lab",
                    Status = "Waiting",
                    Notes = "Doctor sent patient to lab."
                });
            }
            else if (actionType == "SendToPharmacy")
            {
                Entry.Status = "PendingPharmacy";
                Entry.NextStep = "Pharmacy";
                Entry.AssignedDoctorId = user.Id;
                _context.QueueEntries.Add(new QueueEntry
                {
                    PatientId = Entry.PatientId,
                    AssignedById = user.Id,
                    QueueType = "Pharmacy",
                    Status = "Waiting",
                    Notes = "Doctor sent patient to pharmacy."
                });
            }
            else if (actionType == "MarkComplete")
            {
                Entry.Status = "Completed";
                Entry.NextStep = "Finished";
                Entry.AssignedDoctorId = user.Id;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("/Triage/Index");
        }

        public async Task<IActionResult> OnPostAddRecordAsync(int id)
        {
            Entry = await _context.TriageEntries
                .Include(t => t.Patient)
                .Include(t => t.Nurse)
                .Include(t => t.AssignedDoctor)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (Entry == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Doctor"))
            {
                return Forbid();
            }

            if (!TryValidateModel(RecordInput))
            {
                CarePlans = await _context.CarePlans
                    .Include(c => c.CreatedBy)
                    .Where(c => c.PatientId == Entry.PatientId)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                HealthRecords = await _context.HealthRecords
                    .Include(hr => hr.Doctor)
                    .Where(hr => hr.PatientId == Entry.PatientId)
                    .OrderByDescending(hr => hr.RecordDate)
                    .ToListAsync();

                return Page();
            }

            var record = new HealthRecord
            {
                PatientId = Entry.PatientId,
                DoctorId = user.Id,
                RecordDate = DateTime.Now,
                BloodPressure = Entry.BloodPressure,
                WeightKg = Entry.WeightKg,
                Diagnosis = RecordInput.Diagnosis,
                Prescription = RecordInput.Prescription,
                DoctorNotes = RecordInput.DoctorNotes
            };

            _context.HealthRecords.Add(record);
            Entry.AssignedDoctorId = user.Id;
            if (Entry.Status == "PendingDoctor")
            {
                Entry.Status = "PendingPharmacy";
                Entry.NextStep = "Pharmacy";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
