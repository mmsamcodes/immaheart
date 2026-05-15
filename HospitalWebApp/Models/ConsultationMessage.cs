using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models
{
    public class ConsultationMessage
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        
        [ForeignKey("SenderId")]
        public virtual ApplicationUser? Sender { get; set; }
        
        public string ReceiverId { get; set; } = string.Empty;
        
        [ForeignKey("ReceiverId")]
        public virtual ApplicationUser? Receiver { get; set; }
        
        public string MessageText { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
    }
}