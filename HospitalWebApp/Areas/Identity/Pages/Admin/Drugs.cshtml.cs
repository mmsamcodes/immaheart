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
    [Authorize(Roles = "Admin")]
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
        public string Message { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            Drugs = await _context.Drugs.OrderBy(d => d.Name).ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Drugs = await _context.Drugs.OrderBy(d => d.Name).ToListAsync();
                return Page();
            }

            _context.Drugs.Add(InputDrug);
            await _context.SaveChangesAsync();
            Message = "Drug added successfully.";
            return RedirectToPage();
        }
    }
}
