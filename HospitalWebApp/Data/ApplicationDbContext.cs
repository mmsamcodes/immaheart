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
        public DbSet<PharmacyOrder> PharmacyOrders { get; set; }
        public DbSet<PharmacyOrderItem> PharmacyOrderItems { get; set; }
        public DbSet<ServiceCatalog> ServiceCatalogs { get; set; }
        public DbSet<PatientService> PatientServices { get; set; }
        public DbSet<CareNote> CareNotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PharmacyOrder>()
                .HasIndex(order => order.OrderNumber)
                .IsUnique();
            modelBuilder.Entity<PharmacyOrder>()
                .HasOne(order => order.Patient)
                .WithMany()
                .HasForeignKey(order => order.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PharmacyOrder>()
                .HasMany(order => order.Items)
                .WithOne(item => item.PharmacyOrder)
                .HasForeignKey(item => item.PharmacyOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PharmacyOrderItem>()
                .Property(item => item.RequiresPrescription)
                .HasDefaultValue(false);
        }
    }
}