using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.DTOs.Trackers;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.DTOs.Users;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Helpers;
using Operum.Service.Helpers.Analytics;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Services.Authorization;

namespace Operum.Service.Services.Trackers
{
    public class TrackersService(IAuthorizationService authorizationService, OperumContext db, IMapper mapper) : ITrackersService
    {
        public async Task<Result<TrackerDto>> CreateTracker(CreateTrackerDto tracker)
        {
            var user = authorizationService.GetCurrentUserDto();
            var trackerCount = await db.Trackers.Where(x => x.OwnerId == user.Id).CountAsync();
            if (trackerCount >= DataLimits.MaxTrackerCount)
            {
                return Result.Failure(StatusCodeEnum.BadRequest, $"Maximum number of {DataLimits.MaxTrackerCount} trackers reached.");
            }

            if (tracker.TrackerTypeId != null && !await authorizationService.HasRole("Admin"))
            {
                return Result.Failure(StatusCodeEnum.Forbidden, "You don't have permissions to create template trackers.");
            }

            Tracker? templateTracker = null;
            if (tracker.TemplateTrackerId != null)
            {
                templateTracker = await db.Trackers
                    .Include(t => t.Fields)
                    .Include(t => t.Views)
                        .ThenInclude(v => v.Sorts)
                    .Include(t => t.Views)
                        .ThenInclude(v => v.Filters)
                    .FirstOrDefaultAsync(t => t.Id == tracker.TemplateTrackerId);

                if (templateTracker == null || templateTracker.TrackerTypeId != (int)TrackerTypeEnum.PublicTemplate)
                {
                    return Result.Failure(StatusCodeEnum.NotFound, "Template tracker not found.");
                }
            }

            // Create the tracker
            var trackerModel = mapper.Map<CreateTrackerDto, Tracker>(tracker);
            trackerModel.OwnerId = user.Id;
            trackerModel.Color = trackerModel.Color?.ToLower();

            await db.Trackers.AddAsync(trackerModel);
            await db.SaveChangesAsync();

            // If creating from template, copy template data
            if (templateTracker != null)
            {
                await CopyTemplateData(templateTracker, trackerModel);
            }

            var created = await GetTracker(trackerModel.Id);
            return Result.Success(created.Data);
        }

        private async Task CopyTemplateData(Tracker templateTracker, Tracker newTracker)
        {
            // Dictionary to map old field IDs to new field IDs
            var fieldIdMapping = new Dictionary<string, string>();

            // Copy fields
            foreach (var templateField in templateTracker.Fields)
            {
                var newField = new Field
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = templateField.Name,
                    Description = templateField.Description,
                    Type = templateField.Type,
                    Required = templateField.Required,
                    Visible = templateField.Visible,
                    Order = templateField.Order,
                    TrackerId = newTracker.Id,
                };

                fieldIdMapping[templateField.Id] = newField.Id;
                await db.Fields.AddAsync(newField);
            }

            // Save fields first so they exist for view references
            await db.SaveChangesAsync();

            // Dictionary to map old view IDs to new view IDs
            var viewIdMapping = new Dictionary<string, string>();

            // Copy views
            foreach (var templateView in templateTracker.Views)
            {
                var newView = new View
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = templateView.Name,
                    Description = templateView.Description,
                    TrackerId = newTracker.Id
                };

                viewIdMapping[templateView.Id] = newView.Id;
                await db.Views.AddAsync(newView);
            }

            // Save views first so they exist for sort/filter references
            await db.SaveChangesAsync();

            // Copy view sorts
            foreach (var templateView in templateTracker.Views)
            {
                foreach (var templateSort in templateView.Sorts)
                {
                    // Only create sort if the field was copied
                    if (fieldIdMapping.TryGetValue(templateSort.FieldId, out var newFieldId) &&
                        viewIdMapping.TryGetValue(templateView.Id, out var newViewId))
                    {
                        var newSort = new ViewSort
                        {
                            Id = Guid.NewGuid().ToString(),
                            ViewId = newViewId,
                            FieldId = newFieldId,
                            Order = templateSort.Order,
                            Descending = templateSort.Descending
                        };

                        await db.ViewSorts.AddAsync(newSort);
                    }
                }
            }

            // Copy view filters
            foreach (var templateView in templateTracker.Views)
            {
                foreach (var templateFilter in templateView.Filters)
                {
                    // Only create filter if the field was copied
                    if (fieldIdMapping.TryGetValue(templateFilter.FieldId, out var newFieldId) &&
                        viewIdMapping.TryGetValue(templateView.Id, out var newViewId))
                    {
                        var newFilter = new ViewFilter
                        {
                            Id = Guid.NewGuid().ToString(),
                            ViewId = newViewId,
                            FieldId = newFieldId,
                            Operator = templateFilter.Operator,
                            Value = templateFilter.Value
                        };

                        await db.ViewFilters.AddAsync(newFilter);
                    }
                }
            }

