using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Services;

namespace HospitalWebApp.Pages_Billing
{
    [Authorize]
    public class PayModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;

        public PayModel(ApplicationDbContext context, IPaymentService paymentService)
        {
            _context = context;
            _paymentService = paymentService;
        }

        public BillingInvoice? Invoice { get; set; }
        [BindProperty]
        public decimal Amount { get; set; }
        [BindProperty]
        public string Method { get; set; } = "MPesa";
        [BindProperty]
        public string PhoneNumber { get; set; } = string.Empty;
        [BindProperty]
        public string Reference { get; set; } = string.Empty;
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int invoiceId)
        {
            var invoice = await _context.BillingInvoices.Include(i => i.Patient).FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null) return RedirectToPage("/Billing/Index");
            if (!CanAccessInvoice(invoice)) return Forbid();

            Invoice = invoice;
            Amount = Invoice.TotalAmount - Invoice.PaidAmount;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int invoiceId)
        {
            var invoice = await _context.BillingInvoices.Include(i => i.Patient).FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null) return RedirectToPage("/Billing/Index");
            if (!CanAccessInvoice(invoice)) return Forbid();

            if (Amount <= 0 || Amount > invoice.TotalAmount - invoice.PaidAmount)
            {
                ErrorMessage = "Enter a valid payment amount not exceeding the outstanding balance.";
                Invoice = invoice;
                return Page();
            }

            try
            {
                if (Method == "MPesa")
                {
                    if (string.IsNullOrWhiteSpace(PhoneNumber))
                    {
                        ErrorMessage = "Phone number is required for MPesa payments.";
                        Invoice = invoice;
                        return Page();
                    }

                    var checkoutRequestId = await _paymentService.CreateMpesaStkPushAsync(PhoneNumber, Amount, invoice.InvoiceNumber, "Hospital invoice payment");
                    invoice.Status = "PendingMPesa";
                    invoice.Notes += $"\nMPesa checkout initiated at {DateTime.UtcNow}: {Amount} KES, ref={Reference}, checkoutId={checkoutRequestId}.";
                    await _context.SaveChangesAsync();

                    SuccessMessage = "MPesa payment request has been sent to your phone. Please complete the STK prompt.";
                    Amount = invoice.TotalAmount - invoice.PaidAmount;
                    Invoice = invoice;
                    return Page();
                }

                if (Method == "Visa")
                {
                    invoice.Status = "PendingStripe";
                    invoice.Notes += $"\nStripe checkout started at {DateTime.UtcNow}: {Amount} KES, ref={Reference}.";
                    await _context.SaveChangesAsync();

                    var successUrl = Url.Page("/Billing/PaymentSuccess", null, new { invoiceId = invoice.Id, sessionId = "{CHECKOUT_SESSION_ID}" }, Request.Scheme) ?? string.Empty;
                    var cancelUrl = Url.Page("/Billing/Pay", null, new { invoiceId = invoice.Id }, Request.Scheme) ?? string.Empty;
                    var checkoutUrl = await _paymentService.CreateStripeCheckoutSessionAsync(Amount, invoice.InvoiceNumber, successUrl, cancelUrl);
                    return Redirect(checkoutUrl);
                }

                ErrorMessage = "Selected payment method is not recognized.";
                Invoice = invoice;
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Invoice = invoice;
                return Page();
            }
        }

        private bool CanAccessInvoice(BillingInvoice invoice)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            if (invoice.PatientId == userId)
            {
                return true;
            }

            return User.IsInRole("Admin") || User.IsInRole("Accounts") || User.IsInRole("Cashier");
        }
    }
}
