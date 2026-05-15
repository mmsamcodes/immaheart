using System.Security.Claims;
using HospitalWebApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace HospitalWebApp.Pages_Billing;

[Authorize]
public class PaymentSuccessModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public PaymentSuccessModel(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public HospitalWebApp.Models.BillingInvoice? Invoice { get; set; }
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int invoiceId, string? sessionId)
    {
        Invoice = await _context.BillingInvoices.Include(i => i.Patient).FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (Invoice == null)
        {
            StatusMessage = "Invoice could not be found.";
            return Page();
        }

        if (!CanAccessInvoice(Invoice))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            StatusMessage = "Payment completed page reached without a payment session reference.";
            return Page();
        }

        var stripeSecret = _configuration["StripeSettings:SecretKey"];
        if (string.IsNullOrWhiteSpace(stripeSecret))
        {
            StatusMessage = "Stripe configuration is missing, so payment could not be verified.";
            return Page();
        }

        StripeConfiguration.ApiKey = stripeSecret;
        try
        {
            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(sessionId);
            if (session.PaymentStatus == "paid" && session.Status == "complete")
            {
                if (!Invoice.PaymentConfirmed)
                {
                    Invoice.PaidAmount = Invoice.TotalAmount;
                    Invoice.PaymentConfirmed = true;
                    Invoice.Status = "Paid";
                    Invoice.PaymentMethod = "Stripe";
                    Invoice.Notes += $"\nStripe payment confirmed at {DateTime.UtcNow}, session {sessionId}.";
                    await _context.SaveChangesAsync();
                }

                StatusMessage = "Payment confirmed successfully. Thank you.";
                return Page();
            }

            StatusMessage = $"Payment is not yet confirmed. Current status: {session.PaymentStatus}.";
            return Page();
        }
        catch (StripeException ex)
        {
            StatusMessage = $"Unable to verify Stripe payment: {ex.Message}";
            return Page();
        }
    }

    private bool CanAccessInvoice(HospitalWebApp.Models.BillingInvoice invoice)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        return invoice.PatientId == userId || User.IsInRole("Admin") || User.IsInRole("Accounts") || User.IsInRole("Cashier");
    }
}
