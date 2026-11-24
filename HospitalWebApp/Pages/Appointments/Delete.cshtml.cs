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
    public class DeleteModel : PageModel
    {
        private readonly HospitalWebApp.Data.ApplicationDbContext _context;

        public DeleteModel(HospitalWebApp.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
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
                if (!CanCancel(appointment) || appointment.Status == "Completed" || appointment.Status == "Cancelled")
                {
                    return Forbid();
                }

                Appointment = appointment;

                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                if (!CanCancel(appointment) || appointment.Status == "Completed")
                {
                    return Forbid();
                }

                Appointment = appointment;
                Appointment.Status = "Cancelled";
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }

        private bool CanCancel(Appointment appointment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return User.IsInRole("Admin") || User.IsInRole("Reception") ||
                   appointment.PatientId == userId &&
                   (User.IsInRole("Patient") || !User.IsInRole("Doctor") && !User.IsInRole("Nurse") &&
                    !User.IsInRole("Accounts") && !User.IsInRole("Cashier") &&
                    !User.IsInRole("LabTech") && !User.IsInRole("Pharmacy"));
        }
    }
}
