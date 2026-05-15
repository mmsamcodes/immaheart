using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;

namespace HospitalWebApp.Pages_Admissions
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Admission> Admissions { get; set; } = new List<Admission>();

        public async Task OnGetAsync()
        {
            Admissions = await _context.Admissions
                .Include(a => a.Patient)
                .Include(a => a.AdmittedBy)
                .ToListAsync();
        }
    }
}
