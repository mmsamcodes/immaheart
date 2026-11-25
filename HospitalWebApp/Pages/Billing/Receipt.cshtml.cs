using System.Security.Claims;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Pages_Billing;

[Authorize(Roles = "Patient,Cashier,Admin")]
public class ReceiptModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ReceiptModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public BillingPayment? Payment { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Payment = await _context.BillingPayments
            .AsNoTracking()
            .Include(payment => payment.Invoice)
                .ThenInclude(invoice => invoice!.Patient)
            .Include(payment => payment.Invoice)
                .ThenInclude(invoice => invoice!.Items)
            .Include(payment => payment.ReceivedBy)
            .SingleOrDefaultAsync(payment => payment.Id == id);
        if (Payment == null)
        {
            return NotFound();
        }

        if (User.IsInRole("Patient"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Payment.Invoice?.PatientId != userId)
            {
                return Forbid();
            }
        }
        else if (!User.IsInRole("Cashier") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        Response.Headers["Cache-Control"] = "no-store";
        return Page();
    }
}
