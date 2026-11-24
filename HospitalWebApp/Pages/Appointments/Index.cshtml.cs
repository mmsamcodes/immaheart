using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HospitalWebApp.Pages_Appointments
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private static readonly string[] StaffRoles =
        [
            "Admin", "Doctor", "Accounts", "Cashier", "Reception", "Nurse", "LabTech", "Pharmacy"
        ];

        private readonly HospitalWebApp.Data.ApplicationDbContext _context;

        public IndexModel(HospitalWebApp.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Appointment> Appointment { get;set; } = default!;
        public bool CanCreate { get; private set; }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            CanCreate = User.IsInRole("Admin") || User.IsInRole("Reception") ||
                        User.IsInRole("Patient") || !StaffRoles.Any(User.IsInRole);
            var query = _context.Appointments.Include(a => a.Doctor).Include(a => a.Patient).AsQueryable();
            if (User.IsInRole("Doctor"))
                query = query.Where(a => a.DoctorId == userId);
            else if (!User.IsInRole("Admin") && !User.IsInRole("Reception"))
                query = query.Where(a => a.PatientId == userId);
            Appointment = await query.OrderBy(a => a.AppointmentDate).ToListAsync();
        }
    }
}
