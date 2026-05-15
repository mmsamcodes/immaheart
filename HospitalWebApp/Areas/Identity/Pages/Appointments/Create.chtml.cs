using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity.UI.Services;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Identity;

namespace HospitalWebApp.Pages.Appointments
{
    public class CreateModel : PageModel
    {
        private readonly HospitalWebApp.Data.ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(
            HospitalWebApp.Data.ApplicationDbContext context, 
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        public IActionResult OnGet() => Page();

        [BindProperty]
        public Appointment Appointment { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // Link to the logged-in patient
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                Appointment.PatientId = user.Id;
            }

            // Save to the Database
            _context.Appointments.Add(Appointment);
            await _context.SaveChangesAsync();

            // SEND EMAIL TO YOUR HOSPITAL DOMAIN
            string emailBody = $@"
                <h3>New Appointment Request</h3>
                <p><b>Patient:</b> {user?.FullName ?? User.Identity?.Name}</p>
                <p><b>Date:</b> {Appointment.AppointmentDate}</p>
                <p><b>Symptoms:</b> {Appointment.SymptomsSummary}</p>
                <p><b>Visit Type:</b> {(Appointment.IsVirtual ? "Virtual" : "In-Person")}</p>";

            await _emailSender.SendEmailAsync(
                "info@immaculatehearthospitalkereita.org", 
                "New Patient Appointment Request", 
                emailBody
            );

            return RedirectToPage("./Index");
        }
    }
}