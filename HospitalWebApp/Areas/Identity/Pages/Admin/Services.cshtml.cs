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
    public class ServicesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ServicesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ServiceCatalog InputService { get; set; } = new();

        public IList<ServiceCatalog> Services { get; set; } = new List<ServiceCatalog>();
        public string Message { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            Services = await _context.ServiceCatalogs.OrderBy(s => s.Name).ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Services = await _context.ServiceCatalogs.OrderBy(s => s.Name).ToListAsync();
                return Page();
            }

            _context.ServiceCatalogs.Add(InputService);
            await _context.SaveChangesAsync();
            Message = "Service added successfully.";
            return RedirectToPage();
        }
    }
}
