using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using HospitalWebApp.Models;
using HospitalWebApp.Data;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Services;

namespace HospitalWebApp.Areas.Identity.Pages
{
    [Authorize(Roles = "Accounts,Cashier,Admin")]
    public class AccountsDashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly PatientEncounterService _encounterService;

        public AccountsDashboardModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            PatientEncounterService encounterService)
        {
            _userManager = userManager;
            _context = context;
            _encounterService = encounterService;
        }

        public IList<BillingInvoice> Invoices { get; set; } = new List<BillingInvoice>();
        public IList<PatientEncounter> ReadyToLeaveEncounters { get; set; } = new List<PatientEncounter>();
        public bool CanViewPatientInvoices { get; private set; }
        public int TotalInvoicesCount { get; private set; }
        public int PaidInvoicesCount { get; private set; }
        public int OutstandingInvoicesCount { get; private set; }
        public decimal OutstandingBalance { get; private set; }
        public string Message { get; set; } = string.Empty;

        [BindProperty]
        public int InvoiceId { get; set; }
        [BindProperty]
        public decimal Amount { get; set; }
        [BindProperty]
        public string Method { get; set; } = "";
        [BindProperty]
        public string Reference { get; set; } = "";
        [BindProperty]
        public string RequestKey { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            CanViewPatientInvoices =
                await _userManager.IsInRoleAsync(user, "Admin") ||
                await _userManager.IsInRoleAsync(user, "Cashier");

            TotalInvoicesCount = await _context.BillingInvoices.CountAsync();
            PaidInvoicesCount = await _context.BillingInvoices.CountAsync(invoice => invoice.Status == "Paid");
            OutstandingInvoicesCount = await _context.BillingInvoices.CountAsync(invoice => invoice.Status != "Paid");
            OutstandingBalance = await _context.BillingInvoices
                .Where(invoice => invoice.Status != "Paid")
                .SumAsync(invoice => invoice.TotalAmount - invoice.PaidAmount);

            if (CanViewPatientInvoices)
            {
                Invoices = await _context.BillingInvoices
                    .Include(invoice => invoice.Patient)
                    .Include(invoice => invoice.Payments)
                    .OrderByDescending(invoice => invoice.IssuedAt)
                    .Take(200)
                    .ToListAsync();

                ReadyToLeaveEncounters = await _context.PatientEncounters
                    .AsNoTracking()
                    .Include(encounter => encounter.Patient)
                    .Where(encounter => encounter.Status == PatientEncounter.ReadyToLeave)
                    .OrderBy(encounter => encounter.LastUpdatedAt)
                    .Take(100)
                    .ToListAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostMarkPaidAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
            if (!await _userManager.IsInRoleAsync(user, "Admin") &&
                !await _userManager.IsInRoleAsync(user, "Cashier"))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(RequestKey) || RequestKey.Length > 40)
            {
                TempData["PaymentError"] = "Payment request is invalid. Refresh the cashier page and try again.";
                return RedirectToPage();
            }
            var existingPayment = await _context.BillingPayments
                .AsNoTracking()
                .SingleOrDefaultAsync(payment => payment.RequestKey == RequestKey);
            if (existingPayment != null)
            {
                TempData["PaymentMessage"] = $"This payment was already recorded. Receipt: {existingPayment.ReceiptNumber}";
                return RedirectToPage();
            }

            var invoice = await _context.BillingInvoices.FindAsync(InvoiceId);
            if (invoice == null)
            {
                Message = "Invoice not found.";
                return await OnGetAsync();
            }

            if (Amount <= 0 ||
                decimal.Round(Amount, 2) != Amount ||
                Amount > invoice.TotalAmount - invoice.PaidAmount ||
                Method is not ("Cash" or "MPesa" or "Visa" or "Insurance") ||
                Reference?.Length > 100)
            {
                Message = "Enter a valid payment amount, method and reference. The amount cannot exceed the remaining balance.";
                return await OnGetAsync();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _context.Entry(invoice).ReloadAsync();
            if (Amount > invoice.TotalAmount - invoice.PaidAmount)
            {
                await transaction.RollbackAsync();
                TempData["PaymentError"] = "The invoice balance changed before payment was recorded. Refresh and retry.";
                return RedirectToPage();
            }
            invoice.PaidAmount += Amount;
            invoice.Status = invoice.PaidAmount >= invoice.TotalAmount ? "Paid" : "Partial";
            invoice.PaymentConfirmed = invoice.Status == "Paid";
            invoice.PaymentMethod = Method.Trim();

            var payment = new BillingPayment
            {
                Invoice = invoice,
                ReceiptNumber = $"IH-{Guid.NewGuid():N}".ToUpperInvariant(),
                Amount = Amount,
                BalanceAfter = Math.Max(0, invoice.TotalAmount - invoice.PaidAmount),
                Method = Method.Trim(),
                Reference = Reference?.Trim() ?? string.Empty,
                ReceivedById = user.Id,
                ReceivedAt = DateTime.UtcNow,
                Status = BillingPayment.Completed,
                RequestKey = RequestKey
            };
            _context.BillingPayments.Add(payment);

            if (invoice.AppointmentId.HasValue)
            {
                var appt = await _context.Appointments.FindAsync(invoice.AppointmentId.Value);
                if (appt != null)
                {
                    appt.PaymentConfirmed = invoice.Status == "Paid";
                    appt.PaymentMethod = invoice.PaymentMethod;
                    appt.PaymentReference = payment.Reference;
                }
            }

            if (invoice.PatientEncounterId.HasValue)
            {
                var encounter = await _context.PatientEncounters
                    .SingleOrDefaultAsync(item => item.Id == invoice.PatientEncounterId.Value);
                var pharmacyAwaitingCollection = await _context.PharmacyOrders.AnyAsync(order =>
                    order.BillingInvoiceId == invoice.Id &&
                    order.Status == PharmacyOrder.ReadyForCollection);
                var hasOutstandingInvoices = await _context.BillingInvoices
                    .AnyAsync(item =>
                        item.PatientEncounterId == invoice.PatientEncounterId &&
                        item.Id != invoice.Id &&
                        item.Status != "Paid");
                if (encounter != null && invoice.Status == "Paid" && pharmacyAwaitingCollection)
                {
                    _encounterService.Transition(
                        encounter,
                        PatientEncounter.AwaitingPharmacy,
                        "Pharmacy",
                        "Your pharmacy bill is settled. Please collect your prepared medicines.",
                        user.Id);
                }
                else if (encounter?.Status == PatientEncounter.ReadyToLeave &&
                         invoice.Status == "Paid" &&
                         !hasOutstandingInvoices)
                {
                    _encounterService.Transition(
                        encounter,
                        PatientEncounter.Completed,
                        "Discharged",
                        "Your visit is complete. Thank you for visiting Immaculate Heart Hospital.",
                        user.Id,
                        close: true);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["PaymentMessage"] = "Payment recorded. Receipt: " + payment.ReceiptNumber;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCompleteVisitAsync(int encounterId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }
            if (!await _userManager.IsInRoleAsync(user, "Admin") &&
                !await _userManager.IsInRoleAsync(user, "Cashier"))
            {
                return Forbid();
            }

            var encounter = await _context.PatientEncounters.FindAsync(encounterId);
            if (encounter == null)
            {
                return NotFound();
            }
            if (encounter.Status != PatientEncounter.ReadyToLeave || encounter.ClosedAt.HasValue)
            {
                TempData["PaymentError"] = "This visit is not waiting for cashier discharge.";
                return RedirectToPage();
            }

            var hasOutstandingInvoices = await _context.BillingInvoices
                .AnyAsync(invoice =>
                    invoice.PatientEncounterId == encounter.Id &&
                    invoice.Status != "Paid");
            if (hasOutstandingInvoices)
            {
                TempData["PaymentError"] = "Outstanding charges must be settled before the visit can be closed.";
                return RedirectToPage();
            }

            _encounterService.Transition(
                encounter,
                PatientEncounter.Completed,
                "Discharged",
                "Your visit is complete. Thank you for visiting Immaculate Heart Hospital.",
                user.Id,
                close: true);
            await _context.SaveChangesAsync();
            TempData["PaymentMessage"] = "Visit closed successfully.";
            return RedirectToPage();
        }
    }
}
