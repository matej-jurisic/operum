using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Operum.Model.Models;

namespace Operum.Model
{
    public class OperumContext(DbContextOptions<OperumContext> options) : IdentityDbContext<User, IdentityRole, string>(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            foreach (var entity in builder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(
                            new ValueConverter<DateTime, DateTime>(
                                v => v.ToUniversalTime(),
                                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                            )
                        );
                    }
                }
            }
            base.OnModelCreating(builder);

            builder.Entity<View>()
                .HasOne(v => v.Tracker)
                .WithMany(t => t.Views)
                .HasForeignKey(v => v.TrackerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TrackerConstant>()
                .HasOne(c => c.Tracker)
                .WithMany(t => t.TrackerConstants)
                .HasForeignKey(c => c.TrackerId)
                .OnDelete(DeleteBehavior.Cascade);

        }

        public override DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Tracker> Trackers { get; set; }
        public DbSet<Field> Fields { get; set; }
        public DbSet<Entry> Entries { get; set; }
        public DbSet<FieldValue> FieldValues { get; set; }
        public DbSet<View> Views { get; set; }
        public DbSet<ViewSort> ViewSorts { get; set; }
        public DbSet<ViewFilter> ViewFilters { get; set; }
        public DbSet<ViewGroup> ViewGroups { get; set; }
        public DbSet<ViewColumn> ViewColumns { get; set; }
        public DbSet<TrackerType> TrackerTypes { get; set; }
        public DbSet<UserTracker> UserTrackers { get; set; }
        public DbSet<Analytic> Analytics { get; set; }
        public DbSet<AnalyticField> AnalyticFields { get; set; }
        public DbSet<TrackerConstant> TrackerConstants { get; set; }
    }
}
