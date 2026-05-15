using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HospitalWebApp.Data;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HospitalWebApp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class VideoController : Controller
    {
        private readonly ApplicationDbContext _context;
        public VideoController(ApplicationDbContext context) => _context = context;

        [HttpPost("create-session/{appointmentId}")]
        public async Task<IActionResult> CreateSession(int appointmentId)
        {
            var appt = await _context.Appointments.FindAsync(appointmentId);
            if (appt == null) return NotFound();

            if (!IsUserAllowedToAccessAppointment(appt))
            {
                return Forbid();
            }

            var sessionId = Guid.NewGuid().ToString("N");
            var joinUrl = $"https://video.example.local/join/{sessionId}";

            appt.VideoSessionId = sessionId;
            appt.VideoJoinUrl = joinUrl;
            await _context.SaveChangesAsync();

            return Ok(new { sessionId, joinUrl });
        }

        [HttpPost("confirm-session/{appointmentId}")]
        public async Task<IActionResult> ConfirmSession(int appointmentId)
        {
            var appt = await _context.Appointments.FindAsync(appointmentId);
            if (appt == null) return NotFound();

            if (!IsUserAllowedToAccessAppointment(appt))
            {
                return Forbid();
            }

            appt.VirtualConfirmedByDoctor = true;
            await _context.SaveChangesAsync();

            return Ok(new { confirmed = true });
        }

        private bool IsUserAllowedToAccessAppointment(Models.Appointment appt)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return false;
            return appt.PatientId == userId || appt.DoctorId == userId || User.IsInRole("Admin") || User.IsInRole("Accounts");
        }
    }
}
