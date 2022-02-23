using RevitToIfcScheduler.Models;
using Microsoft.EntityFrameworkCore;

namespace RevitToIfcScheduler.Context
{
    public class RevitIfcContext: DbContext
    {
        public RevitIfcContext(DbContextOptions<RevitIfcContext> options)
            : base(options)
        {
            // Database.SetCommandTimeout(300);
        }
        
        public DbSet<User> Users { get; set; }
        public DbSet<IfcSettingsSet> IfcSettingsSets { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<ConversionJob> ConversionJobs { get; set; }
        public DbSet<Account> Accounts { get; set; }
    }
}