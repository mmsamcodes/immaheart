using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HospitalWebApp.Pages_Labs
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<LabOrder> LabOrders { get; set; } = new List<LabOrder>();

        public async Task OnGetAsync()
        {
            var query = _context.LabOrders.Include(l => l.Patient).Include(l => l.Doctor).AsQueryable();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && !User.IsInRole("Doctor") &&
                !User.IsInRole("LabTech") && !User.IsInRole("Nurse"))
            {
                query = query.Where(l => l.PatientId == userId);
            }
            LabOrders = await query.OrderByDescending(l => l.OrderedAt).ToListAsync();
        }
    }
}