            // Save all the sorts and filters
            await db.SaveChangesAsync();
        }

        public async Task<Result> DeleteTracker(string id)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(id);

            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return Result.Failure(StatusCodeEnum.NotFound);
            }

            db.Trackers.Remove(tracker);
            await db.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result<TrackerDto>> GetTracker(string id)
        {
            var user = authorizationService.GetCurrentUserDto();

            var isAdmin = await authorizationService.HasRole("Admin");

            var tracker = await db.Trackers
                .Include(x => x.Fields)
                .Include(x => x.Owner)
                .Include(x => x.ApplicationUserTrackers)
                .Include(x => x.TrackerType)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (tracker == null)
            {
                return Result.Failure(StatusCodeEnum.NotFound);
            }

            bool hasAccess = tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id);

            if (isAdmin)
            {
                hasAccess = hasAccess || tracker.TrackerTypeId != null;
            }

            if (!hasAccess)
            {
                return Result.Failure(StatusCodeEnum.Forbidden);
            }

            return Result.Success(mapper.Map<Tracker, TrackerDto>(tracker));
        }

        public async Task<Result<List<TrackerDto>>> GetTrackerList(string filter)
        {
            var user = authorizationService.GetCurrentUserDto();

            if (filter == TrackerFilters.Owned)
            {
                var ownedTrackers = await db.Trackers
                .Include(x => x.ApplicationUserTrackers)
                .Include(x => x.Owner)
                .Where(x => x.TrackerTypeId == null && x.OwnerId == user.Id)
                .ToListAsync();
                return Result.Success(mapper.Map<List<Tracker>, List<TrackerDto>>(ownedTrackers));
            }
            else if (filter == TrackerFilters.Collaborating)
            {
                var trackers = await db.Trackers
                    .Include(x => x.ApplicationUserTrackers)
                    .Include(x => x.Owner)
                    .Where(x => x.TrackerTypeId == null && x.OwnerId != user.Id && x.ApplicationUserTrackers.Any(a => a.ApplicationUserId == user.Id))
                    .ToListAsync();
                return Result.Success(mapper.Map<List<Tracker>, List<TrackerDto>>(trackers));
            }

            return Result.Failure(StatusCodeEnum.BadRequest, "Invalid filter.");
        }

        public async Task<Result<List<TrackerDto>>> GetAllTemplateTrackerList()
        {
            var trackers = await db.Trackers
                .Include(x => x.Fields)
                .Include(x => x.TrackerType)
                .Where(x => x.TrackerTypeId == (int)TrackerTypeEnum.PublicTemplate || x.TrackerTypeId == (int)TrackerTypeEnum.TemplateDraft)
                .ToListAsync();
            return Result.Success(mapper.Map<List<Tracker>, List<TrackerDto>>(trackers));
        }

        public async Task<Result<List<TrackerDto>>> GetPublicTemplateTrackerList()
        {
            var trackers = await db.Trackers
                .Include(x => x.Fields)
                .Include(x => x.TrackerType)
                .Where(x => x.TrackerTypeId == (int)TrackerTypeEnum.PublicTemplate)
                .ToListAsync();
            return Result.Success(mapper.Map<List<Tracker>, List<TrackerDto>>(trackers));
        }

        public async Task<Result<TrackerDto>> UpdateTracker(string id, UpdateTrackerDto tracker)
        {
            var user = authorizationService.GetCurrentUserDto();
            var originalTracker = await db.Trackers.FindAsync(id);

            if (originalTracker?.OwnerId != user.Id)
            {
                return Result.Failure(StatusCodeEnum.NotFound);
            }

            if (tracker.TrackerTypeId != null && !await authorizationService.HasRole("Admin"))
            {
                return Result.Failure(StatusCodeEnum.Forbidden, "You don't have permissions to create template trackers.");
            }

            mapper.Map(tracker, originalTracker);
            db.Trackers.Update(originalTracker);
            await db.SaveChangesAsync();

            var updatedTracker = await GetTracker(originalTracker.Id);
            return Result.Success(updatedTracker.Data);
        }

        public async Task<Result<List<AnalyticResultDto>>> GetTrackerAnalytics(string trackerId, string? viewId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers
                .Include(x => x.ApplicationUserTrackers)
                .FirstOrDefaultAsync(x => x.Id == trackerId);

            var hasAccess = tracker != null && (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (tracker == null || !hasAccess)
            {
                return Result.Failure(StatusCodeEnum.Forbidden);
            }

            View? view = null;
            if (!string.IsNullOrEmpty(viewId))
            {
                view = await db.Views
                    .Include(v => v.Filters)
                    .ThenInclude(v => v.Field)
                    .Include(v => v.Sorts)
                    .ThenInclude(s => s.Field)
                    .FirstOrDefaultAsync(v => v.Id == viewId && v.TrackerId == trackerId);

                if (view == null)
                {
                    return Result.Failure(StatusCodeEnum.NotFound, "View not found or doesn't belong to this tracker");
                }
            }

            var entriesQuery = db.Entries
                .Include(x => x.FieldValues)
                .ThenInclude(x => x.Field)
                .Where(x => x.TrackerId == trackerId);

            if (view != null)
            {
                entriesQuery = ViewHelpers.ApplyViewFilters(entriesQuery, view.Filters);
                entriesQuery = ViewHelpers.ApplyViewSorting(entriesQuery, view.Sorts);
            }

            var entries = await entriesQuery.ToListAsync();

            var analytics = await db.Analytics
                .Include(x => x.AnalyticFields)
                    .ThenInclude(x => x.Field)
                .Where(x => x.TrackerId == trackerId)
                .OrderBy(x => x.Order)
                .ToListAsync();

            List<AnalyticResultDto> analyticResults = [];

            foreach (var analytic in analytics)
            {
                var calculationResult = AnalyticsHelpers.GetAnalyticResult(analytic, entries);
                if (calculationResult.IsSuccess)
                {
                    calculationResult.Data.AnalyticId = analytic.Id;
                    analyticResults.Add(calculationResult.Data);
                }
            }

            return Result.Success(analyticResults);
        }

        public async Task<Result> UpdateDefaultView(string trackerId, string? defaultViewId)
        {
            var user = authorizationService.GetCurrentUserDto();

            Tracker? tracker;

            if (string.IsNullOrEmpty(defaultViewId))
            {
                tracker = await db.Trackers.FindAsync(trackerId);
            }
            else
            {
                tracker = await db.Trackers
                    .Include(t => t.Views)
                    .FirstOrDefaultAsync(t => t.Id == trackerId && t.Views.Any(v => v.Id == defaultViewId));
            }

            if (tracker == null || !user.Owns(tracker))
            {
                return Result.Failure(StatusCodeEnum.NotFound);
            }

            tracker.DefaultViewId = defaultViewId;
            db.Update(tracker);
            await db.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> AddUserToTracker(string trackerId, ModifyUserTrackerDto addUserToTracker)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || !user.Owns(tracker))
            {
                return Result.Failure(StatusCodeEnum.NotFound, "Tracker not found.");
            }

            var userToAdd = await db.ApplicationUsers.FirstOrDefaultAsync(x => x.UserName == addUserToTracker.Username);

            if (userToAdd == null)
            {
                return Result.Failure(StatusCodeEnum.NotFound, "User not found");
            }

            if (userToAdd.Id == user.Id)
            {
                return Result.Failure(StatusCodeEnum.BadRequest, "Cannot add yourself to your own tracker.");
            }

            var userTrackerRelation = await db.ApplicationUserTrackers.FirstOrDefaultAsync(x => x.TrackerId == trackerId && x.ApplicationUserId == userToAdd.Id);

            if (userTrackerRelation != null)
            {
                return Result.Failure(StatusCodeEnum.BadRequest, "User is already in tracker.");
            }

            ApplicationUserTracker newRelation = new()
            {
                ApplicationUserId = userToAdd.Id,
                TrackerId = trackerId,
            };

            await db.ApplicationUserTrackers.AddAsync(newRelation);
            await db.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<List<PublicApplicationUserDto>>> GetApplicationUserTrackerList(string trackerId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers
              .Include(x => x.ApplicationUserTrackers)
              .FirstOrDefaultAsync(x => x.Id == trackerId);

            var hasAccess = tracker != null && (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (tracker == null || !hasAccess)
            {
                return Result.Failure(StatusCodeEnum.Forbidden);
            }

            var appUserTrackerList = await db.ApplicationUserTrackers
                .Include(x => x.ApplicationUser)
                .Where(x => x.TrackerId == trackerId)
                .OrderBy(x => x.ApplicationUser.UserName)
                .Select(x => x.ApplicationUser)
                .ToListAsync();

            return Result.Success(mapper.Map<List<ApplicationUser>, List<PublicApplicationUserDto>>(appUserTrackerList));
        }

        public async Task<Result> RemoveUserFromTracker(string trackerId, ModifyUserTrackerDto addUserToTracker)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || !user.Owns(tracker))
            {
                return Result.Failure(StatusCodeEnum.NotFound, "Tracker not found.");
            }

            var userToRemove = await db.ApplicationUsers.FirstOrDefaultAsync(x => x.UserName == addUserToTracker.Username);

            if (userToRemove == null)
            {
                return Result.Failure(StatusCodeEnum.NotFound, "User not found");
            }

            var userTrackerRelation = await db.ApplicationUserTrackers.FirstOrDefaultAsync(x => x.TrackerId == trackerId && x.ApplicationUserId == userToRemove.Id);

            if (userTrackerRelation == null)
            {
                return Result.Failure(StatusCodeEnum.BadRequest, "User is not in tracker.");
            }

            db.ApplicationUserTrackers.Remove(userTrackerRelation);
            await db.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> AddAnalytic(string trackerId, AddAnalyticDto addAnalytic)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);
            if (tracker == null || !user.Owns(tracker))
            {
                return Result.Failure(StatusCodeEnum.NotFound, "Tracker not found.");
            }

            var addFieldIds = addAnalytic.AnalyticFields
                .Select(x => x.FieldId)
                .Distinct()
                .ToList();

            var fields = await db.Fields
                .Where(x => addFieldIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            foreach (var field in addAnalytic.AnalyticFields)
            {
                var existingField = await db.Fields.FirstOrDefaultAsync(x => x.Id == field.FieldId);
                if (existingField == null || !fields.ContainsKey(field.FieldId))
                {
                    return Result.Failure(StatusCodeEnum.NotFound,
                        $"Field {field.FieldId} not found.");
                }
                if (!AnalyticDefinitions.IsValidDataType(addAnalytic.ResultType, addAnalytic.Code, field.Purpose, existingField.Type))
                {
                    return Result.Failure(StatusCodeEnum.NotFound,
                        $"Invalid analytic configuration.");
                }
            }

            var count = await db.Analytics.Where(x => x.TrackerId == trackerId).CountAsync();

            if (count >= DataLimits.MaxAnalyticCount)
            {
                return Result.Failure(StatusCodeEnum.BadRequest, $"Maximum number of {DataLimits.MaxAnalyticCount} analytics reached.");
            }

            Analytic analytic = new()
            {
                TrackerId = tracker.Id,
                Code = addAnalytic.Code,
                ResultType = addAnalytic.ResultType,
            };

            var maxOrder = await db.Analytics
                .Where(x => x.TrackerId == trackerId)
                .MaxAsync(x => x.Order) ?? 0;
            analytic.Order = maxOrder + 1;

            foreach (var analyticField in addAnalytic.AnalyticFields)
            {
                analytic.AnalyticFields.Add(new AnalyticField()
                {
                    FieldId = analyticField.FieldId,
                    Purpose = analyticField.Purpose,
                });
            }

            db.Analytics.Add(analytic);
            await db.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> RemoveAnalytic(string trackerId, string trackerAnalyticId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);
            if (tracker == null || !user.Owns(tracker))
            {
                return Result.Failure(StatusCodeEnum.NotFound, "Tracker not found.");
            }

            var analytic = await db.Analytics.FindAsync(trackerAnalyticId);
            if (analytic == null || analytic.TrackerId != trackerId)
            {
                return Result.Failure(StatusCodeEnum.NotFound, "Analytic not found.");
            }

            db.Analytics.Remove(analytic);
            await db.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> ReorderAnalytics(string trackerId, ReorderAnalyticsDto reorderAnalyticsDto)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || !user.Owns(tracker))
            {
                return Result.Failure(StatusCodeEnum.NotFound);
            }

            var existingTrackerAnalytics = await db.Analytics
                .Where(x => x.TrackerId == trackerId)
                .Select(x => x.Id)
                .ToListAsync();

            var requestedTrackerAnalyticIds = reorderAnalyticsDto.AnalyticIds.ToHashSet();
            var existingTrackerAnalyticIds = existingTrackerAnalytics.ToHashSet();

            if (!requestedTrackerAnalyticIds.SetEquals(existingTrackerAnalyticIds))
            {
                return Result.Failure(StatusCodeEnum.BadRequest,
                    "Invalid tracker analytic IDs provided or missing IDs in reorder request.");
            }

            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                for (int i = 0; i < reorderAnalyticsDto.AnalyticIds.Count; i++)
                {
                    var analyticId = reorderAnalyticsDto.AnalyticIds[i];
                    var analytics = await db.Analytics.FindAsync(analyticId);

                    if (analytics != null && analytics.TrackerId == trackerId)
                    {
                        analytics.Order = i + 1;
                        db.Analytics.Update(analytics);
                    }
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result.Success();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return Result.Failure(StatusCodeEnum.InternalServerError,
                    "Failed to reorder fields. Please try again.");
            }
        }
    }
}
