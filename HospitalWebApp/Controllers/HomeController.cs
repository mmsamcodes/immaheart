using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;

namespace HospitalWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public HomeController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public IActionResult Index() => View();

        public async Task<IActionResult> About()
        {
            var recentBlogPosts = await _context.BlogPosts
                .Include(b => b.Author)
                .OrderByDescending(b => b.DatePosted)
                .Take(3)
                .ToListAsync();

            var model = new AboutViewModel
            {
                Title = "About Us",
                Subtitle = "Rooted in faith, dedicated to healing, serving with love.",
                IntroText = "Immaculate Heart Hospital Kereita is a faith-based Level 4 mission hospital built to deliver dignified healthcare with modern clinical excellence and a welcoming patient experience.",
                RecentBlogPosts = recentBlogPosts
            };

            return View(model);
        }

        public IActionResult Services() => View();

        public IActionResult Contact() => View();
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(string fullName, string phone, string email, string subject, string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
                {
                    return Json(new { success = false, message = "Please fill in all required fields." });
                }

                var hospitalEmail = "info@immaculatehearthospitalkereita.org";
                var htmlMessage = $@"
                    <h3>New Inquiry from Hospital Contact Form</h3>
                    <p><strong>Name:</strong> {fullName}</p>
                    <p><strong>Phone:</strong> {phone}</p>
                    <p><strong>Email:</strong> {email}</p>
                    <p><strong>Subject:</strong> {subject}</p>
                    <hr />
                    <p><strong>Message:</strong></p>
                    <p>{message.Replace(Environment.NewLine, "<br>")}</p>
                ";

                await _emailSender.SendEmailAsync(hospitalEmail, $"Hospital Inquiry: {subject}", htmlMessage);

                return Json(new { success = true, message = "Thank you! Your inquiry has been sent successfully. We will contact you shortly." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email error: {ex.Message}");
                return Json(new { success = false, message = "Error sending inquiry. Please try again or call us directly." });
            }
        }
        
        public IActionResult Privacy() => View();

    }
}



