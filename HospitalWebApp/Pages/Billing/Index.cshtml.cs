using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;

namespace HospitalWebApp.Pages_Billing
{
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
            Invoices = await _context.BillingInvoices
                .Include(i => i.Patient)
                .Include(i => i.Items)
                .ToListAsync();
        }
    }
}
