using Microsoft.EntityFrameworkCore;
using SFCDashboard.EscalationService.Models;

namespace SFCDashboard.EscalationService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<PlannedEvent> PlannedEvents { get; set; }
        public DbSet<PETask> PETasks { get; set; }
        public DbSet<Escalation> Escalations { get; set; }
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
        public DbSet<SystemUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
