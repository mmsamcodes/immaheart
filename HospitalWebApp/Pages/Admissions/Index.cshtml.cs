using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HospitalWebApp.Pages_Admissions
{
    [Authorize]
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
            var query = _context.Admissions.Include(a => a.Patient).Include(a => a.AdmittedBy).AsQueryable();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && !User.IsInRole("Doctor") &&
                !User.IsInRole("Nurse") && !User.IsInRole("Accounts"))
            {
                query = query.Where(a => a.PatientId == userId);
            }
            Admissions = await query.OrderByDescending(a => a.AdmissionDate).ToListAsync();
        }
    }
}
