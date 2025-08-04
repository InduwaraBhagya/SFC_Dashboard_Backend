using Microsoft.EntityFrameworkCore;
using SFCDashboard.Background.Models;

namespace SFCDashboard.Background.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<PERecord> PERecords { get; set; }
        public DbSet<PlannedEvent> PlannedEvents { get; set; }
        public DbSet<PETask> PETasks { get; set; }
        public DbSet<PETaskList> PETaskLists { get; set; }
        public DbSet<PEIssue> PEIssues { get; set; }
        public DbSet<SystemUser> Users { get; set; }
        public DbSet<UserWorkGroup> UserWorkGroups { get; set; }
        public DbSet<WorkGroup> WorkGroups { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Escalation> Escalations { get; set; }
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table names to match existing database schema
            modelBuilder.Entity<PERecord>().ToTable("PERecords");
            modelBuilder.Entity<PETaskList>().ToTable("PETaskList"); 
            modelBuilder.Entity<PETask>().ToTable("PETasks");
            modelBuilder.Entity<PEIssue>().ToTable("PEIssues");
            modelBuilder.Entity<SystemUser>().ToTable("Users");
            modelBuilder.Entity<UserWorkGroup>().ToTable("UserWorkGroups");
            modelBuilder.Entity<WorkGroup>().ToTable("WorkGroups");
            modelBuilder.Entity<UserRole>().ToTable("UserRoles");
            modelBuilder.Entity<RolePermission>().ToTable("RolePermissions");
            modelBuilder.Entity<Permission>().ToTable("Permissions");
            modelBuilder.Entity<PlannedEvent>().ToTable("PlannedEvents");
            modelBuilder.Entity<Escalation>().ToTable("Escalations");
            modelBuilder.Entity<SystemConfiguration>().ToTable("SystemConfigurations");

            // Configure relationship between PETask and PlannedEvent
            modelBuilder.Entity<PETask>()
                .HasOne(t => t.PlannedEvent)
                .WithMany()
                .HasForeignKey(t => t.PENumber)
                .HasPrincipalKey(e => e.PeNumber)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Escalation and PETask relationship
            modelBuilder.Entity<Escalation>()
                .HasOne(e => e.PETask)
                .WithMany()
                .HasForeignKey(e => e.TaskId);

            // Configure Escalation and SystemUser relationship
            modelBuilder.Entity<Escalation>()
                .HasOne(e => e.IgnoredBy)
                .WithMany()
                .HasForeignKey(e => e.IgnoredById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
