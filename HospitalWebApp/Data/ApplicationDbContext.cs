using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Models; // Ensure the model namespace matches the folder structure

namespace HospitalWebApp.Data // Ensure the data namespace matches the project structure
{
    // ApplicationDbContext inherits from IdentityDbContext with the custom user type.
   public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for the hospital entities persisted in the database.
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<HealthRecord> HealthRecords { get; set; }
        public DbSet<ConsultationMessage> ConsultationMessages { get; set; }
        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<LabOrder> LabOrders { get; set; }
        public DbSet<BillingInvoice> BillingInvoices { get; set; }
        public DbSet<BillingItem> BillingItems { get; set; }
        public DbSet<Admission> Admissions { get; set; }
        public DbSet<QueueEntry> QueueEntries { get; set; }
        public DbSet<TriageEntry> TriageEntries { get; set; }
        public DbSet<DischargeDocument> DischargeDocuments { get; set; }
        public DbSet<CarePlanEntry> CarePlans { get; set; }
        public DbSet<Drug> Drugs { get; set; }
        public DbSet<ServiceCatalog> ServiceCatalogs { get; set; }
    }
}