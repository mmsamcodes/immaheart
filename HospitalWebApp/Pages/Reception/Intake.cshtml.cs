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
using HospitalWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HospitalWebApp.Pages.Reception
{
    [Authorize(Roles = "Admin,Reception")]
    public class IntakeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly PatientEncounterService _encounterService;
        private readonly ILogger<IntakeModel> _logger;

        public IntakeModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            PatientEncounterService encounterService,
            ILogger<IntakeModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _encounterService = encounterService;
            _logger = logger;
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

            if (!await _roleManager.RoleExistsAsync("Patient"))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole("Patient"));
                if (!roleResult.Succeeded)
                {
                    Message = string.Join(" ", roleResult.Errors.Select(error => error.Description));
                    return Page();
                }
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

                var patientRoleResult = await _userManager.AddToRoleAsync(patient, "Patient");
                if (!patientRoleResult.Succeeded)
                {
                    Message = $"Patient account was created but could not be assigned the Patient role: {string.Join(" ", patientRoleResult.Errors.Select(error => error.Description))}";
                    return Page();
                }
                isNewPatient = true;
            }
            else
            {
                var existingRoles = await _userManager.GetRolesAsync(patient);
                if (existingRoles.Any(role => role != "Patient"))
                {
                    Message = "That email belongs to a staff account and cannot be registered as a patient.";
                    return Page();
                }

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
                    var patientRoleResult = await _userManager.AddToRoleAsync(patient, "Patient");
                    if (!patientRoleResult.Succeeded)
                    {
                        Message = $"Patient profile could not be assigned: {string.Join(" ", patientRoleResult.Errors.Select(error => error.Description))}";
                        return Page();
                    }
                }
            }

            var activeEncounter = await _encounterService.FindActiveAsync(patient.Id);
            if (activeEncounter != null)
            {
                Message = $"This patient already has an open visit ({activeEncounter.VisitNumber}). Continue that visit instead of creating a duplicate check-in.";
                return Page();
            }

            var encounter = _encounterService.Start(
                patient.Id,
                user.Id,
                PatientEncounter.AwaitingTriage,
                "Triage",
                "You are checked in and waiting for an initial nursing assessment.");

            _context.QueueEntries.Add(new QueueEntry
            {
                PatientId = patient.Id,
                PatientEncounter = encounter,
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
            if (string.IsNullOrWhiteSpace(patient.Email))
            {
                _logger.LogWarning("Welcome email was not sent because patient {PatientId} has no email address.", patient.Id);
                return;
            }

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
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send a welcome email to patient {PatientId}.", patient.Id);
            }
        }
    }
}
