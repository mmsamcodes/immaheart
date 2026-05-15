using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Pages.Reception
{
    [Authorize(Roles = "Admin,Accounts,Cashier,Reception")]
    public class IntakeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public IntakeModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public ReceptionInputModel Input { get; set; } = new();

        public string Message { get; set; } = string.Empty;

        public class ReceptionInputModel
        {
            [Required]
            public string FullName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            public string PhoneNumber { get; set; } = string.Empty;

            public string IDNumber { get; set; } = string.Empty;
            public int Age { get; set; }
            public string BloodGroup { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            var patient = await _userManager.FindByEmailAsync(Input.Email);
            var isNewPatient = false;
            var tempPassword = string.Empty;

            if (patient == null)
            {
                tempPassword = GenerateSecurePassword();
                patient = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FullName = Input.FullName,
                    PhoneNumber = Input.PhoneNumber,
                    IDNumber = Input.IDNumber,
                    Age = Input.Age,
                    BloodGroup = Input.BloodGroup,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(patient, tempPassword);
                if (!createResult.Succeeded)
                {
                    Message = string.Join(" ", createResult.Errors.Select(e => e.Description));
                    return Page();
                }

                await _userManager.AddToRoleAsync(patient, "Patient");
                isNewPatient = true;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(patient.FullName))
                    patient.FullName = Input.FullName;
                if (string.IsNullOrWhiteSpace(patient.PhoneNumber))
                    patient.PhoneNumber = Input.PhoneNumber;
                if (string.IsNullOrWhiteSpace(patient.IDNumber))
                    patient.IDNumber = Input.IDNumber;
                patient.Age = patient.Age == 0 ? Input.Age : patient.Age;
                patient.BloodGroup = string.IsNullOrWhiteSpace(patient.BloodGroup) ? Input.BloodGroup : patient.BloodGroup;
                await _userManager.UpdateAsync(patient);
                if (!await _userManager.IsInRoleAsync(patient, "Patient"))
                {
                    await _userManager.AddToRoleAsync(patient, "Patient");
                }
            }

            _context.QueueEntries.Add(new QueueEntry
            {
                PatientId = patient.Id,
                AssignedById = user.Id,
                QueueType = "Triage",
                Status = "Waiting",
                Notes = string.IsNullOrWhiteSpace(Input.Notes) ? "Patient registered at reception and queued for triage." : Input.Notes,
                Room = "Triage"
            });

            await _context.SaveChangesAsync();

            if (isNewPatient)
            {
                await SendPatientWelcomeEmailAsync(patient, tempPassword);
            }

            Message = "Patient intake completed and queued for triage.";
            return RedirectToPage("/Queue/Index", new { queueType = "Triage" });
        }

        private string GenerateSecurePassword()
        {
            var random = new Random();
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specials = "@#$%&*!";
            var builder = new StringBuilder();
            builder.Append(upper[random.Next(upper.Length)]);
            builder.Append(lower[random.Next(lower.Length)]);
            builder.Append(digits[random.Next(digits.Length)]);
            builder.Append(specials[random.Next(specials.Length)]);
            builder.Append(upper[random.Next(upper.Length)]);
            builder.Append(lower[random.Next(lower.Length)]);
            builder.Append(digits[random.Next(digits.Length)]);
            builder.Append(specials[random.Next(specials.Length)]);
            return builder.ToString();
        }

        private async Task SendPatientWelcomeEmailAsync(ApplicationUser patient, string password)
        {
            try
            {
                var loginUrl = Url.Page("/Account/Login", null, null, Request.Scheme);
                var message = $"<p>Dear {patient.FullName},</p>\r\n" +
                              $"<p>Your patient portal account has been created. Use the details below to sign in:</p>\r\n" +
                              $"<ul><li><strong>Email:</strong> {patient.Email}</li>" +
                              $"<li><strong>Temporary password:</strong> {password}</li></ul>" +
                              $"<p>Please log in, update your password, and review your care plan once you are logged in.</p>" +
                              $"<p><a href=\"{loginUrl}\">Login to patient portal</a></p>" +
                              $"<p>Thank you for choosing Immaculate Heart Hospital.</p>";

                await _emailSender.SendEmailAsync(patient.Email, "Your patient portal account is ready", message);
            }
            catch
            {
                // Keep the intake flow working even if email delivery fails.
            }
        }
    }
}
