using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using HospitalWebApp.Models;
using HospitalWebApp.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Areas.Identity.Pages
{
    public class AccountsDashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountsDashboardModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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

            // Allow accounts, admin, or cashier users to access this page.
            if (!await _userManager.IsInRoleAsync(user, "Accounts") && !await _userManager.IsInRoleAsync(user, "Admin") && !await _userManager.IsInRoleAsync(user, "Cashier"))
            {
                return RedirectToPage("/Dashboard");
            }

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
            if (!await _userManager.IsInRoleAsync(user, "Accounts") && !await _userManager.IsInRoleAsync(user, "Admin") && !await _userManager.IsInRoleAsync(user, "Cashier"))
            {
                return RedirectToPage("/Dashboard");
            }

            var invoice = await _context.BillingInvoices.FindAsync(InvoiceId);
            if (invoice == null)
            {
                Message = "Invoice not found.";
                return await OnGetAsync();
            }

            // Update the invoice with the payment amount and status.
            invoice.PaidAmount += Amount;
            invoice.Status = invoice.PaidAmount >= invoice.TotalAmount ? "Paid" : "Partial";

            // Append a payment note for audit and tracking.
            invoice.Notes += $"\nPayment logged {DateTime.Now}: {Amount} via {Method} ref:{Reference} by {user.Email}";

            await _context.SaveChangesAsync();

            // If the invoice is linked to an appointment, mark that appointment as paid.
            if (invoice.AppointmentId.HasValue)
            {
                var appt = await _context.Appointments.FindAsync(invoice.AppointmentId.Value);
                if (appt != null)
                {
                    appt.PaymentConfirmed = true;
                    appt.PaymentMethod = Method;
                    appt.PaymentReference = Reference;
                    await _context.SaveChangesAsync();
                }
            }

            Message = "Payment logged successfully.";
            return RedirectToPage();
        }
    }
}
