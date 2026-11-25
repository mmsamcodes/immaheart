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
using HospitalWebApp.Services;

namespace HospitalWebApp.Pages.Triage
{
    [Authorize(Roles = "Doctor,Nurse,Admin")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PatientEncounterService _encounterService;

        public DetailsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            PatientEncounterService encounterService)
        {
            _context = context;
            _userManager = userManager;
            _encounterService = encounterService;
        }

        public TriageEntry? Entry { get; set; }
        public IList<CarePlanEntry> CarePlans { get; set; } = new List<CarePlanEntry>();
        public IList<HealthRecord> HealthRecords { get; set; } = new List<HealthRecord>();
        public IList<ServiceCatalog> LabServices { get; set; } = new List<ServiceCatalog>();
        public IList<Drug> PharmacyDrugs { get; set; } = new List<Drug>();
        [BindProperty]
        public HealthRecordInputModel RecordInput { get; set; } = new HealthRecordInputModel();
        [BindProperty]
        public int? LabServiceCatalogId { get; set; }
        [BindProperty]
        public int? PharmacyDrugId { get; set; }
        [BindProperty]
        public int PharmacyQuantity { get; set; } = 1;
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

            if (User.IsInRole("Doctor") &&
                Entry.AssignedDoctorId != null &&
                Entry.AssignedDoctorId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

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

            LabServices = await _context.ServiceCatalogs
                .AsNoTracking()
                .Where(service => service.IsAvailable && service.Price > 0)
                .OrderBy(service => service.Category)
                .ThenBy(service => service.Name)
                .ToListAsync();
            PharmacyDrugs = await _context.Drugs
                .AsNoTracking()
                .Where(drug => drug.IsActive && drug.StockQuantity > 0)
                .OrderBy(drug => drug.Name)
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

            if (actionType is "SendToLab" or "SendToPharmacy")
            {
                if (Entry.Status != "PendingDoctor")
                {
                    return BadRequest("Only visits awaiting clinician review can be sent to a new department.");
                }
            }
            else if (actionType == "MarkComplete")
            {
                if (Entry.Status is not ("PendingDoctor" or "PendingLab" or "PendingPharmacy"))
                {
                    return BadRequest("This visit is not ready for clinical completion.");
                }
            }
            else
            {
                return BadRequest();
            }

            ServiceCatalog? labService = null;
            Drug? prescribedDrug = null;
            string? prescriptionText = null;
            if (actionType == "SendToLab")
            {
                if (!LabServiceCatalogId.HasValue)
                {
                    return BadRequest("Select the diagnostic service to order.");
                }

                labService = await _context.ServiceCatalogs
                    .SingleOrDefaultAsync(service =>
                        service.Id == LabServiceCatalogId.Value &&
                        service.IsAvailable &&
                        service.Price > 0);
                if (labService == null)
                {
                    return BadRequest("Selected diagnostic service is not available.");
                }
            }
            else if (actionType == "SendToPharmacy")
            {
                if (!PharmacyDrugId.HasValue || PharmacyQuantity <= 0 || PharmacyQuantity > 100)
                {
                    return BadRequest("Select a medicine and a valid quantity.");
                }

                prescribedDrug = await _context.Drugs
                    .SingleOrDefaultAsync(drug =>
                        drug.Id == PharmacyDrugId.Value &&
                        drug.IsActive &&
                        drug.StockQuantity >= PharmacyQuantity);
                if (prescribedDrug == null || prescribedDrug.UnitPrice <= 0)
                {
                    return BadRequest("Selected medicine is unavailable or has no valid charge.");
                }

                prescriptionText = await _context.HealthRecords
                    .Where(record => record.PatientId == Entry.PatientId)
                    .OrderByDescending(record => record.RecordDate)
                    .Select(record => record.Prescription)
                    .FirstOrDefaultAsync();
                if (prescribedDrug.RequiresPrescription && string.IsNullOrWhiteSpace(prescriptionText))
                {
                    return BadRequest("Record the prescription in clinical findings before sending this medicine to pharmacy.");
                }
            }

            if (actionType == "SendToLab")
            {
                Entry.Status = "PendingLab";
                Entry.NextStep = "Lab";
                Entry.AssignedDoctorId = user.Id;
            }
            else if (actionType == "SendToPharmacy")
            {
                Entry.Status = "PendingPharmacy";
                Entry.NextStep = "Pharmacy";
                Entry.AssignedDoctorId = user.Id;
            }
            else if (actionType == "MarkComplete")
            {
                if (Entry.Status == "PendingLab" &&
                    await _context.LabOrders.AnyAsync(order =>
                        order.PatientId == Entry.PatientId &&
                        (!Entry.PatientEncounterId.HasValue ||
                         order.PatientEncounterId == Entry.PatientEncounterId.Value) &&
                        order.Status != "Completed"))
                {
                    return BadRequest("Complete all diagnostic tests before closing the clinician visit.");
                }
                if (Entry.Status == "PendingPharmacy" &&
                    await _context.PharmacyOrders.AnyAsync(order =>
                        order.PatientId == Entry.PatientId &&
                        (!Entry.PatientEncounterId.HasValue ||
                         order.PatientEncounterId == Entry.PatientEncounterId.Value) &&
                        order.Status != PharmacyOrder.Completed &&
                        order.Status != PharmacyOrder.Rejected))
                {
                    return BadRequest("Complete pharmacy dispensing before closing the clinician visit.");
                }

                Entry.Status = "Completed";
                Entry.NextStep = "Cashier";
                Entry.AssignedDoctorId = user.Id;
            }
            else
            {
                return BadRequest();
            }

            var encounter = Entry.PatientEncounterId.HasValue
                ? await _context.PatientEncounters.FindAsync(Entry.PatientEncounterId.Value)
                : await _encounterService.FindActiveAsync(Entry.PatientId);
            if (encounter == null)
            {
                encounter = _encounterService.Start(
                    Entry.PatientId,
                    user.Id,
                    PatientEncounter.InProgress,
                    "Consultation",
                    "Your clinician is updating your care plan.");
                Entry.PatientEncounter = encounter;
            }
            else
            {
                Entry.PatientEncounter = encounter;
            }
            encounter.AssignedClinicianId = user.Id;

            switch (actionType)
            {
                case "SendToLab":
                    _context.LabOrders.Add(new LabOrder
                    {
                        PatientId = Entry.PatientId,
                        DoctorId = user.Id,
                        TestName = labService!.Name,
                        Notes = labService.Description,
                        Cost = labService.Price,
                        PatientEncounter = encounter,
                        OrderedAt = DateTime.UtcNow,
                        Status = "Ordered"
                    });
                    _encounterService.Transition(
                        encounter,
                        PatientEncounter.AwaitingDiagnostics,
                        "Diagnostics",
                        "Your clinician has requested diagnostic testing. Please follow staff directions.",
                        user.Id);
                    break;
                case "SendToPharmacy":
                    _context.PharmacyOrders.Add(new PharmacyOrder
                    {
                        PatientId = Entry.PatientId,
                        PatientEncounter = encounter,
                        PrescriberId = user.Id,
                        PrescriptionText = prescriptionText,
                        PatientNote = "Prescription generated during clinician review.",
                        Status = PharmacyOrder.PendingReview,
                        Items = new List<PharmacyOrderItem>
                        {
                            new()
                            {
                                DrugId = prescribedDrug!.Id,
                                DrugName = prescribedDrug.Name,
                                Quantity = PharmacyQuantity,
                                UnitPrice = prescribedDrug.UnitPrice,
                                RequiresPrescription = prescribedDrug.RequiresPrescription
                            }
                        }
                    });
                    _encounterService.Transition(
                        encounter,
                        PatientEncounter.AwaitingPharmacy,
                        "Pharmacy",
                        "Your clinician has sent a prescription for pharmacist review.",
                        user.Id);
                    break;
                case "MarkComplete":
                    _encounterService.Transition(
                        encounter,
                        PatientEncounter.ReadyToLeave,
                        "Cashier",
                        "Your clinical visit is complete. Please check with the cashier before leaving.",
                        user.Id);
                    break;
            }

            if (actionType == "SendToLab" || actionType == "SendToPharmacy")
            {
                _context.QueueEntries.Add(new QueueEntry
                {
                    PatientId = Entry.PatientId,
                    PatientEncounter = encounter,
                    AssignedById = user.Id,
                    QueueType = actionType == "SendToLab" ? "Lab" : "Pharmacy",
                    Status = "Waiting",
                    Notes = actionType == "SendToLab" ? "Diagnostic test requested." : "Doctor sent patient to pharmacy."
                });
            }

            if (actionType is "SendToLab" or "SendToPharmacy")
            {
                var oldQueueType = actionType == "SendToLab" ? "Doctor" : "Doctor";
                var currentQueue = await _context.QueueEntries
                    .Where(queue => queue.PatientId == Entry.PatientId &&
                                    queue.QueueType == oldQueueType &&
                                    queue.Status == "Waiting")
                    .OrderByDescending(queue => queue.CreatedAt)
                    .FirstOrDefaultAsync();
                if (currentQueue != null)
                {
                    currentQueue.Status = "Processed";
                    currentQueue.ProcessedAt = DateTime.Now;
                }
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
                LabServices = await _context.ServiceCatalogs
                    .AsNoTracking()
                    .Where(service => service.IsAvailable && service.Price > 0)
                    .OrderBy(service => service.Category)
                    .ThenBy(service => service.Name)
                    .ToListAsync();
                PharmacyDrugs = await _context.Drugs
                    .AsNoTracking()
                    .Where(drug => drug.IsActive && drug.StockQuantity > 0)
                    .OrderBy(drug => drug.Name)
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

            var encounter = Entry.PatientEncounterId.HasValue
                ? await _context.PatientEncounters.FindAsync(Entry.PatientEncounterId.Value)
                : await _encounterService.FindActiveAsync(Entry.PatientId);
            if (encounter != null)
            {
                Entry.PatientEncounter = encounter;
                encounter.AssignedClinicianId = user.Id;
                _encounterService.Transition(
                    encounter,
                    PatientEncounter.InProgress,
                    "Consultation",
                    "Your clinician has updated your care plan.",
                    user.Id);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
