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
        public DbSet<Escalation> Escalations { get; set; }
        public DbSet<SurveyTaskActivity> SurveyTaskActivities { get; set; } // <-- Added DbSet for SurveyTaskActivity

        public DbSet<UDCategory> UDCategories { get; set; }
        public DbSet<UDSubCategory> UDSubCategories { get; set; }
        public DbSet<UDName> UDNames { get; set; }
        public DbSet<RTOMWeight> RTOMWeights { get; set; }
        public DbSet<BOQ> BOQs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
             base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserWorkGroup>()
            .HasKey(uwg => new { uwg.SystemUserId, uwg.WorkGroupId });

        modelBuilder.Entity<UserWorkGroup>()
            .HasOne(uwg => uwg.SystemUser)
            .WithMany(u => u.UserWorkGroups)
            .HasForeignKey(uwg => uwg.SystemUserId);

        modelBuilder.Entity<UserWorkGroup>()
            .HasOne(uwg => uwg.WorkGroup)
            .WithMany(wg => wg.UserWorkGroups)
            .HasForeignKey(uwg => uwg.WorkGroupId);
            
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
            new Permission { Id = 5, Name = "Admin", Description = "Admin permissions" },
            new Permission { Id = 6, Name = "ViewAll", Description = "Can view all records" },
            new Permission { Id = 7, Name = "CanReportIssues", Description = "Can report issues on Planned Events" }
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

            // Configure Escalation and PETask relationship
            modelBuilder.Entity<Escalation>()
                .HasOne(e => e.PETask)
                .WithMany()
                .HasForeignKey(e => e.TaskId);
            
            // Make sure System.Threading.Tasks.Task is not registered as an entity
            modelBuilder.Ignore<System.Threading.Tasks.Task>();
        }
        public DbSet<PETaskList> PETaskList { get; set; } = default!;
        public DbSet<UrgentReason> UrgentReasons { get; set; }
        public DbSet<SubTaskList> SubTaskLists { get; set; }
        public DbSet<UserWorkGroup> UserWorkGroups { get; set; }
    }

}
