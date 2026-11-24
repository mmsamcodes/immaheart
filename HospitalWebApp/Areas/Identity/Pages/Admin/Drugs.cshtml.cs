using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Areas.Identity.Pages.Admin
{
    [Authorize(Roles = "Admin,Pharmacy")]
    public class DrugsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DrugsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Drug InputDrug { get; set; } = new();

        public IList<Drug> Drugs { get; set; } = new List<Drug>();
        [TempData]
        public string Message { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            await LoadDrugsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Drugs = await _context.Drugs.OrderBy(d => d.Name).ToListAsync();
                return Page();
            }

            InputDrug.Name = (InputDrug.Name ?? string.Empty).Trim();
            InputDrug.Code = (InputDrug.Code ?? string.Empty).Trim();
            InputDrug.Category = (InputDrug.Category ?? string.Empty).Trim();
            InputDrug.Unit = (InputDrug.Unit ?? string.Empty).Trim();
            InputDrug.Description = (InputDrug.Description ?? string.Empty).Trim();
            _context.Drugs.Add(InputDrug);
            await _context.SaveChangesAsync();
            Message = "Drug added successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync(int id)
        {
            if (id != InputDrug.Id)
            {
                return BadRequest();
            }

            var drug = await _context.Drugs.FindAsync(id);
            if (drug == null)
            {
                return NotFound();
            }

            if (!TryValidateModel(InputDrug, nameof(InputDrug)))
            {
                await LoadDrugsAsync();
                return Page();
            }

            drug.Name = (InputDrug.Name ?? string.Empty).Trim();
            drug.Code = (InputDrug.Code ?? string.Empty).Trim();
            drug.Category = (InputDrug.Category ?? string.Empty).Trim();
            drug.Unit = (InputDrug.Unit ?? string.Empty).Trim();
            drug.Description = (InputDrug.Description ?? string.Empty).Trim();
            drug.UnitPrice = InputDrug.UnitPrice;
            drug.StockQuantity = InputDrug.StockQuantity;
            drug.RequiresPrescription = InputDrug.RequiresPrescription;
            drug.IsActive = InputDrug.IsActive;

            await _context.SaveChangesAsync();
            Message = "Drug catalog and stock details were updated.";
            return RedirectToPage();
        }

        private async Task LoadDrugsAsync()
        {
            Drugs = await _context.Drugs.OrderBy(drug => drug.Name).ToListAsync();
        }
    }
}
