using System.Security.Claims;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Services;
using Stripe;
using Stripe.Checkout;

namespace HospitalWebApp.Pages_Billing;

[Authorize(Roles = "Patient")]
public class PaymentSuccessModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PatientEncounterService _encounterService;

    public PaymentSuccessModel(
        ApplicationDbContext context,
        IConfiguration configuration,
        PatientEncounterService encounterService)
    {
        _context = context;
        _configuration = configuration;
        _encounterService = encounterService;
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
            if (session.PaymentStatus == "paid" &&
                session.Status == "complete" &&
                session.ClientReferenceId == Invoice.InvoiceNumber)
            {
                var payment = await _context.BillingPayments
                    .SingleOrDefaultAsync(item =>
                        item.InvoiceId == Invoice.Id &&
                        item.GatewayReference == sessionId);
                if (payment == null)
                {
                    StatusMessage = "The verified checkout does not match a pending invoice payment. Contact the cashier.";
                    return Page();
                }

                if (payment.Status == BillingPayment.Pending)
                {
                    if (session.AmountTotal != decimal.ToInt64(payment.Amount * 100m) ||
                        Invoice.PaidAmount + payment.Amount > Invoice.TotalAmount)
                    {
                        StatusMessage = "The verified payment amount does not match the invoice balance. Contact the cashier.";
                        return Page();
                    }

                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    var claimed = await _context.BillingPayments
                        .Where(item => item.Id == payment.Id && item.Status == BillingPayment.Pending)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, BillingPayment.Completed));
                    if (claimed != 1)
                    {
                        await transaction.RollbackAsync();
                        StatusMessage = "This payment has already been processed.";
                        return Page();
                    }

                    await _context.Entry(Invoice).ReloadAsync();
                    if (payment.Amount > Invoice.TotalAmount - Invoice.PaidAmount)
                    {
                        await transaction.RollbackAsync();
                        StatusMessage = "The invoice balance changed before payment confirmation. Contact the cashier.";
                        return Page();
                    }

                    Invoice.PaidAmount += payment.Amount;
                    Invoice.Status = Invoice.PaidAmount >= Invoice.TotalAmount
                        ? "Paid"
                        : "Partial";
                    Invoice.PaymentConfirmed = Invoice.Status == "Paid";
                    Invoice.PaymentMethod = "Stripe";
                    payment.Status = BillingPayment.Completed;
                    payment.ReceiptNumber = $"STRIPE-{Guid.NewGuid():N}".ToUpperInvariant();
                    payment.Reference = sessionId;
                    payment.ReceivedAt = DateTime.UtcNow;
                    payment.BalanceAfter = Math.Max(0, Invoice.TotalAmount - Invoice.PaidAmount);

                    if (Invoice.Status == "Paid" && Invoice.PatientEncounterId.HasValue)
                    {
                        var encounter = await _context.PatientEncounters
                            .SingleOrDefaultAsync(item => item.Id == Invoice.PatientEncounterId.Value);
                        if (encounter != null)
                        {
                            var pharmacyAwaitingCollection = await _context.PharmacyOrders.AnyAsync(order =>
                                order.BillingInvoiceId == Invoice.Id &&
                                order.Status == HospitalWebApp.Models.PharmacyOrder.ReadyForCollection);
                            var otherOutstandingInvoices = await _context.BillingInvoices.AnyAsync(invoice =>
                                invoice.PatientEncounterId == encounter.Id &&
                                invoice.Id != Invoice.Id &&
                                invoice.Status != "Paid");
                            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                            if (pharmacyAwaitingCollection)
                            {
                                _encounterService.Transition(
                                    encounter,
                                    HospitalWebApp.Models.PatientEncounter.AwaitingPharmacy,
                                    "Pharmacy",
                                    "Your pharmacy bill is settled. Please collect your prepared medicines.",
                                    userId);
                            }
                            else if (encounter.Status == HospitalWebApp.Models.PatientEncounter.ReadyToLeave &&
                                     !otherOutstandingInvoices)
                            {
                                _encounterService.Transition(
                                    encounter,
                                    HospitalWebApp.Models.PatientEncounter.Completed,
                                    "Discharged",
                                    "Your visit is complete. Thank you for visiting Immaculate Heart Hospital.",
                                    userId,
                                    close: true);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                else if (payment.Status != BillingPayment.Completed)
                {
                    StatusMessage = "The payment is not available for confirmation. Contact the cashier.";
                    return Page();
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

        return User.IsInRole("Patient") && invoice.PatientId == userId;
    }
}
