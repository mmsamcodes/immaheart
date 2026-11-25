using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Services;
using Microsoft.Extensions.Logging;
using Stripe;

namespace HospitalWebApp.Pages_Billing
{
    [Authorize(Roles = "Patient")]
    public class PayModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PayModel> _logger;

        public PayModel(ApplicationDbContext context, IPaymentService paymentService, ILogger<PayModel> logger)
        {
            _context = context;
            _paymentService = paymentService;
            _logger = logger;
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

            if (Amount <= 0 ||
                Amount > invoice.TotalAmount - invoice.PaidAmount ||
                Reference?.Length > 100)
            {
                ErrorMessage = "Enter a valid payment amount not exceeding the outstanding balance.";
                Invoice = invoice;
                return Page();
            }

            try
            {
                if (Method == "MPesa")
                {
                    if (decimal.Truncate(Amount) != Amount)
                    {
                        ErrorMessage = "M-Pesa payments must be entered as whole Kenyan shillings.";
                        Invoice = invoice;
                        return Page();
                    }
                    if (string.IsNullOrWhiteSpace(PhoneNumber))
                    {
                        ErrorMessage = "Phone number is required for MPesa payments.";
                        Invoice = invoice;
                        return Page();
                    }

                    var checkoutRequestId = await _paymentService.CreateMpesaStkPushAsync(PhoneNumber, Amount, invoice.InvoiceNumber, "Hospital invoice payment");
                    invoice.Status = "PendingMPesa";
                    invoice.Notes += $"\nMPesa checkout initiated at {DateTime.UtcNow}: {Amount} KES, ref={Reference}, checkoutId={checkoutRequestId}.";
                    _context.BillingPayments.Add(new BillingPayment
                    {
                        InvoiceId = invoice.Id,
                        ReceiptNumber = $"PENDING-{Guid.NewGuid():N}",
                        Amount = Amount,
                        BalanceAfter = invoice.TotalAmount - invoice.PaidAmount,
                        Method = "MPesa",
                        Reference = Reference?.Trim() ?? string.Empty,
                        GatewayReference = checkoutRequestId,
                        Status = BillingPayment.Pending,
                        ReceivedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();

                    SuccessMessage = "MPesa payment request has been sent to your phone. Please complete the STK prompt.";
                    Amount = invoice.TotalAmount - invoice.PaidAmount;
                    Invoice = invoice;
                    return Page();
                }

                if (Method == "Visa")
                {
                    var successUrl = Url.Page("/Billing/PaymentSuccess", null, new { invoiceId = invoice.Id, sessionId = "{CHECKOUT_SESSION_ID}" }, Request.Scheme) ?? string.Empty;
                    var cancelUrl = Url.Page("/Billing/Pay", null, new { invoiceId = invoice.Id }, Request.Scheme) ?? string.Empty;
                    var checkout = await _paymentService.CreateStripeCheckoutSessionAsync(Amount, invoice.InvoiceNumber, successUrl, cancelUrl);
                    invoice.Status = "PendingStripe";
                    invoice.Notes += $"\nStripe checkout started at {DateTime.UtcNow}: {Amount} KES, ref={Reference}.";
                    _context.BillingPayments.Add(new BillingPayment
                    {
                        InvoiceId = invoice.Id,
                        ReceiptNumber = $"PENDING-{Guid.NewGuid():N}",
                        Amount = Amount,
                        BalanceAfter = invoice.TotalAmount - invoice.PaidAmount,
                        Method = "Stripe",
                        Reference = Reference?.Trim() ?? string.Empty,
                        GatewayReference = checkout.SessionId,
                        Status = BillingPayment.Pending,
                        ReceivedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                    return Redirect(checkout.Url);
                }

                ErrorMessage = "Selected payment method is not recognized.";
                Invoice = invoice;
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Unable to start online payment for invoice {InvoiceNumber}.", invoice.InvoiceNumber);
                ErrorMessage = "The selected payment service could not be started. Please try again or contact the cashier.";
                Invoice = invoice;
                return Page();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Payment service request failed for invoice {InvoiceNumber}.", invoice.InvoiceNumber);
                ErrorMessage = "The payment service is temporarily unavailable. Please try again or contact the cashier.";
                Invoice = invoice;
                return Page();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Unable to start Stripe checkout for invoice {InvoiceNumber}.", invoice.InvoiceNumber);
                ErrorMessage = "Card checkout could not be started. Please try again or contact the cashier.";
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

            return User.IsInRole("Patient") && invoice.PatientId == userId;
        }
    }
}
