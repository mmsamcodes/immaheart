using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HospitalWebApp.Areas.Identity.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ImportPatientsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ImportPatientsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        [Required]
        public IFormFile? PatientCsvFile { get; set; }

        public IList<string> ImportMessages { get; set; } = new List<string>();
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public int ExistingCount { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (PatientCsvFile == null || PatientCsvFile.Length == 0)
            {
                ModelState.AddModelError("PatientCsvFile", "Please upload a CSV file with patient records.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            using var reader = new StreamReader(PatientCsvFile.OpenReadStream(), Encoding.UTF8);
            var lines = new List<string>();
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lines.Add(line.Trim());
                }
            }

            foreach (var line in lines)
            {
                if (line.Contains("Email", System.StringComparison.OrdinalIgnoreCase) && line.Contains("FullName", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var pieces = line.Split(',');
                if (pieces.Length < 3)
                {
                    SkippedCount++;
                    ImportMessages.Add($"Skipped invalid row: {line}");
                    continue;
                }

                var fullName = pieces[0].Trim();
                var email = pieces[1].Trim();
                var phone = pieces.Length > 2 ? pieces[2].Trim() : string.Empty;
                var idNumber = pieces.Length > 3 ? pieces[3].Trim() : string.Empty;
                var age = pieces.Length > 4 && int.TryParse(pieces[4].Trim(), out var parsedAge) ? parsedAge : 0;
                var bloodGroup = pieces.Length > 5 ? pieces[5].Trim() : string.Empty;

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
                {
                    SkippedCount++;
                    ImportMessages.Add($"Skipped incomplete row: {line}");
                    continue;
                }

                if (await _userManager.FindByEmailAsync(email) != null)
                {
                    ExistingCount++;
                    ImportMessages.Add($"Existing patient skipped: {email}");
                    continue;
                }

                var password = GenerateSecurePassword();
                var newPatient = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    PhoneNumber = phone,
                    IDNumber = idNumber,
                    Age = age,
                    BloodGroup = bloodGroup,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(newPatient, password);
                if (createResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(newPatient, "Patient");
                    ImportedCount++;
                    ImportMessages.Add($"Imported patient: {fullName} ({email})");
                    await SendPatientWelcomeEmailAsync(newPatient, password);
                }
                else
                {
                    SkippedCount++;
                    ImportMessages.Add($"Failed to import {email}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
            }

            return Page();
        }

        private string GenerateSecurePassword()
        {
            var random = new Random();
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specials = "@#$%&*!";
            return string.Concat(
                upper[random.Next(upper.Length)],
                lower[random.Next(lower.Length)],
                digits[random.Next(digits.Length)],
                specials[random.Next(specials.Length)],
                upper[random.Next(upper.Length)],
                lower[random.Next(lower.Length)],
                digits[random.Next(digits.Length)],
                specials[random.Next(specials.Length)]);
        }

        private async Task SendPatientWelcomeEmailAsync(ApplicationUser patient, string password)
        {
            try
            {
                var loginUrl = Url.Page("/Account/Login", null, null, Request.Scheme);
                var message = $"<p>Dear {patient.FullName},</p>" +
                              $"<p>Your hospital portal account has been created.</p>" +
                              $"<ul><li>Email: {patient.Email}</li><li>Temporary password: {password}</li></ul>" +
                              $"<p>Please log in and change your password.</p>" +
                              $"<p><a href=\"{loginUrl}\">Login to patient portal</a></p>" +
                              $"<p>Welcome to Immaculate Heart Hospital.</p>";
                await _emailSender.SendEmailAsync(patient.Email, "Your patient portal account has been created", message);
            }
            catch
            {
                // ignore email failure so import still completes.
            }
        }
    }
}
