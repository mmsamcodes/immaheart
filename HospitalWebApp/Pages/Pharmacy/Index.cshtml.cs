using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Services;

namespace HospitalWebApp.Pages.Pharmacy;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private const long MaxPrescriptionSize = 5 * 1024 * 1024;
    private static readonly string[] StaffRoles =
    [
        "Admin", "Doctor", "Accounts", "Cashier", "Reception", "Nurse", "LabTech", "Pharmacy"
    ];

    private readonly ApplicationDbContext _context;
    private readonly IDataProtector _prescriptionProtector;
    private readonly PatientEncounterService _encounterService;

    public IndexModel(
        ApplicationDbContext context,
        IDataProtectionProvider dataProtectionProvider,
        PatientEncounterService encounterService)
    {
        _context = context;
        _prescriptionProtector = dataProtectionProvider.CreateProtector("HospitalWebApp.PharmacyPrescription.v1");
        _encounterService = encounterService;
    }

    public IList<Drug> Products { get; private set; } = new List<Drug>();
    public IList<PharmacyOrder> MyOrders { get; private set; } = new List<PharmacyOrder>();
    public IList<string> Categories { get; private set; } = new List<string>();
    public bool CanPlaceOrders { get; private set; }

    [BindProperty]
    public Dictionary<int, int> Quantities { get; set; } = new();

    [BindProperty]
    [StringLength(500)]
    public string PatientNote { get; set; } = string.Empty;

    [BindProperty]
    public IFormFile? PrescriptionFile { get; set; }

    public async Task OnGetAsync(string? search, string? category)
    {
        await LoadPageAsync(search, category);
    }

    public async Task<IActionResult> OnPostSubmitOrderAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Account/Login", new
            {
                area = "Identity",
                returnUrl = Url.Page("/Pharmacy/Index")
            });
        }

        if (StaffRoles.Any(User.IsInRole))
        {
            return Forbid();
        }

        var patientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(patientId))
        {
            return Forbid();
        }

        if (Quantities.Values.Any(quantity => quantity < 0))
        {
            ModelState.AddModelError(nameof(Quantities), "Enter a quantity of zero or more.");
        }

        var requested = Quantities.Where(pair => pair.Value > 0)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        if (requested.Count == 0)
        {
            ModelState.AddModelError(nameof(Quantities), "Choose at least one item to request.");
        }

        var drugs = await _context.Drugs
            .Where(drug => requested.Keys.Contains(drug.Id) && drug.IsActive)
            .ToListAsync();
        if (drugs.Count != requested.Count)
        {
            ModelState.AddModelError(nameof(Quantities), "One or more selected items are no longer available.");
        }

        foreach (var drug in drugs)
        {
            var quantity = requested[drug.Id];
            if (quantity > drug.StockQuantity)
            {
                ModelState.AddModelError(nameof(Quantities), $"{drug.Name} has only {drug.StockQuantity} available.");
            }
        }

        var prescription = await ReadPrescriptionAsync();
        if (prescription == null && drugs.Any(drug => drug.RequiresPrescription))
        {
            ModelState.AddModelError(nameof(PrescriptionFile), "Upload a current prescription for prescription-required items.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPageAsync();
            return Page();
        }

        var encounter = await _encounterService.FindActiveAsync(patientId);
        var order = new PharmacyOrder
        {
            PatientId = patientId,
            PatientEncounter = encounter,
            Status = PharmacyOrder.PendingReview,
            PatientNote = (PatientNote ?? string.Empty).Trim(),
            PrescriptionDocument = prescription == null ? null : _prescriptionProtector.Protect(prescription.Value.Bytes),
            PrescriptionContentType = prescription?.ContentType,
            Items = drugs.Select(drug => new PharmacyOrderItem
            {
                DrugId = drug.Id,
                DrugName = drug.Name,
                Quantity = requested[drug.Id],
                UnitPrice = drug.UnitPrice,
                RequiresPrescription = drug.RequiresPrescription
            }).ToList()
        };

        _context.PharmacyOrders.Add(order);
        await _context.SaveChangesAsync();

        TempData["PharmacyOrderMessage"] = $"Request {order.OrderNumber} was submitted for pharmacist review.";
        return RedirectToPage();
    }

    private async Task<(byte[] Bytes, string ContentType)?> ReadPrescriptionAsync()
    {
        if (PrescriptionFile == null || PrescriptionFile.Length == 0)
        {
            return null;
        }

        if (PrescriptionFile.Length > MaxPrescriptionSize)
        {
            ModelState.AddModelError(nameof(PrescriptionFile), "Prescription files must be 5 MB or smaller.");
            return null;
        }

        await using var memoryStream = new MemoryStream();
        await PrescriptionFile.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        if (bytes.AsSpan().StartsWith("%PDF-"u8))
        {
            return (bytes, "application/pdf");
        }

        if (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
        {
            return (bytes, "image/png");
        }

        if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff)
        {
            return (bytes, "image/jpeg");
        }

        ModelState.AddModelError(nameof(PrescriptionFile), "Upload a valid PDF, PNG, or JPEG prescription.");
        return null;
    }

    private async Task LoadPageAsync(string? search = null, string? category = null)
    {
        search = search?.Trim();
        category = category?.Trim();

        var query = _context.Drugs
            .AsNoTracking()
            .Where(drug => drug.IsActive && drug.StockQuantity > 0);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(drug => drug.Name.Contains(search) ||
                                        drug.Category.Contains(search) ||
                                        drug.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(drug => drug.Category == category);
        }

        Products = await query.OrderBy(drug => drug.Category).ThenBy(drug => drug.Name)
            .Take(100)
            .ToListAsync();
        Categories = await _context.Drugs.AsNoTracking()
            .Where(drug => drug.IsActive && drug.StockQuantity > 0 && drug.Category != string.Empty)
            .Select(drug => drug.Category)
            .Distinct()
            .OrderBy(value => value)
            .ToListAsync();

        CanPlaceOrders = User.Identity?.IsAuthenticated == true && !StaffRoles.Any(User.IsInRole);
        if (CanPlaceOrders)
        {
            var patientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(patientId))
            {
                MyOrders = await _context.PharmacyOrders.AsNoTracking()
                    .Where(order => order.PatientId == patientId)
                    .OrderByDescending(order => order.PlacedAt)
                    .Take(10)
                    .Select(order => new PharmacyOrder
                    {
                        Id = order.Id,
                        OrderNumber = order.OrderNumber,
                        PatientId = order.PatientId,
                        PlacedAt = order.PlacedAt,
                        Status = order.Status,
                        Items = order.Items.Select(item => new PharmacyOrderItem
                        {
                            DrugName = item.DrugName,
                            Quantity = item.Quantity
                        }).ToList()
                    })
                    .ToListAsync();
            }
        }
    }
}
