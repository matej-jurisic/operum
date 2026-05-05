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

            builder.Entity<TrackerConstantValue>()
                .HasOne(v => v.TrackerConstant)
                .WithMany(c => c.Values)
                .HasForeignKey(v => v.TrackerConstantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TrackerConstantValueFilter>()
                .HasOne(f => f.TrackerConstantValue)
                .WithMany(v => v.Filters)
                .HasForeignKey(f => f.TrackerConstantValueId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Dashboard>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DashboardItem>()
                .HasOne(i => i.Dashboard)
                .WithMany(d => d.Items)
                .HasForeignKey(i => i.DashboardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TrackerNotification>()
                .HasOne(n => n.Tracker)
                .WithMany(t => t.Notifications)
                .HasForeignKey(n => n.TrackerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NotificationEvent>()
                .HasOne(e => e.Notification)
                .WithOne(n => n.Event)
                .HasForeignKey<NotificationEvent>(e => e.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NotificationCondition>()
                .HasOne(c => c.Notification)
                .WithOne(n => n.Condition)
                .HasForeignKey<NotificationCondition>(c => c.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NotificationConditionFilter>()
                .HasOne(f => f.Condition)
                .WithMany(c => c.Filters)
                .HasForeignKey(f => f.ConditionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NotificationConditionFilter>()
                .HasOne(f => f.Field)
                .WithMany()
                .HasForeignKey(f => f.FieldId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<NotificationConditionPurposeField>()
                .HasOne(f => f.Condition)
                .WithMany(c => c.PurposeFields)
                .HasForeignKey(f => f.ConditionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NotificationTriggeredEntry>()
                .HasOne(t => t.Notification)
                .WithMany(n => n.TriggeredEntries)
                .HasForeignKey(t => t.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NotificationTriggeredEntry>()
                .HasOne(t => t.Entry)
                .WithMany()
                .HasForeignKey(t => t.EntryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NotificationTriggeredEntry>()
                .HasIndex(t => new { t.NotificationId, t.EntryId })
                .IsUnique();

            builder.Entity<UserPushSubscription>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
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
        public DbSet<TrackerConstantValue> TrackerConstantValues { get; set; }
        public DbSet<TrackerConstantValueFilter> TrackerConstantValueFilters { get; set; }
        public DbSet<Dashboard> Dashboards { get; set; }
        public DbSet<DashboardItem> DashboardItems { get; set; }
        public DbSet<TrackerNotification> TrackerNotifications { get; set; }
        public DbSet<NotificationEvent> NotificationEvents { get; set; }
        public DbSet<NotificationCondition> NotificationConditions { get; set; }
        public DbSet<NotificationConditionFilter> NotificationConditionFilters { get; set; }
        public DbSet<NotificationConditionPurposeField> NotificationConditionPurposeFields { get; set; }
        public DbSet<NotificationTriggeredEntry> NotificationTriggeredEntries { get; set; }
        public DbSet<UserPushSubscription> UserPushSubscriptions { get; set; }
    }
}
