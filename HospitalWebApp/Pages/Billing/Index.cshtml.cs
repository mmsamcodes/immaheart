using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HospitalWebApp.Pages_Billing
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<BillingInvoice> Invoices { get; set; } = new List<BillingInvoice>();

        public async Task OnGetAsync()
        {
            var query = _context.BillingInvoices.Include(i => i.Patient).Include(i => i.Items).AsQueryable();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && !User.IsInRole("Accounts") && !User.IsInRole("Cashier"))
            {
                query = query.Where(i => i.PatientId == userId);
            }
            Invoices = await query.OrderByDescending(i => i.IssuedAt).ToListAsync();
        }
    }
}
