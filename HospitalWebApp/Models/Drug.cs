using System.ComponentModel.DataAnnotations;

namespace HospitalWebApp.Models
{
    public class Drug
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string Unit { get; set; } = "tablet";
        public int StockQuantity { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
