using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Areas.Identity.Pages;

[Authorize(Roles = "Pharmacy")]
public class PharmacyOrdersModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IDataProtector _prescriptionProtector;

    public PharmacyOrdersModel(ApplicationDbContext context, IDataProtectionProvider dataProtectionProvider)
    {
        _context = context;
        _prescriptionProtector = dataProtectionProvider.CreateProtector("HospitalWebApp.PharmacyPrescription.v1");
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
            if (prescriptionRequired && order.PrescriptionDocument == null)
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
            .Where(item => item.Id == orderId && item.Status == PharmacyOrder.Approved)
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

        await transaction.CommitAsync();
        TempData["PharmacyOrderMessage"] = $"Request {order.OrderNumber} is ready for hospital collection.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCollectedAsync(int orderId)
    {
        var order = await _context.PharmacyOrders
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == orderId);
        if (order == null)
        {
            return NotFound();
        }

        var updated = await _context.PharmacyOrders
            .Where(item => item.Id == orderId && item.Status == PharmacyOrder.ReadyForCollection)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, PharmacyOrder.Completed));
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
                PrescriptionContentType = order.PrescriptionContentType,
                Patient = order.Patient,
                Items = order.Items.Select(item => new PharmacyOrderItem
                {
                    DrugName = item.DrugName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    RequiresPrescription = item.RequiresPrescription
                }).ToList()
            })
            .ToListAsync();
    }
}
