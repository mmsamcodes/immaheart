using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using HospitalWebApp.Models;
using HospitalWebApp.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Areas.Identity.Pages
{
    [Authorize(Roles = "Accounts,Cashier,Admin")]
    public class AccountsDashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AccountsDashboardModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IList<BillingInvoice> Invoices { get; set; } = new List<BillingInvoice>();
        public string Message { get; set; } = string.Empty;

        [BindProperty]
        public int InvoiceId { get; set; }
        [BindProperty]
        public decimal Amount { get; set; }
        [BindProperty]
        public string Method { get; set; } = "";
        [BindProperty]
        public string Reference { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Allow accounts, admin, cashier, or reception users to access this page.
            Invoices = await _context.BillingInvoices
                .Include(i => i.Patient)
                .Include(i => i.Appointment)
                .OrderByDescending(i => i.IssuedAt)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostMarkPaidAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
            var invoice = await _context.BillingInvoices.FindAsync(InvoiceId);
            if (invoice == null)
            {
                Message = "Invoice not found.";
                return await OnGetAsync();
            }

            if (Amount <= 0 || Amount > invoice.TotalAmount - invoice.PaidAmount ||
                string.IsNullOrWhiteSpace(Method) || Method.Length > 50 || Reference?.Length > 100)
            {
                Message = "Enter a valid payment amount, method and reference. The amount cannot exceed the remaining balance.";
                return await OnGetAsync();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            invoice.PaidAmount += Amount;
            invoice.Status = invoice.PaidAmount >= invoice.TotalAmount ? "Paid" : "Partial";

            // Append a payment note for audit and tracking.
            invoice.Notes += $"\nPayment logged {DateTime.Now}: {Amount} via {Method} ref:{Reference} by {user.Email}";

            if (invoice.AppointmentId.HasValue)
            {
                var appt = await _context.Appointments.FindAsync(invoice.AppointmentId.Value);
                if (appt != null)
                {
                    appt.PaymentConfirmed = true;
                    appt.PaymentMethod = Method;
                    appt.PaymentReference = Reference;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            Message = "Payment logged successfully.";
            return RedirectToPage();
        }
    }
}
