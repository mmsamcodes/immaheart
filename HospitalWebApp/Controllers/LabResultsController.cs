using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;

namespace HospitalWebApp.Controllers
{
    [Route("labs")]
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
            var order = await _context.LabOrders.FindAsync(id);
            if (order == null || string.IsNullOrEmpty(order.ResultFileName))
            {
                return NotFound();
            }

            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "lab-results");
            var filePath = Path.Combine(uploads, order.ResultFileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var contentType = "application/octet-stream";
            return PhysicalFile(filePath, contentType, order.ResultFileName);
        }
    }
}
