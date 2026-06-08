using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Models.Soms;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Data
{
    public class SomsDbContext : DbContext
    {
        public SomsDbContext(DbContextOptions<SomsDbContext> options)
            : base(options)
        {
        }

        public DbSet<SomsWorkgroup> Workgroups { get; set; }
        public DbSet<Notice> Notices { get; set; }
    }
}
