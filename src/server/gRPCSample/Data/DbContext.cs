using Microsoft.EntityFrameworkCore;

namespace gRPCSample.Data
{
    public class SampleContext : DbContext
    {
        public SampleContext(DbContextOptions<SampleContext> options) : base(options) { }

        public DbSet<CountryModel> Countries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CountryModel>().ToTable("tblCountries", "dbo");
            modelBuilder.Entity<CountryModel>().HasKey(x => x.CountryID);
        }
    }
}
