using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Models;
using SFCDB.Models;
using static Project;

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
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<TaskEstimationHistory> TaskEstimationHistory { get; set; }

        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectPEMapping> ProjectPEMappings { get; set; }

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

            modelBuilder.Entity<Permission>().HasData(
            new Permission { Id = 1, Name = "CanManageEstimatedTime", Description = "Can manage estimated time for tasks" },
            new Permission { Id = 2, Name = "CanSendPEUrgentRequests", Description = "Can send \"Planned Event\" urgent requests" },
            new Permission { Id = 3, Name = "CanAcceptUrgentRequests", Description = "Can view and accept Planned Event urgent requests" },
            new Permission { Id = 4, Name = "CanMakeTasksUrgent", Description = "Can mark a \"Task\" of a specific PE as Urgent" },
            new Permission { Id = 5, Name = "Admin", Description = "Admin permissions" }
            // Add other permissions here
            );

            modelBuilder.Entity<ProjectPEMapping>()
                .HasOne(pp => pp.Project)
                .WithMany(p => p.ProjectPEs)
                .HasForeignKey(pp => pp.ProjectId);

            modelBuilder.Entity<ProjectPEMapping>()
                .HasOne(pp => pp.PlannedEvent)
                .WithMany()
                .HasForeignKey(pp => pp.PlannedEventId);
        }
        public DbSet<PETaskList> PETaskList { get; set; } = default!;
        public DbSet<UrgentReason> UrgentReasons { get; set; }
        public DbSet<SubTaskList> SubTaskLists { get; set; }
    }

    public class PE
    {
    }
}
