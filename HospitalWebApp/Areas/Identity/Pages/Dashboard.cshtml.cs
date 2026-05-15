using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using HospitalWebApp.Models;

namespace HospitalWebApp.Areas.Identity.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public DashboardModel(UserManager<ApplicationUser> userManager) => _userManager = userManager;

        public string FullName { get; set; } = "";
        public double BMI { get; set; }
        public string BMICategory { get; set; } = "Not Set";
        
        // Cycle tracker fields for display.
        public int DayOfCycle { get; set; }
        public string CyclePhase { get; set; } = "Log data to see phase";

        // Lists used to render tips and FAQ content.
        public List<NutritionTip> NutritionTips { get; set; } = new();
        public List<HealthyHabit> HealthyHabits { get; set; } = new();
        public List<FAQItem> FAQItems { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Admin and doctor users are redirected to their own dashboards.
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToPage("/AdminDashboard");
            }
            if (await _userManager.IsInRoleAsync(user, "Doctor"))
            {
                return RedirectToPage("/DoctorDashboard");
            }
            if (await _userManager.IsInRoleAsync(user, "Nurse"))
            {
                return RedirectToPage("/NurseDashboard");
            }
            if (await _userManager.IsInRoleAsync(user, "LabTech"))
            {
                return RedirectToPage("/LabTechDashboard");
            }
            if (await _userManager.IsInRoleAsync(user, "Pharmacy"))
            {
                return RedirectToPage("/PharmacyDashboard");
            }
            if (await _userManager.IsInRoleAsync(user, "Reception"))
            {
                return RedirectToPage("/AccountsDashboard");
            }

            FullName = user.FullName;
            
            // Calculate BMI from the user's height and weight.
            if (user.Height > 0)
            {
                BMI = user.Weight / (user.Height * user.Height);
                BMICategory = BMI switch {
                    < 18.5 => "Underweight",
                    < 25 => "Healthy",
                    < 30 => "Overweight",
                    _ => "Obese"
                };
            }

            // Set a sample cycle phase for the dashboard display.
            DayOfCycle = 14; 
            CyclePhase = "Ovulation Window";

            // Populate the nutrition tips list.
            LoadNutritionTips();

            // Populate the healthy habits list.
            LoadHealthyHabits();

            // Populate the FAQ list.
            LoadFAQItems();

            return Page();
        }

        private void LoadNutritionTips()
        {
            NutritionTips = new List<NutritionTip>
            {
                new NutritionTip { Title = "💧 Stay Hydrated", Description = "Drink at least 8-10 glasses of water daily to maintain optimal health." },
                new NutritionTip { Title = "🥗 Eat Colorful Foods", Description = "Include a variety of colorful fruits and vegetables in every meal." },
                new NutritionTip { Title = "🥛 Calcium & Vitamin D", Description = "Essential for bone health. Get from dairy, leafy greens, or supplements." },
                new NutritionTip { Title = "🍎 Portion Control", Description = "Use smaller plates and practice mindful eating for better portion sizes." }
            };
        }

        private void LoadHealthyHabits()
        {
            HealthyHabits = new List<HealthyHabit>
            {
                new HealthyHabit { Title = "Exercise Regularly", Description = "Aim for 150 minutes of moderate aerobic activity per week." },
                new HealthyHabit { Title = "Sleep Well", Description = "Get 7-9 hours of quality sleep each night for mental & physical health." },
                new HealthyHabit { Title = "Manage Stress", Description = "Practice meditation, yoga, or deep breathing exercises daily." },
                new HealthyHabit { Title = "Regular Checkups", Description = "Schedule annual health screenings and doctor visits." }
            };
        }

        private void LoadFAQItems()
        {
            FAQItems = new List<FAQItem>
            {
                new FAQItem 
                { 
                    Question = "How often should I visit the doctor?",
                    Answer = "Generally, adults should have an annual checkup. However, if you have chronic conditions or risk factors, more frequent visits may be necessary. Consult your doctor for personalized recommendations."
                },
                new FAQItem 
                { 
                    Question = "What should I do if I have severe abdominal pain?",
                    Answer = "Seek immediate medical attention by visiting the emergency room or calling emergency services. Severe abdominal pain can indicate serious conditions that require urgent evaluation."
                },
                new FAQItem 
                { 
                    Question = "How can I maintain a healthy weight?",
                    Answer = "Maintain a healthy weight through balanced nutrition, regular exercise (150+ minutes weekly), adequate sleep, stress management, and regular health monitoring. Consult a nutritionist for personalized advice."
                },
                new FAQItem 
                { 
                    Question = "What are signs of dehydration?",
                    Answer = "Signs include dark urine, dry mouth, dizziness, fatigue, and reduced urination. Drink water throughout the day and increase intake in hot weather or during exercise."
                },
                new FAQItem 
                { 
                    Question = "Is it normal to feel anxious about health?",
                    Answer = "Yes, health anxiety is common. However, if it interferes with daily life, seek professional help. Our counselors and doctors can help you manage these concerns."
                },
                new FAQItem 
                { 
                    Question = "What should I eat before and after exercise?",
                    Answer = "Before: Light carbs 1-2 hours prior. After: Protein + carbs within 30-60 minutes for muscle recovery. Stay hydrated throughout."
                }
            };
        }
    }

    // Helper Classes
    public class NutritionTip
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class HealthyHabit
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class FAQItem
    {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
    }
}