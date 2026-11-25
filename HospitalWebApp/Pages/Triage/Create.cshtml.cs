using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalWebApp.Services;

namespace HospitalWebApp.Pages.Triage
{
    [Authorize(Roles = "Nurse,Admin")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly PatientEncounterService _encounterService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            PatientEncounterService encounterService,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _encounterService = encounterService;
            _logger = logger;
        }

        [BindProperty]
        public TriageInputModel Input { get; set; } = new();

        public class TriageInputModel
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

            [Required]
            public string BloodPressure { get; set; } = string.Empty;

            [Required]
            public double TemperatureC { get; set; }

            [Required]
            public int HeartRate { get; set; }

            [Required]
            public int RespiratoryRate { get; set; }

            [Required]
            public int OxygenSaturation { get; set; }

            [Required]
            public double WeightKg { get; set; }

            [Required]
            public double HeightCm { get; set; }

            public string Symptoms { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
            public string CarePlanSummary { get; set; } = string.Empty;
            public string CarePlanGoals { get; set; } = string.Empty;
            public string PrimaryConcern { get; set; } = string.Empty;
        }

        public string Message { get; set; } = string.Empty;

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

            var nurse = await _userManager.GetUserAsync(User);
            if (nurse == null ||
                !await _userManager.IsInRoleAsync(nurse, "Nurse") &&
                !await _userManager.IsInRoleAsync(nurse, "Admin"))
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

                var addRoleResult = await _userManager.AddToRoleAsync(patient, "Patient");
                if (!addRoleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(patient);
                    Message = $"Patient account could not be assigned: {string.Join(" ", addRoleResult.Errors.Select(error => error.Description))}";
                    return Page();
                }
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
                    var addRoleResult = await _userManager.AddToRoleAsync(patient, "Patient");
                    if (!addRoleResult.Succeeded)
                    {
                        Message = $"Patient profile could not be assigned: {string.Join(" ", addRoleResult.Errors.Select(error => error.Description))}";
                        return Page();
                    }
                }
            }

            var encounter = await _encounterService.FindActiveAsync(patient.Id);
            if (encounter == null)
            {
                encounter = _encounterService.Start(
                    patient.Id,
                    nurse.Id,
                    PatientEncounter.AwaitingClinician,
                    "Consultation",
                    "Your initial assessment is complete. Please wait for clinician review.");
            }
            else
            {
                _encounterService.Transition(
                    encounter,
                    PatientEncounter.AwaitingClinician,
                    "Consultation",
                    "Your initial assessment is complete. Please wait for clinician review.",
                    nurse.Id);
            }

            var triage = new TriageEntry
            {
                PatientId = patient.Id,
                PatientEncounter = encounter,
                PatientName = Input.FullName,
                PatientEmail = Input.Email,
                PatientPhone = Input.PhoneNumber,
                PatientIdentifier = Input.IDNumber,
                NurseId = nurse.Id,
                BloodPressure = Input.BloodPressure,
                TemperatureC = Input.TemperatureC,
                HeartRate = Input.HeartRate,
                RespiratoryRate = Input.RespiratoryRate,
                OxygenSaturation = Input.OxygenSaturation,
                WeightKg = Input.WeightKg,
                HeightCm = Input.HeightCm,
                Symptoms = Input.Symptoms,
                Notes = Input.Notes,
                PrimaryConcern = Input.PrimaryConcern,
                Status = "PendingDoctor",
                NextStep = "Doctor"
            };

            _context.TriageEntries.Add(triage);
            _context.QueueEntries.Add(new QueueEntry
            {
                PatientId = patient.Id,
                PatientEncounter = encounter,
                AssignedById = nurse.Id,
                QueueType = "Doctor",
                Status = "Waiting",
                Notes = "Patient triaged and queued for doctor review"
            });

            if (!string.IsNullOrWhiteSpace(Input.CarePlanSummary))
            {
                _context.CarePlans.Add(new CarePlanEntry
                {
                    PatientId = patient.Id,
                    TriageEntry = triage,
                    CreatedById = nurse.Id,
                    Instructions = Input.CarePlanSummary,
                    Goals = Input.CarePlanGoals,
                    ReviewDate = DateTime.Now.AddDays(7)
                });
            }

            await _context.SaveChangesAsync();

            if (isNewPatient)
            {
                await SendPatientWelcomeEmailAsync(patient, tempPassword);
            }

            Message = "Triage information has been saved and the patient is queued for the doctor.";
            return RedirectToPage("/Triage/Index");
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
