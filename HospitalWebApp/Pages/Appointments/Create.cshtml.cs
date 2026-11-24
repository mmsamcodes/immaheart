using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace HospitalWebApp.Pages_Appointments
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly HospitalWebApp.Data.ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private static readonly string[] StaffRoles =
        [
            "Admin", "Doctor", "Accounts", "Cashier", "Reception", "Nurse", "LabTech", "Pharmacy"
        ];

        public CreateModel(HospitalWebApp.Data.ApplicationDbContext context, IEmailSender emailSender, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!CanBook())
            {
                return Forbid();
            }

            var user = await _userManager.GetUserAsync(User);
            Appointment = new Appointment
            {
                PatientId = CanBookForOthers() ? string.Empty : user?.Id ?? string.Empty,
                Status = "Pending",
                AppointmentDate = DateTime.Now.AddDays(1)
            };
            await LoadChoicesAsync();
            return Page();
        }

        [BindProperty]
        public Appointment Appointment { get; set; } = default!;
        public string CurrentPatientName { get; private set; } = string.Empty;

        // Handle form submission for a new appointment.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!CanBook())
            {
                return Forbid();
            }

            if (!CanBookForOthers())
            {
                Appointment.PatientId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            }

            if (!ModelState.IsValid)
            {
                await LoadChoicesAsync();
                return Page();
            }

            if (Appointment.AppointmentDate <= DateTime.Now)
            {
                ModelState.AddModelError("Appointment.AppointmentDate", "Choose a future appointment time.");
                await LoadChoicesAsync();
                return Page();
            }

            var doctor = await _userManager.FindByIdAsync(Appointment.DoctorId);
            if (doctor == null || !await _userManager.IsInRoleAsync(doctor, "Doctor"))
            {
                ModelState.AddModelError("Appointment.DoctorId", "Select an active doctor.");
                await LoadChoicesAsync();
                return Page();
            }

            if (CanBookForOthers())
            {
                var selectedPatient = await _userManager.FindByIdAsync(Appointment.PatientId);
                if (selectedPatient == null || !await _userManager.IsInRoleAsync(selectedPatient, "Patient"))
                {
                    ModelState.AddModelError("Appointment.PatientId", "Select a patient account.");
                    await LoadChoicesAsync();
                    return Page();
                }
            }
            else
            {
                Appointment.PatientId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            }

            Appointment.Id = 0;
            Appointment.Status = "Pending";
            Appointment.InvoiceId = null;
            Appointment.PaymentConfirmed = false;
            Appointment.PaymentMethod = string.Empty;
            Appointment.PaymentReference = string.Empty;
            Appointment.VideoSessionId = null;
            Appointment.VideoJoinUrl = null;
            Appointment.VirtualConfirmedByDoctor = false;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            _context.Appointments.Add(Appointment);
            await _context.SaveChangesAsync();

            // Create a billing invoice for the new appointment.
            var invoice = new BillingInvoice
            {
                PatientId = Appointment.PatientId,
                AppointmentId = Appointment.Id,
                IssuedAt = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14),
                TotalAmount = 1000m, // default consultation fee; replace with dynamic pricing as needed
                PaidAmount = 0m,
                Status = "Unpaid",
                Notes = "Auto-generated for consultation booking"
            };
            _context.BillingInvoices.Add(invoice);
            await _context.SaveChangesAsync();
            Appointment.InvoiceId = invoice.Id;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send a confirmation email to the patient if email settings are configured.
            var patient = await _context.Users.FindAsync(Appointment.PatientId);
            if (patient != null && !string.IsNullOrWhiteSpace(patient.Email))
            {
                var emailBody = $@"<p>Dear {WebUtility.HtmlEncode(patient.FullName)},</p>
<p>Your appointment has been successfully booked.</p>
<ul>
<li><strong>Date:</strong> {WebUtility.HtmlEncode(Appointment.AppointmentDate.ToString("yyyy-MM-dd HH:mm"))}</li>
<li><strong>Symptoms Summary:</strong> {WebUtility.HtmlEncode(Appointment.SymptomsSummary)}</li>
<li><strong>Invoice:</strong> {WebUtility.HtmlEncode(invoice.InvoiceNumber)}</li>
<li><strong>Amount Due:</strong> {WebUtility.HtmlEncode(invoice.TotalAmount.ToString("C"))}</li>
</ul>
<p>Please complete payment via the invoice payment page.</p>";

                try
                {
                    await _emailSender.SendEmailAsync(patient.Email, "Appointment confirmation and invoice", emailBody);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Email send failed: {ex.Message}");
                }
            }

            // Redirect the patient to the payment page.
            return CanBookForOthers()
                ? RedirectToPage("/Appointments/Index", new { area = "" })
                : RedirectToPage("/Billing/Pay", new { invoiceId = invoice.Id, area = "" });
        }

        private bool CanBookForOthers() => User.IsInRole("Admin") || User.IsInRole("Reception");

        private bool CanBook() => CanBookForOthers() ||
                                  !StaffRoles.Any(User.IsInRole) &&
                                  (User.IsInRole("Patient") || User.Identity?.IsAuthenticated == true);

        private async Task LoadChoicesAsync()
        {
            var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
            ViewData["DoctorId"] = new SelectList(doctors.OrderBy(user => user.FullName), "Id", "FullName");

            if (CanBookForOthers())
            {
                var patients = await _userManager.GetUsersInRoleAsync("Patient");
                ViewData["PatientId"] = new SelectList(patients.OrderBy(user => user.FullName), "Id", "FullName");
            }
            else
            {
                var patient = await _userManager.GetUserAsync(User);
                CurrentPatientName = patient?.FullName ?? string.Empty;
                ViewData["PatientId"] = new SelectList(patient == null ? Array.Empty<ApplicationUser>() : new[] { patient }, "Id", "FullName");
            }
        }
    }
}
