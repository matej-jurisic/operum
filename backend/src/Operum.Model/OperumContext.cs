using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Operum.Model.Models;

namespace Operum.Model
{
    public class OperumContext(DbContextOptions<OperumContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Tracker> Trackers { get; set; }
        public DbSet<Field> Fields { get; set; }
        public DbSet<Entry> Entries { get; set; }
        public DbSet<FieldValue> FieldValues { get; set; }
        public DbSet<Analytic> Analytics { get; set; }
        public DbSet<AnalyticRequiredDataType> AnalyticRequiredDataTypes { get; set; }
        public DbSet<View> Views { get; set; }
        public DbSet<ViewSort> ViewSorts { get; set; }
        public DbSet<ViewFilter> ViewFilters { get; set; }
        public DbSet<ViewGroup> ViewGroups { get; set; }
        public DbSet<ViewColumn> ViewColumns { get; set; }
    }
}
