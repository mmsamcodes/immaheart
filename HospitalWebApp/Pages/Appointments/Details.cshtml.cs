using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using HospitalWebApp.Data;
using HospitalWebApp.Models;

namespace HospitalWebApp.Pages_Appointments
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly HospitalWebApp.Data.ApplicationDbContext _context;

        public DetailsModel(HospitalWebApp.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Appointment Appointment { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(item => item.Patient)
                .Include(item => item.Doctor)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (appointment is not null)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var canAccess = User.IsInRole("Admin") || User.IsInRole("Reception") ||
                                appointment.PatientId == userId ||
                                User.IsInRole("Doctor") && appointment.DoctorId == userId;
                if (!canAccess)
                {
                    return Forbid();
                }

                Appointment = appointment;

                return Page();
            }

            return NotFound();
        }
    }
}
