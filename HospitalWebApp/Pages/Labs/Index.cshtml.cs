using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using HospitalWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Pages_Labs;

[Authorize(Roles = "Admin,Doctor,LabTech,Nurse,Patient")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly PatientEncounterService _encounterService;

    public IndexModel(ApplicationDbContext context, PatientEncounterService encounterService)
    {
        _context = context;
        _encounterService = encounterService;
    }

    public IList<LabOrder> LabOrders { get; set; } = new List<LabOrder>();

    [BindProperty]
    public LabResultInput Input { get; set; } = new();

    public class LabResultInput
    {
        [Required]
        [StringLength(4000, MinimumLength = 1)]
        public string ResultSummary { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        var query = _context.LabOrders
            .AsNoTracking()
            .Include(order => order.Patient)
            .Include(order => order.Doctor)
            .AsQueryable();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.IsInRole("Patient"))
        {
            query = query.Where(order => order.PatientId == userId);
        }
        else if (User.IsInRole("Doctor"))
        {
            query = query.Where(order => order.DoctorId == userId);
        }

        LabOrders = await query.OrderByDescending(order => order.OrderedAt).Take(200).ToListAsync();
    }

    public async Task<IActionResult> OnPostCollectAsync(int id)
    {
        if (!User.IsInRole("LabTech") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var updated = await _context.LabOrders
            .Where(order => order.Id == id && order.Status == "Ordered")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(order => order.Status, "Collected")
                .SetProperty(order => order.CollectedAt, DateTime.UtcNow));
        if (updated != 1)
        {
            TempData["LabWorkMessage"] = "This test is no longer waiting for specimen collection.";
        }
        else
        {
            TempData["LabWorkMessage"] = "Specimen collection recorded.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCompleteAsync(int id)
    {
        if (!User.IsInRole("LabTech") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }
        if (!TryValidateModel(Input))
        {
            await OnGetAsync();
            return Page();
        }

        var order = await _context.LabOrders
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);
        if (order == null)
        {
            return NotFound();
        }
        if (order.Cost <= 0)
        {
            TempData["LabWorkError"] = "This test has no valid charge in the catalog. Contact an administrator before completing it.";
            return RedirectToPage();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var updated = await _context.LabOrders
            .Where(item => item.Id == id && item.Status == "Collected")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, "Completed")
                .SetProperty(item => item.CompletedAt, DateTime.UtcNow)
                .SetProperty(item => item.ResultSummary, Input.ResultSummary.Trim()));
        if (updated != 1)
        {
            await transaction.RollbackAsync();
            TempData["LabWorkError"] = "This order is not awaiting a result. Refresh the worklist.";
            return RedirectToPage();
        }

        var invoice = order.PatientEncounterId.HasValue
            ? await _context.BillingInvoices
                .Where(item =>
                    item.PatientEncounterId == order.PatientEncounterId.Value &&
                    item.Status != "Paid")
                .OrderBy(item => item.IssuedAt)
                .FirstOrDefaultAsync()
            : null;
        if (invoice == null)
        {
            invoice = new BillingInvoice
            {
                PatientId = order.PatientId,
                PatientEncounterId = order.PatientEncounterId,
                IssuedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(14),
                Status = "Unpaid",
                Notes = "Charges generated from completed diagnostic services."
            };
            _context.BillingInvoices.Add(invoice);
        }

        invoice.TotalAmount += order.Cost;
        invoice.Status = invoice.PaidAmount >= invoice.TotalAmount
            ? "Paid"
            : invoice.PaidAmount > 0 ? "Partial" : "Unpaid";
        invoice.PaymentConfirmed = invoice.Status == "Paid";
        _context.BillingItems.Add(new BillingItem
        {
            Invoice = invoice,
            LabOrderId = order.Id,
            Description = order.TestName,
            Amount = order.Cost
        });

        var queueEntry = await _context.QueueEntries
            .Where(queue =>
                queue.PatientId == order.PatientId &&
                queue.QueueType == "Lab" &&
                queue.Status == "Waiting")
            .OrderByDescending(queue => queue.CreatedAt)
            .FirstOrDefaultAsync();
        if (queueEntry != null)
        {
            queueEntry.Status = "Processed";
            queueEntry.ProcessedAt = DateTime.UtcNow;
        }
        if (order.PatientEncounterId.HasValue)
        {
            var encounter = await _context.PatientEncounters.FindAsync(order.PatientEncounterId.Value);
            if (encounter != null && encounter.ClosedAt == null)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _encounterService.Transition(
                    encounter,
                    PatientEncounter.AwaitingClinician,
                    "Consultation",
                    "Your diagnostic test is complete. Please wait for clinician review.",
                    userId);

                var triageEntry = await _context.TriageEntries
                    .Where(entry => entry.PatientEncounterId == encounter.Id)
                    .OrderByDescending(entry => entry.TriageDate)
                    .FirstOrDefaultAsync();
                if (triageEntry != null)
                {
                    triageEntry.Status = "PendingDoctor";
                    triageEntry.NextStep = "Doctor";
                }

                if (!string.IsNullOrEmpty(order.DoctorId) && !string.IsNullOrEmpty(userId))
                {
                    _context.QueueEntries.Add(new QueueEntry
                    {
                        PatientId = order.PatientId,
                        PatientEncounter = encounter,
                        AssignedById = userId,
                        AssignedDoctorId = order.DoctorId,
                        QueueType = "Doctor",
                        Status = "Waiting",
                        Notes = "Diagnostic result is ready for clinician review."
                    });
                    encounter.AssignedClinicianId = order.DoctorId;
                }
            }
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["LabWorkMessage"] = "Test completed, result recorded and service charge added to the visit.";
        return RedirectToPage();
    }
}
