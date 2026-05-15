using Microsoft.AspNetCore.Identity;

namespace HospitalWebApp.Models;

public class ApplicationUser : IdentityUser
{
    // Custom profile fields for hospital users.
    public string FullName { get; set; } = string.Empty;
    public int Age { get; set; }
    public double Weight { get; set; }
    public double Height { get; set; }
    public string BloodGroup { get; set; } = "Unknown";
    public string IDNumber { get; set; } = string.Empty;
}