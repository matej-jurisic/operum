using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Operum.Model.Models;

namespace Operum.Model
{
    public class OperumContext(DbContextOptions<OperumContext> options) : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
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

            // Tracker ↔ Views (one-to-many)
            builder.Entity<View>()
                .HasOne(v => v.Tracker)
                .WithMany(t => t.Views)
                .HasForeignKey(v => v.TrackerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Tracker ↔ DefaultView (one-to-one / optional)
            builder.Entity<Tracker>()
                .HasOne(t => t.DefaultView)
                .WithMany()
                .HasForeignKey(t => t.DefaultViewId)
                .OnDelete(DeleteBehavior.SetNull);
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
        public DbSet<TrackerType> TrackerTypes { get; set; }
        public DbSet<AnalyticType> AnalyticTypes { get; set; }
    }
}
