using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Services;

namespace HospitalWebApp.Areas.Identity.Pages;

[Authorize(Roles = "Pharmacy")]
public class PharmacyOrdersModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IDataProtector _prescriptionProtector;
    private readonly PatientEncounterService _encounterService;

    public PharmacyOrdersModel(
        ApplicationDbContext context,
        IDataProtectionProvider dataProtectionProvider,
        PatientEncounterService encounterService)
    {
        _context = context;
        _prescriptionProtector = dataProtectionProvider.CreateProtector("HospitalWebApp.PharmacyPrescription.v1");
        _encounterService = encounterService;
    }

    public IList<PharmacyOrder> Orders { get; private set; } = new List<PharmacyOrder>();

    public async Task OnGetAsync()
    {
        await LoadOrdersAsync();
    }

    public async Task<IActionResult> OnPostReviewAsync(int orderId, string decision)
    {
        if (decision is not ("approve" or "reject"))
        {
            return BadRequest();
        }

        var order = await _context.PharmacyOrders
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == orderId);
        if (order == null)
        {
            return NotFound();
        }
        if (order.Items.Count == 0)
        {
            TempData["PharmacyOrderError"] = "This request has no items and cannot be prepared.";
            return RedirectToPage();
        }

        if (order.Status != PharmacyOrder.PendingReview)
        {
            TempData["PharmacyOrderError"] = "This request has already been reviewed.";
            return RedirectToPage();
        }

        if (decision == "approve")
        {
            var drugIds = order.Items.Select(item => item.DrugId).ToArray();
            var prescriptionRequired = order.Items.Any(item => item.RequiresPrescription) ||
                await _context.Drugs
                .AnyAsync(drug => drugIds.Contains(drug.Id) && drug.RequiresPrescription);
            if (prescriptionRequired &&
                order.PrescriptionDocument == null &&
                string.IsNullOrWhiteSpace(order.PrescriptionText))
            {
                TempData["PharmacyOrderError"] = "This request needs a prescription document before it can be approved.";
                return RedirectToPage();
            }
        }

        var nextStatus = decision == "approve" ? PharmacyOrder.Approved : PharmacyOrder.Rejected;
        var updated = await _context.PharmacyOrders
            .Where(item => item.Id == orderId && item.Status == PharmacyOrder.PendingReview)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, nextStatus));

        TempData[updated == 1 ? "PharmacyOrderMessage" : "PharmacyOrderError"] =
            updated == 1
                ? $"Request {order.OrderNumber} was marked {nextStatus.ToLowerInvariant()}."
                : "This request has already been reviewed.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReadyAsync(int orderId)
    {
        var order = await _context.PharmacyOrders
            .AsNoTracking()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == orderId);
        if (order == null)
        {
            return NotFound();
        }
        if (order.Items.Count == 0)
        {
            TempData["PharmacyOrderError"] = "This request has no items and cannot be prepared.";
            return RedirectToPage();
        }
        if (order.Items.Any(item => item.Quantity <= 0))
        {
            TempData["PharmacyOrderError"] = "This request contains an invalid quantity and cannot be prepared.";
            return RedirectToPage();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var updated = await _context.PharmacyOrders
            .Where(item => item.Id == orderId &&
                           item.Status == PharmacyOrder.Approved &&
                           item.BillingInvoiceId == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, PharmacyOrder.ReadyForCollection));
        if (updated != 1)
        {
            await transaction.RollbackAsync();
            TempData["PharmacyOrderError"] = "This request is no longer awaiting preparation.";
            return RedirectToPage();
        }

        foreach (var item in order.Items)
        {
            var stockUpdated = await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Drugs SET StockQuantity = StockQuantity - {item.Quantity} WHERE Id = {item.DrugId} AND IsActive = 1 AND StockQuantity >= {item.Quantity}");
            if (stockUpdated != 1)
            {
                await transaction.RollbackAsync();
                TempData["PharmacyOrderError"] = $"There is not enough stock to prepare {item.DrugName}. No stock was changed.";
                return RedirectToPage();
            }
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
                Notes = "Medication charges generated from an approved pharmacy order."
            };
            _context.BillingInvoices.Add(invoice);
        }

        foreach (var item in order.Items)
        {
            var lineAmount = item.UnitPrice * item.Quantity;
            _context.BillingItems.Add(new BillingItem
            {
                Invoice = invoice,
                Description = $"{item.DrugName} × {item.Quantity}",
                Amount = lineAmount
            });
            invoice.TotalAmount += lineAmount;
        }
        invoice.Status = invoice.PaidAmount >= invoice.TotalAmount
            ? "Paid"
            : invoice.PaidAmount > 0 ? "Partial" : "Unpaid";
        invoice.PaymentConfirmed = invoice.Status == "Paid";

        await _context.SaveChangesAsync();
        var invoiceLinked = await _context.PharmacyOrders
            .Where(item =>
                item.Id == orderId &&
                item.Status == PharmacyOrder.ReadyForCollection &&
                item.BillingInvoiceId == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.BillingInvoiceId, invoice.Id));
        if (invoiceLinked != 1)
        {
            await transaction.RollbackAsync();
            TempData["PharmacyOrderError"] = "Could not link the pharmacy bill to this order. No stock or billing changes were kept.";
            return RedirectToPage();
        }

        if (order.PatientEncounterId.HasValue)
        {
            var encounter = await _context.PatientEncounters.FindAsync(order.PatientEncounterId.Value);
            if (encounter != null && encounter.ClosedAt == null)
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                _encounterService.Transition(
                    encounter,
                    PatientEncounter.AwaitingPayment,
                    "Cashier",
                    "Your medication is prepared. Please visit the cashier to settle the pharmacy bill.",
                    user);
                await _context.SaveChangesAsync();
            }
        }
        await transaction.CommitAsync();
        TempData["PharmacyOrderMessage"] = $"Request {order.OrderNumber} is ready for hospital collection.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCollectedAsync(int orderId)
    {
        var order = await _context.PharmacyOrders
            .AsNoTracking()
            .Include(item => item.BillingInvoice)
            .SingleOrDefaultAsync(item => item.Id == orderId);
        if (order == null)
        {
            return NotFound();
        }
        if (order.BillingInvoice == null || order.BillingInvoice.Status != "Paid")
        {
            TempData["PharmacyOrderError"] = "The pharmacy bill must be settled by the cashier before medicines can be collected.";
            return RedirectToPage();
        }

        var updated = await _context.PharmacyOrders
            .Where(item => item.Id == orderId && item.Status == PharmacyOrder.ReadyForCollection)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, PharmacyOrder.Completed));
        if (updated == 1 && order.PatientEncounterId.HasValue)
        {
            var encounter = await _context.PatientEncounters.FindAsync(order.PatientEncounterId.Value);
            if (encounter != null && encounter.ClosedAt == null)
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                _encounterService.Transition(
                    encounter,
                    order.PrescriberId == null ? PatientEncounter.InProgress : PatientEncounter.AwaitingClinician,
                    "Consultation",
                    order.PrescriberId == null
                        ? "Your medicines were collected. Follow the instructions provided by your care team."
                        : "Your medicines were collected. Please return to your clinician for follow-up.",
                    user);
                var triageEntry = await _context.TriageEntries
                    .Where(entry => entry.PatientEncounterId == encounter.Id)
                    .OrderByDescending(entry => entry.TriageDate)
                    .FirstOrDefaultAsync();
                if (triageEntry != null)
                {
                    triageEntry.Status = "PendingDoctor";
                    triageEntry.NextStep = "Doctor";
                }
                if (order.PrescriberId != null)
                {
                    encounter.AssignedClinicianId = order.PrescriberId;
                    _context.QueueEntries.Add(new QueueEntry
                    {
                        PatientId = order.PatientId,
                        PatientEncounter = encounter,
                        AssignedById = user,
                        AssignedDoctorId = order.PrescriberId,
                        QueueType = "Doctor",
                        Status = "Waiting",
                        Notes = "Prescription dispensed; clinician follow-up requested."
                    });
                }
                var pharmacyQueue = await _context.QueueEntries
                    .Where(queue =>
                        queue.PatientId == order.PatientId &&
                        queue.QueueType == "Pharmacy" &&
                        queue.Status == "Waiting")
                    .OrderByDescending(queue => queue.CreatedAt)
                    .FirstOrDefaultAsync();
                if (pharmacyQueue != null)
                {
                    pharmacyQueue.Status = "Processed";
                    pharmacyQueue.ProcessedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
        }
        TempData[updated == 1 ? "PharmacyOrderMessage" : "PharmacyOrderError"] =
            updated == 1 ? $"Request {order.OrderNumber} was marked collected." : "This request is not ready for collection.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetPrescriptionAsync(int orderId)
    {
        var order = await _context.PharmacyOrders
            .AsNoTracking()
            .Where(item => item.Id == orderId)
            .Select(item => new { item.PrescriptionDocument, item.PrescriptionContentType })
            .SingleOrDefaultAsync();
        if (order?.PrescriptionDocument == null || order.PrescriptionContentType == null)
        {
            return NotFound();
        }

        Response.Headers["Cache-Control"] = "no-store";
        var extension = order.PrescriptionContentType switch
        {
            "application/pdf" => ".pdf",
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            _ => throw new InvalidOperationException("The stored prescription has an unsupported media type.")
        };
        return File(_prescriptionProtector.Unprotect(order.PrescriptionDocument), order.PrescriptionContentType, $"prescription-{orderId}{extension}");
    }

    private async Task LoadOrdersAsync()
    {
        Orders = await _context.PharmacyOrders
            .AsNoTracking()
            .Include(order => order.BillingInvoice)
            .Include(order => order.Prescriber)
            .Where(order => order.Status != PharmacyOrder.Completed && order.Status != PharmacyOrder.Rejected)
            .OrderBy(order => order.PlacedAt)
            .Select(order => new PharmacyOrder
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                PatientId = order.PatientId,
                PlacedAt = order.PlacedAt,
                Status = order.Status,
                PatientNote = order.PatientNote,
                PrescriptionText = order.PrescriptionText,
                PrescriptionContentType = order.PrescriptionContentType,
                Patient = order.Patient,
                Prescriber = order.Prescriber,
                BillingInvoice = order.BillingInvoice == null ? null : new BillingInvoice
                {
                    Status = order.BillingInvoice.Status
                },
                Items = order.Items.Select(item => new PharmacyOrderItem
                {
                    DrugName = item.DrugName,
                    Quantity = item.Quantity,
                    RequiresPrescription = item.RequiresPrescription
                }).ToList()
            })
            .ToListAsync();
    }
}
