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

            // A Query is a field-agnostic, user-owned clause pooled across every view and
            // dashboard view that reads the same way. It outlives any one of them; only
            // deleting the user takes it down.
            builder.Entity<Query>()
                .HasOne(q => q.Owner)
                .WithMany()
                .HasForeignKey(q => q.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ViewQuery>()
                .HasOne(vq => vq.View)
                .WithMany(v => v.ViewQueries)
                .HasForeignKey(vq => vq.ViewId)
                .OnDelete(DeleteBehavior.Cascade);

            // A Query is independent of any View: deleting it should just drop it from
            // whichever Views used it, never cascade back into deleting the Query itself.
            builder.Entity<ViewQuery>()
                .HasOne(vq => vq.Query)
                .WithMany()
                .HasForeignKey(vq => vq.QueryId)
                .OnDelete(DeleteBehavior.Cascade);

            // A ViewQuery binds its clause to one concrete field; deleting the field drops
            // the binding (and so the clause) from whichever views used it -- the same edge
            // the old field-bound Query had.
            builder.Entity<ViewQuery>()
                .HasOne(vq => vq.Field)
                .WithMany()
                .HasForeignKey(vq => vq.FieldId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DashboardView>()
                .HasOne(dv => dv.Dashboard)
                .WithMany()
                .HasForeignKey(dv => dv.DashboardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DashboardViewQuery>()
                .HasOne(dvq => dvq.DashboardView)
                .WithMany(dv => dv.DashboardViewQueries)
                .HasForeignKey(dvq => dvq.DashboardViewId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DashboardViewQuery>()
                .HasOne(dvq => dvq.Query)
                .WithMany()
                .HasForeignKey(dvq => dvq.QueryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ViewColumn>()
                .HasOne(vc => vc.View)
                .WithMany(v => v.ViewColumns)
                .HasForeignKey(vc => vc.ViewId)
                .OnDelete(DeleteBehavior.Cascade);

            // A column is a field and nothing else, so deleting the field drops it from
            // whichever views showed it, the same way it drops the queries over it.
            builder.Entity<ViewColumn>()
                .HasOne(vc => vc.Field)
                .WithMany()
                .HasForeignKey(vc => vc.FieldId)
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

            builder.Entity<DashboardItemSource>()
                .HasOne(s => s.DashboardItem)
                .WithMany(i => i.Sources)
                .HasForeignKey(s => s.DashboardItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Widget>()
                .HasOne(w => w.Owner)
                .WithMany()
                .HasForeignKey(w => w.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WidgetSource>()
                .HasOne(s => s.Widget)
                .WithMany(w => w.Sources)
                .HasForeignKey(s => s.WidgetId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WidgetSourceField>()
                .HasOne(f => f.WidgetSource)
                .WithMany(s => s.Fields)
                .HasForeignKey(f => f.WidgetSourceId)
                .OnDelete(DeleteBehavior.Cascade);

            // A widget's field mapping is the whole of what a Field deletion should be
            // able to take down -- the widget itself survives on every dashboard it's
            // placed on and falls back to a degraded render (see AnalyticResultBuilder).
            builder.Entity<WidgetSourceField>()
                .HasOne(f => f.Field)
                .WithMany()
                .HasForeignKey(f => f.FieldId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EntriesWidget>()
                .HasOne(w => w.Owner)
                .WithMany()
                .HasForeignKey(w => w.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // A placement can't render without its shared definition, so deleting the
            // Widget/EntriesWidget takes every placement of it down too -- the sharpest
            // edge of the reuse model, surfaced to the user before deleting from the
            // library.
            builder.Entity<DashboardItem>()
                .HasOne(i => i.Widget)
                .WithMany()
                .HasForeignKey(i => i.WidgetId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DashboardItem>()
                .HasOne(i => i.EntriesWidget)
                .WithMany()
                .HasForeignKey(i => i.EntriesWidgetId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DashboardItemSource>()
                .HasOne(s => s.WidgetSource)
                .WithMany()
                .HasForeignKey(s => s.WidgetSourceId)
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

            builder.Entity<Integration>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One connection per provider account per user. ExternalAccountId is null for a
            // push-only connection, and Postgres treats nulls as distinct in a unique index,
            // so this constrains resolved accounts and lets a user hold several push
            // connections to the same provider -- one per tracker they wire up.
            builder.Entity<Integration>()
                .HasIndex(i => new { i.UserId, i.Provider, i.ExternalAccountId })
                .IsUnique();

            builder.Entity<IntegrationTarget>()
                .HasOne(t => t.Integration)
                .WithMany(i => i.Targets)
                .HasForeignKey(t => t.IntegrationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Deleting the tracker takes its targets with it; the connection survives, since
            // it may feed other trackers. Entries already imported are left alone.
            builder.Entity<IntegrationTarget>()
                .HasOne(t => t.Tracker)
                .WithMany()
                .HasForeignKey(t => t.TrackerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<IntegrationTarget>()
                .HasIndex(t => new { t.IntegrationId, t.TrackerId, t.ResourceType })
                .IsUnique();

            // The webhook route carries only this token, so it has to identify a target on
            // its own. Filtered because pull targets have none and many nulls must stay legal.
            builder.Entity<IntegrationTarget>()
                .HasIndex(t => t.WebhookToken)
                .IsUnique()
                .HasFilter(@"""WebhookToken"" IS NOT NULL");

            builder.Entity<IntegrationFieldMapping>()
                .HasOne(m => m.Target)
                .WithMany(t => t.Mappings)
                .HasForeignKey(m => m.TargetId)
                .OnDelete(DeleteBehavior.Cascade);

            // Deleting a field drops whatever mapping fed it, the same edge a view column or
            // a widget's field mapping has.
            builder.Entity<IntegrationFieldMapping>()
                .HasOne(m => m.Field)
                .WithMany()
                .HasForeignKey(m => m.FieldId)
                .OnDelete(DeleteBehavior.Cascade);

            // One source per field. Two sources writing the same field would race on every
            // sync and neither would win predictably.
            builder.Entity<IntegrationFieldMapping>()
                .HasIndex(m => new { m.TargetId, m.FieldId })
                .IsUnique();

            // An integration's idempotency key: re-ingesting a record it has already written
            // must update that entry, not add a second one. Filtered so it constrains only
            // integration-authored rows -- every hand-created and CSV-imported entry leaves
            // both columns null, and many nulls must stay legal.
            builder.Entity<Entry>()
                .HasIndex(e => new { e.TrackerId, e.Source, e.ExternalId })
                .IsUnique()
                .HasFilter(@"""Source"" IS NOT NULL");

            // Serves EntryWriter's group reconciliation: "every entry this parent produced".
            builder.Entity<Entry>()
                .HasIndex(e => new { e.TrackerId, e.Source, e.ExternalGroupId })
                .HasFilter(@"""ExternalGroupId"" IS NOT NULL");

            // Declared explicitly because the filtered index above would otherwise suppress
            // it: convention sees an index leading with TrackerId and skips the FK one. A
            // partial index cannot serve "every entry in this tracker" -- that predicate does
            // not imply Source IS NOT NULL -- so losing this would leave the single hottest
            // entry query (EntriesService.GetEntries) with no index at all.
            builder.Entity<Entry>()
                .HasIndex(e => e.TrackerId);

            // Entries are EAV, so reading them is all correlated subqueries over FieldValues:
            // every view filter is an EXISTS (ViewQueryBuilder.ApplyViewFilters) and every
            // view sort is a scalar FirstOrDefault (ApplyViewSorting), up to MaxFilters +
            // MaxSorts of them on one page load. Until DataLimits was raised for integrations
            // the table was small enough that the FK indexes EF generates covered it; at the
            // new MaxEntryCount it is not.
            //
            // These two shapes are what those queries actually ask for. Both lead with the
            // column the subquery correlates on, so they also stand in for the single-column
            // FK indexes convention would otherwise add.
            //
            // (Postgres could make the sort lookup index-only with an INCLUDE of the value
            // columns. Left off deliberately -- it is a provider-specific annotation and the
            // plain composite is the thing to measure first.)
            builder.Entity<FieldValue>()
                .HasIndex(fv => new { fv.EntryId, fv.FieldId });

            // The selective direction: "which entries have this field above that value".
            // One per value column, because a filter only ever touches the column its
            // field's type maps to.
            builder.Entity<FieldValue>()
                .HasIndex(fv => new { fv.FieldId, fv.NumberValue });

            builder.Entity<FieldValue>()
                .HasIndex(fv => new { fv.FieldId, fv.DateTimeValue });

            builder.Entity<FieldValue>()
                .HasIndex(fv => new { fv.FieldId, fv.TimeSpanValue });

            builder.Entity<FieldValue>()
                .HasIndex(fv => new { fv.FieldId, fv.BooleanValue });

            // Serves Equals/NotEquals and, on Postgres, StartsWith. Contains cannot use a
            // btree at all -- if that operator turns out to be hot on a large tracker it
            // needs a trigram index, which is a Postgres-only migration.
            builder.Entity<FieldValue>()
                .HasIndex(fv => new { fv.FieldId, fv.StringValue });
        }

        public override DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Tracker> Trackers { get; set; }
        public DbSet<Field> Fields { get; set; }
        public DbSet<Entry> Entries { get; set; }
        public DbSet<FieldValue> FieldValues { get; set; }
        public DbSet<View> Views { get; set; }
        public DbSet<ViewQuery> ViewQueries { get; set; }
        public DbSet<Query> Queries { get; set; }
        public DbSet<ViewGroup> ViewGroups { get; set; }
        public DbSet<ViewColumn> ViewColumns { get; set; }
        public DbSet<TrackerType> TrackerTypes { get; set; }
        public DbSet<UserTracker> UserTrackers { get; set; }
        public DbSet<TrackerConstant> TrackerConstants { get; set; }
        public DbSet<TrackerConstantValue> TrackerConstantValues { get; set; }
        public DbSet<TrackerConstantValueFilter> TrackerConstantValueFilters { get; set; }
        public DbSet<Dashboard> Dashboards { get; set; }
        public DbSet<DashboardItem> DashboardItems { get; set; }
        public DbSet<DashboardItemSource> DashboardItemSources { get; set; }
        public DbSet<DashboardView> DashboardViews { get; set; }
        public DbSet<DashboardViewQuery> DashboardViewQueries { get; set; }
        public DbSet<Widget> Widgets { get; set; }
        public DbSet<WidgetSource> WidgetSources { get; set; }
        public DbSet<WidgetSourceField> WidgetSourceFields { get; set; }
        public DbSet<EntriesWidget> EntriesWidgets { get; set; }
        public DbSet<TrackerNotification> TrackerNotifications { get; set; }
        public DbSet<NotificationEvent> NotificationEvents { get; set; }
        public DbSet<NotificationCondition> NotificationConditions { get; set; }
        public DbSet<NotificationConditionFilter> NotificationConditionFilters { get; set; }
        public DbSet<NotificationConditionPurposeField> NotificationConditionPurposeFields { get; set; }
        public DbSet<NotificationTriggeredEntry> NotificationTriggeredEntries { get; set; }
        public DbSet<UserPushSubscription> UserPushSubscriptions { get; set; }
        public DbSet<Integration> Integrations { get; set; }
        public DbSet<IntegrationTarget> IntegrationTargets { get; set; }
        public DbSet<IntegrationFieldMapping> IntegrationFieldMappings { get; set; }
    }
}
