using Microsoft.EntityFrameworkCore;
using SFCDashboard.Models;
using SFCDB.Models;

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
        public DbSet<PERecord> PERecords { get; set; }

        public DbSet<PETaskList> PETaskLists { get; set; }
        public DbSet<PETask> PETasks { get; set; }

        public DbSet<PEIssue> PEIssues { get; set; }
        public DbSet<PEIssueResolution> PEIssueResolutions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure relationships that need special handling
            modelBuilder.Entity<TaskExtensionRequest>()
                .HasOne(t => t.RequestedBy)
                .WithMany(u => u.RequestedExtensions)
                .HasForeignKey(t => t.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PERecord>().ToTable("PERecords");

            // Configure relationship between PETask and PlannedEvent
            modelBuilder.Entity<PETask>()
                .HasOne(t => t.PlannedEvent)
                .WithMany()
                .HasForeignKey(t => t.PENumber)
                .HasPrincipalKey(e => e.PeNumber)
                .OnDelete(DeleteBehavior.Cascade);


        }
        public DbSet<UserRole> UserRole { get; set; } = default!;
        public DbSet<PETaskList> PETaskList { get; set; } = default!;
        public DbSet<UrgentReason> UrgentReasons { get; set; }
    }
}
