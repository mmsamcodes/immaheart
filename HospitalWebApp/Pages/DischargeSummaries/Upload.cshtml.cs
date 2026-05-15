using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Pages.DischargeSummaries
{
    [Authorize(Roles = "Admin,Doctor,Nurse,Accounts,Cashier")]
    public class UploadModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public UploadModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        [BindProperty]
        public IFormFile? SummaryFile { get; set; }

        [BindProperty]
        public string PatientId { get; set; } = string.Empty;

        [BindProperty]
        public string Description { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
        public SelectList PatientList { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync()
        {
            PatientList = new SelectList(await _context.Users
                .Where(u => !string.IsNullOrEmpty(u.Email) && u.Email != "")
                .OrderBy(u => u.FullName)
                .ToListAsync(), "Id", "FullName");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (SummaryFile == null || SummaryFile.Length == 0)
            {
                ModelState.AddModelError("SummaryFile", "A discharge summary file is required.");
            }

            if (string.IsNullOrEmpty(PatientId))
            {
                ModelState.AddModelError("PatientId", "Please select a patient.");
            }

            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var patient = await _context.Users.FindAsync(PatientId);
            var uploader = await _userManager.GetUserAsync(User);
            if (patient == null || uploader == null)
            {
                return NotFound();
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "discharge-summaries");
            Directory.CreateDirectory(uploadsFolder);

            var safeFileName = Path.GetRandomFileName() + Path.GetExtension(SummaryFile.FileName);
            var filePath = Path.Combine(uploadsFolder, safeFileName);
            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await SummaryFile.CopyToAsync(fileStream);

            var document = new DischargeDocument
            {
                PatientId = patient.Id,
                UploadedById = uploader.Id,
                UploadedAt = DateTime.Now,
                FileName = safeFileName,
                OriginalFileName = SummaryFile.FileName,
                Description = Description
            };

            _context.DischargeDocuments.Add(document);
            await _context.SaveChangesAsync();

            Message = "Discharge summary uploaded successfully.";
            return RedirectToPage("/DischargeSummaries/Index");
        }
    }
}
