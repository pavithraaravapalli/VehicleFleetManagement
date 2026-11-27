using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.Data
{
    /// <summary>
    /// Application database context managing entity persistence, Identity user storage,
    /// and SQL Server relational configurations via Entity Framework Core.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Core Domain Entity DbSets
        public virtual DbSet<Vehicle> Vehicles { get; set; } = null!;
        public virtual DbSet<Driver> Drivers { get; set; } = null!;
        public virtual DbSet<Trip> Trips { get; set; } = null!;
        public virtual DbSet<Maintenance> Maintenances { get; set; } = null!;
        public virtual DbSet<FuelLog> FuelLogs { get; set; } = null!;
        public virtual DbSet<Notification> Notifications { get; set; } = null!;
        public virtual DbSet<DriverReview> DriverReviews { get; set; } = null!;
        public virtual DbSet<VehicleIssueReport> VehicleIssueReports { get; set; } = null!;
        public virtual DbSet<DriverAvailabilityLog> DriverAvailabilityLogs { get; set; } = null!;
        public virtual DbSet<TripRequest> TripRequests { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ==========================================
            // 1. VEHICLE ENTITY CONFIGURATION
            // ==========================================
            builder.Entity<Vehicle>(entity =>
            {
                entity.ToTable("Vehicles");

                entity.HasKey(v => v.VehicleId);

                // Unique index on vehicle registration number
                entity.HasIndex(v => v.RegistrationNumber)
                      .IsUnique()
                      .HasDatabaseName("IX_Vehicles_RegistrationNumber");

                entity.Property(v => v.RegistrationNumber)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(v => v.VehicleName)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(v => v.Model)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(v => v.VehicleType)
                      .IsRequired()
                      .HasMaxLength(30);

                entity.Property(v => v.FuelType)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(v => v.CurrentOdometer)
                      .HasPrecision(10, 2);

                entity.Property(v => v.Status)
                      .IsRequired()
                      .HasMaxLength(25);
            });

            // ==========================================
            // 2. DRIVER ENTITY CONFIGURATION
            // ==========================================
            builder.Entity<Driver>(entity =>
            {
                entity.ToTable("Drivers");

                entity.HasKey(d => d.DriverId);

                // Unique index on driver license number
                entity.HasIndex(d => d.LicenseNumber)
                      .IsUnique()
                      .HasDatabaseName("IX_Drivers_LicenseNumber");

                entity.Property(d => d.FullName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(d => d.Email)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(d => d.PhoneNumber)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(d => d.LicenseNumber)
                      .IsRequired()
                      .HasMaxLength(30);

                entity.Property(d => d.Address)
                      .IsRequired()
                      .HasMaxLength(250);

                entity.Property(d => d.AvailabilityStatus)
                      .IsRequired()
                      .HasMaxLength(25);

                entity.Property(d => d.Rating)
                      .HasPrecision(3, 2);

                // 1-to-0..1 relationship with ApplicationUser (for driver login credentials)
                entity.HasOne(d => d.User)
                      .WithOne(u => u.Driver)
                      .HasForeignKey<Driver>(d => d.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ==========================================
            // 3. TRIP ENTITY CONFIGURATION
            // ==========================================
            builder.Entity<Trip>(entity =>
            {
                entity.ToTable("Trips");

                entity.HasKey(t => t.TripId);

                entity.Property(t => t.Source)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(t => t.Destination)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(t => t.Purpose)
                      .IsRequired()
                      .HasMaxLength(250);

                entity.Property(t => t.TripStatus)
                      .IsRequired()
                      .HasMaxLength(25);

                entity.Property(t => t.StartOdometer)
                      .HasPrecision(10, 2);

                entity.Property(t => t.EndOdometer)
                      .HasPrecision(10, 2);

                entity.Property(t => t.Distance)
                      .HasPrecision(10, 2);

                // Relationship: Vehicle -> Trips (Restrict deletion if historical trips exist)
                entity.HasOne(t => t.Vehicle)
                      .WithMany(v => v.Trips)
                      .HasForeignKey(t => t.VehicleId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relationship: Driver -> Trips (Restrict deletion if historical trips exist)
                entity.HasOne(t => t.Driver)
                      .WithMany(d => d.Trips)
                      .HasForeignKey(t => t.DriverId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 4. MAINTENANCE ENTITY CONFIGURATION
            // ==========================================
            builder.Entity<Maintenance>(entity =>
            {
                entity.ToTable("Maintenances");

                entity.HasKey(m => m.MaintenanceId);

                entity.Property(m => m.ServiceType)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(m => m.Description)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(m => m.ServiceCenter)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(m => m.Status)
                      .IsRequired()
                      .HasMaxLength(25);

                entity.Property(m => m.Cost)
                      .HasPrecision(18, 2);

                // Relationship: Vehicle -> Maintenances (Cascade on vehicle removal)
                entity.HasOne(m => m.Vehicle)
                      .WithMany(v => v.Maintenances)
                      .HasForeignKey(m => m.VehicleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 5. FUEL LOG ENTITY CONFIGURATION
            // ==========================================
            builder.Entity<FuelLog>(entity =>
            {
                entity.ToTable("FuelLogs");

                entity.HasKey(f => f.FuelLogId);

                entity.Property(f => f.FuelStation)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(f => f.FuelQuantity)
                      .HasPrecision(8, 2);

                entity.Property(f => f.PricePerLitre)
                      .HasPrecision(8, 2);

                entity.Property(f => f.TotalCost)
                      .HasPrecision(18, 2);

                entity.Property(f => f.OdometerReading)
                      .HasPrecision(10, 2);

                // Relationship: Vehicle -> FuelLogs (Restrict deletion)
                entity.HasOne(f => f.Vehicle)
                      .WithMany(v => v.FuelLogs)
                      .HasForeignKey(f => f.VehicleId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relationship: Trip -> FuelLogs (SetNull on trip deletion)
                entity.HasOne(f => f.Trip)
                      .WithMany(t => t.FuelLogs)
                      .HasForeignKey(f => f.TripId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ==========================================
            // 6. NOTIFICATION ENTITY CONFIGURATION
            // ==========================================
            builder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");

                entity.HasKey(n => n.NotificationId);

                entity.Property(n => n.Title)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(n => n.Message)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(n => n.Type)
                      .IsRequired()
                      .HasMaxLength(50);

                // Relationship: ApplicationUser -> Notifications (Cascade deletion)
                entity.HasOne(n => n.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 7. DRIVER REVIEW CONFIGURATION
            // ==========================================
            builder.Entity<DriverReview>(entity =>
            {
                entity.ToTable("DriverReviews");

                entity.HasKey(r => r.ReviewId);

                entity.Property(r => r.Comment)
                      .IsRequired()
                      .HasMaxLength(500);

                // Prevent duplicate review for the same trip and reviewer
                entity.HasIndex(r => new { r.TripId, r.ReviewerUserId })
                      .IsUnique()
                      .HasDatabaseName("IX_DriverReviews_Trip_Reviewer");

                entity.HasOne(r => r.Driver)
                      .WithMany(d => d.Reviews)
                      .HasForeignKey(r => r.DriverId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Trip)
                      .WithMany(t => t.Reviews)
                      .HasForeignKey(r => r.TripId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.ReviewerUser)
                      .WithMany(u => u.ReviewsWritten)
                      .HasForeignKey(r => r.ReviewerUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 8. VEHICLE ISSUE REPORT CONFIGURATION
            // ==========================================
            builder.Entity<VehicleIssueReport>(entity =>
            {
                entity.ToTable("VehicleIssueReports");

                entity.HasKey(i => i.IssueReportId);

                entity.Property(i => i.Title)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(i => i.Description)
                      .IsRequired()
                      .HasMaxLength(1000);

                entity.Property(i => i.Category)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(i => i.Severity)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(i => i.Status)
                      .IsRequired()
                      .HasMaxLength(25);

                entity.HasOne(i => i.Vehicle)
                      .WithMany()
                      .HasForeignKey(i => i.VehicleId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Driver)
                      .WithMany(d => d.ReportedIssues)
                      .HasForeignKey(i => i.DriverId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Trip)
                      .WithMany(t => t.IssueReports)
                      .HasForeignKey(i => i.TripId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ==========================================
            // 9. DRIVER AVAILABILITY LOG CONFIGURATION
            // ==========================================
            builder.Entity<DriverAvailabilityLog>(entity =>
            {
                entity.ToTable("DriverAvailabilityLogs");

                entity.HasKey(l => l.LogId);

                entity.Property(l => l.PreviousStatus)
                      .IsRequired()
                      .HasMaxLength(25);

                entity.Property(l => l.NewStatus)
                      .IsRequired()
                      .HasMaxLength(25);

                entity.HasOne(l => l.Driver)
                      .WithMany(d => d.AvailabilityLogs)
                      .HasForeignKey(l => l.DriverId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 10. TRIP REQUEST CONFIGURATION
            // ==========================================
            builder.Entity<TripRequest>(entity =>
            {
                entity.ToTable("TripRequests");

                entity.HasKey(r => r.TripRequestId);

                entity.Property(r => r.Source)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(r => r.Destination)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(r => r.Purpose)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(r => r.Status)
                      .IsRequired()
                      .HasMaxLength(25);

                entity.HasOne(r => r.User)
                      .WithMany(u => u.TripRequests)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.ApprovedTrip)
                      .WithMany()
                      .HasForeignKey(r => r.ApprovedTripId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
