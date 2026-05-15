using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models
{
    public class BlogPost
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Summary is required")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Summary must be between 10 and 500 characters")]
        public string Summary { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required")]
        [StringLength(5000, MinimumLength = 20, ErrorMessage = "Content must be between 20 and 5000 characters")]
        public string Content { get; set; } = string.Empty;

        public DateTime DatePosted { get; set; } = DateTime.Now;

        [Required]
        public string Category { get; set; } = "Hospital Updates";

        // Foreign key to ApplicationUser
        public string? AuthorId { get; set; }

        [ForeignKey(nameof(AuthorId))]
        public virtual ApplicationUser? Author { get; set; }

        // Audit trail
        public DateTime? DateUpdated { get; set; }
        public int ViewCount { get; set; } = 0;
    }
}