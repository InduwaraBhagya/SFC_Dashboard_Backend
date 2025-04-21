using Microsoft.EntityFrameworkCore;
using SFCDashboard.Models;

namespace SFCDashboard.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<WorkGroup> WorkGroups { get; set; }
        public DbSet<SystemUser> Users { get; set; }
        public DbSet<PlannedEvent> PlannedEvents { get; set; }
        public DbSet<TaskEscalation> TaskEscalations { get; set; }
        public DbSet<TaskExtensionRequest> TaskExtensionRequests { get; set; }
        public DbSet<TaskHistory> TaskHistories { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure relationships that need special handling
            modelBuilder.Entity<TaskExtensionRequest>()
                .HasOne(t => t.RequestedBy)
                .WithMany(u => u.RequestedExtensions)
                .HasForeignKey(t => t.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskExtensionRequest>()
                .HasOne(t => t.ApprovedBy)
                .WithMany(u => u.ApprovedExtensions)
                .HasForeignKey(t => t.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
        public DbSet<SFCDashboard.Models.UserRole> UserRole { get; set; } = default!;
    }
}
