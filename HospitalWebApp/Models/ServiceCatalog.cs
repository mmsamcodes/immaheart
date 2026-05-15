using System.ComponentModel.DataAnnotations;

namespace HospitalWebApp.Models
{
    public class ServiceCatalog
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;
    }
}
