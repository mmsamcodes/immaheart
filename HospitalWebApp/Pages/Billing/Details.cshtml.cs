using System.Threading.Tasks;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Pages_Billing
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public BillingInvoice? Invoice { get; set; }
        public List<ServiceCatalog> Services { get; set; } = new();
        public List<Drug> Drugs { get; set; } = new();

        [BindProperty]
        public NewBillingItemInput ItemInput { get; set; } = new();

        [BindProperty]
        public InsuranceInputModel InsuranceInput { get; set; } = new();

        public class NewBillingItemInput
        {
            public string Description { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public int? ServiceId { get; set; }
            public int? DrugId { get; set; }
        }

        public class InsuranceInputModel
        {
            public string Scheme { get; set; } = string.Empty;
            public string Number { get; set; } = string.Empty;
            public decimal CoveragePercent { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Invoice = await _context.BillingInvoices
                .Include(i => i.Patient)
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (Invoice == null)
            {
                return NotFound();
            }

            if (!CanAccessInvoice(Invoice))
            {
                return Forbid();
            }

            // Load available services and drugs for quick selection
            Services = await _context.ServiceCatalogs.Where(s => s.IsAvailable).ToListAsync();
            Drugs = await _context.Drugs.Where(d => d.IsActive).ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddItemAsync(int id)
        {
            Invoice = await _context.BillingInvoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id);
            if (Invoice == null) return NotFound();
            if (!CanAccessInvoice(Invoice)) return Forbid();

            string description = ItemInput.Description;
            decimal amount = ItemInput.Amount;

            if (ItemInput.ServiceId.HasValue)
            {
                var svc = await _context.ServiceCatalogs.FindAsync(ItemInput.ServiceId.Value);
                if (svc != null)
                {
                    description = svc.Name;
                    amount = svc.Price;
                }
            }
            else if (ItemInput.DrugId.HasValue)
            {
                var drug = await _context.Drugs.FindAsync(ItemInput.DrugId.Value);
                if (drug != null)
                {
                    description = drug.Name;
                    amount = drug.UnitPrice;
                }
            }

            var item = new BillingItem
            {
                InvoiceId = Invoice.Id,
                Description = description,
                Amount = amount
            };
            _context.BillingItems.Add(item);
            Invoice.TotalAmount += amount;
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddInsuranceAsync(int id)
        {
            Invoice = await _context.BillingInvoices.FirstOrDefaultAsync(i => i.Id == id);
            if (Invoice == null) return NotFound();
            if (!CanAccessInvoice(Invoice)) return Forbid();

            Invoice.InsuranceScheme = InsuranceInput.Scheme;
            Invoice.InsuranceNumber = InsuranceInput.Number;
            Invoice.InsuranceCoveragePercent = InsuranceInput.CoveragePercent;

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        private bool CanAccessInvoice(BillingInvoice invoice)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Accounts") || User.IsInRole("Cashier"))
            {
                return true;
            }

            if (User.IsInRole("Patient") && invoice.PatientId == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value)
            {
                return true;
            }

            return User.IsInRole("Doctor") || User.IsInRole("Nurse") || User.IsInRole("Pharmacy") || User.IsInRole("LabTech");
        }
    }
}
