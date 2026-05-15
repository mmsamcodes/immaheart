using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;

namespace HospitalWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
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
        
        public IActionResult Privacy() => View();

    }
}


