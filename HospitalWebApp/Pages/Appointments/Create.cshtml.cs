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

namespace HospitalWebApp.Pages_Appointments
{
    public class CreateModel : PageModel
    {
        private readonly HospitalWebApp.Data.ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public CreateModel(HospitalWebApp.Data.ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public IActionResult OnGet()
        {
            ViewData["DoctorId"] = new SelectList(_context.Users, "Id", "FullName");
            ViewData["PatientId"] = new SelectList(_context.Users, "Id", "FullName");
            return Page();
        }

        [BindProperty]
        public Appointment Appointment { get; set; } = default!;

        // Handle form submission for a new appointment.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Save the appointment before creating the invoice record.
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
            return RedirectToPage("/Billing/Pay", new { invoiceId = invoice.Id });
        }
    }
}
