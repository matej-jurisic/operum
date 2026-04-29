using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Dashboard;
using Operum.Model.DTOs.Dashboard.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Domain.Analytics;
using Operum.Service.Domain.Views;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Dashboards
{
    public class DashboardService(ICurrentUserService currentUserService, OperumContext db) : IDashboardService
    {
        public async Task<Result<List<DashboardDto>>> GetDashboards()
        {
            var user = currentUserService.GetCurrentUser();
            var dashboards = await db.Dashboards
                .Include(d => d.Items)
                .Where(d => d.UserId == user.Id)
                .ToListAsync();

            return Result.Success(dashboards.Select(MapToDto).ToList());
        }

        public async Task<Result<DashboardDto>> GetDashboard(string dashboardId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            return Result.Success(MapToDto(dashboard));
        }

        public async Task<Result<List<AnalyticDto>>> GetDashboardAnalytics(string dashboardId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var items = dashboard.Items.OrderBy(i => i.Order).ToList();
            var results = new List<AnalyticDto>();

            foreach (var item in items)
            {
                var analytic = await db.Analytics
                    .Include(a => a.AnalyticFields)
                        .ThenInclude(af => af.Field)
                    .FirstOrDefaultAsync(a => a.Id == item.AnalyticId);

                if (analytic == null) continue;

                var viewIds = ParseViewIds(item.ViewIds);
                var views = new List<View>();

                foreach (var viewId in viewIds)
                {
                    var view = await db.Views
                        .Include(v => v.Filters).ThenInclude(f => f.Field)
                        .Include(v => v.Sorts).ThenInclude(s => s.Field)
                        .FirstOrDefaultAsync(v => v.Id == viewId && v.TrackerId == item.TrackerId);
                    if (view != null) views.Add(view);
                }

                var entriesQuery = db.Entries
                    .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                    .Where(e => e.TrackerId == item.TrackerId);

                if (views.Count > 0)
                {
                    entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, ViewQueryBuilder.MergeViewFilters(views));
                    entriesQuery = ViewQueryBuilder.ApplyViewSorting(entriesQuery, ViewQueryBuilder.MergeViewSorts(views));
                }

                var entries = await entriesQuery.ToListAsync();

                var request = new AnalyticResultBuilderRequest
                {
                    Analytic = analytic,
                    Entries = entries,
                    FieldMap = analytic.AnalyticFields.ToDictionary(f => f.Purpose, f => f.Field)
                };

                var calculationResult = AnalyticResultBuilder.GetAnalyticResult(request);
                if (calculationResult.IsSuccess)
                {
                    // Use dashboard item ID so frontend can reference it for reorder/remove
                    calculationResult.Data.Id = item.Id;
                    calculationResult.Data.Order = item.Order;
                    results.Add(calculationResult.Data);
                }
            }

            return Result.Success(results);
        }

        public async Task<Result<DashboardDto>> CreateDashboard(CreateDashboardDto dto)
        {
            var user = currentUserService.GetCurrentUser();

            var count = await db.Dashboards.CountAsync(d => d.UserId == user.Id);
            if (count >= DataLimits.MaxDashboardCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("dashboards", DataLimits.MaxDashboardCount));

            var dashboard = new Dashboard
            {
                Name = dto.Name,
                Color = dto.Color,
                Icon = dto.Icon,
                UserId = user.Id
            };

            db.Dashboards.Add(dashboard);
            await db.SaveChangesAsync();

            return Result.Success(MapToDto(dashboard));
        }

        public async Task<Result> DeleteDashboard(string dashboardId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            db.Dashboards.Remove(dashboard);
            await db.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result<DashboardItemDto>> AddDashboardItem(string dashboardId, AddDashboardItemDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            if (dashboard.Items.Count >= DataLimits.MaxDashboardItemCount)
                return Result.Failure(ResultStatusCodes.Conflict, Messages.MaxNumberReached("dashboard items", DataLimits.MaxDashboardItemCount));

            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == dto.TrackerId);

            var hasAccess = tracker != null &&
                (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(ut => ut.ApplicationUserId == user.Id));

            if (tracker == null || !hasAccess)
                return Result.Failure(ResultStatusCodes.Forbidden);

            var analytic = await db.Analytics.FirstOrDefaultAsync(a => a.Id == dto.AnalyticId && a.TrackerId == dto.TrackerId);
            if (analytic == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("analytic"));

            var viewIds = dto.ViewIds.Where(v => !string.IsNullOrEmpty(v)).ToList();
            foreach (var viewId in viewIds)
            {
                var exists = await db.Views.AnyAsync(v => v.Id == viewId && v.TrackerId == dto.TrackerId);
                if (!exists)
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));
            }

            var nextOrder = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Order) + 1 : 0;

            var item = new DashboardItem
            {
                DashboardId = dashboardId,
                AnalyticId = dto.AnalyticId,
                TrackerId = dto.TrackerId,
                ViewIds = viewIds.Count > 0 ? string.Join(",", viewIds) : null,
                Order = nextOrder
            };

            db.DashboardItems.Add(item);
            await db.SaveChangesAsync();

            return Result.Success(new DashboardItemDto
            {
                Id = item.Id,
                AnalyticId = analytic.Id,
                AnalyticName = analytic.Name,
                TrackerId = tracker.Id,
                TrackerName = tracker.Name,
                ViewIds = viewIds,
                Order = item.Order
            });
        }

        public async Task<Result> RemoveDashboardItem(string dashboardId, string itemId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard item"));

            db.DashboardItems.Remove(item);
            await db.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> ReorderDashboardItems(string dashboardId, List<string> orderedItemIds)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            for (int i = 0; i < orderedItemIds.Count; i++)
            {
                var item = dashboard.Items.FirstOrDefault(x => x.Id == orderedItemIds[i]);
                if (item != null) item.Order = i;
            }

            await db.SaveChangesAsync();
            return Result.Success();
        }

        private async Task<Dashboard?> GetUserDashboard(string dashboardId)
        {
            var user = currentUserService.GetCurrentUser();
            return await db.Dashboards
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.Id == dashboardId && d.UserId == user.Id);
        }

        private static List<string> ParseViewIds(string? viewIds) =>
            string.IsNullOrEmpty(viewIds)
                ? []
                : viewIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        private static DashboardDto MapToDto(Dashboard d) => new()
        {
            Id = d.Id,
            Name = d.Name,
            Color = d.Color,
            Icon = d.Icon,
            Items = d.Items.OrderBy(i => i.Order).Select(i => new DashboardItemDto
            {
                Id = i.Id,
                AnalyticId = i.AnalyticId,
                TrackerId = i.TrackerId,
                ViewIds = ParseViewIds(i.ViewIds),
                Order = i.Order
            }).ToList()
        };
    }
}
