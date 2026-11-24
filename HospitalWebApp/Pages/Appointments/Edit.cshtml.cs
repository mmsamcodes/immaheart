using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace HospitalWebApp.Pages_Appointments
{
    [Authorize(Roles = "Admin,Reception")]
    public class EditModel : PageModel
    {
        private readonly HospitalWebApp.Data.ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(HospitalWebApp.Data.ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Appointment Appointment { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments.FirstOrDefaultAsync(item => item.Id == id);
            if (appointment == null)
            {
                return NotFound();
            }
            Appointment = appointment;
           await LoadChoicesAsync();
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadChoicesAsync();
                return Page();
            }

            var current = await _context.Appointments.FindAsync(Appointment.Id);
            if (current == null)
            {
                return NotFound();
            }

            var doctor = await _userManager.FindByIdAsync(Appointment.DoctorId);
            var patient = await _userManager.FindByIdAsync(Appointment.PatientId);
            var allowedStatuses = new[] { "Pending", "Confirmed", "Completed", "Cancelled" };
            if (doctor == null || !await _userManager.IsInRoleAsync(doctor, "Doctor") ||
                patient == null || !await _userManager.IsInRoleAsync(patient, "Patient") ||
                !allowedStatuses.Contains(Appointment.Status))
            {
                ModelState.AddModelError(string.Empty, "Select a valid patient, doctor and appointment status.");
                await LoadChoicesAsync();
                return Page();
            }

            current.PatientId = Appointment.PatientId;
            current.DoctorId = Appointment.DoctorId;
            current.AppointmentDate = Appointment.AppointmentDate;
            current.SymptomsSummary = Appointment.SymptomsSummary;
            current.Status = Appointment.Status;
            current.IsVirtual = Appointment.IsVirtual;
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private async Task LoadChoicesAsync()
        {
            var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
            var patients = await _userManager.GetUsersInRoleAsync("Patient");
            ViewData["DoctorId"] = new SelectList(doctors.OrderBy(user => user.FullName), "Id", "FullName");
            ViewData["PatientId"] = new SelectList(patients.OrderBy(user => user.FullName), "Id", "FullName");
        }
    }
}
