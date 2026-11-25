using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace HospitalWebApp.Controllers
{
    [Route("labs")]
    [Authorize(Roles = "Admin,Doctor,LabTech,Nurse,Patient")]
    public class LabResultsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LabResultsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> Download(int id)
        {
            var order = await _context.LabOrders
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == id);
            if (order == null || string.IsNullOrEmpty(order.ResultFileName))
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if ((User.IsInRole("Patient") && order.PatientId != userId) ||
                (User.IsInRole("Doctor") && order.DoctorId != userId))
            {
                return Forbid();
            }
            if (!User.IsInRole("Patient") &&
                !User.IsInRole("Doctor") &&
                !User.IsInRole("LabTech") &&
                !User.IsInRole("Nurse") &&
                !User.IsInRole("Admin"))
            {
                return Forbid();
            }
            if (!string.Equals(Path.GetFileName(order.ResultFileName), order.ResultFileName, StringComparison.Ordinal))
            {
                return NotFound();
            }

            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "lab-results");
            var filePath = Path.Combine(uploads, order.ResultFileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            Response.Headers["Cache-Control"] = "no-store";
            var contentType = "application/octet-stream";
            return PhysicalFile(filePath, contentType, order.ResultFileName);
        }
    }
}
