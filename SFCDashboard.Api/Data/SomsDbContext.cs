using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Models.Soms;

namespace SFCDashboard.Api.Data
{
    public class SomsDbContext : DbContext
    {
        public SomsDbContext(DbContextOptions<SomsDbContext> options)
            : base(options)
        {
        }

        public DbSet<SomsWorkgroup> Workgroups { get; set; }
    }
}
