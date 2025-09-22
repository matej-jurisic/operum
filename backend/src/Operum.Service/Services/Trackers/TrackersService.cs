using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Trackers;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.DTOs.Users;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Helpers;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Services.Authorization;

namespace Operum.Service.Services.Trackers
{
    public class TrackersService(IAuthorizationService authorizationService, OperumContext db, IMapper mapper) : ITrackersService
    {
        public async Task<ServiceResponse<TrackerDto>> CreateTracker(CreateTrackerDto tracker)
        {
            var user = authorizationService.GetCurrentUserDto();
            var trackerCount = await db.Trackers.Where(x => x.OwnerId == user.Id).CountAsync();
            if (trackerCount >= DataLimits.MaxTrackerCount)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, $"Maximum number of trackers {DataLimits.MaxTrackerCount} reached.");
            }

            if (tracker.TrackerTypeId != null && !await authorizationService.HasRole("Admin"))
            {
                return ServiceResponse.Failure(StatusCodeEnum.Forbidden, "You don't have permissions to create template trackers.");
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
                    return ServiceResponse.Failure(StatusCodeEnum.NotFound, "Template tracker not found.");
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
            return ServiceResponse.Success(created.Data);
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

        public async Task<ServiceResponse> DeleteTracker(string id)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(id);

            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            db.Trackers.Remove(tracker);
            await db.SaveChangesAsync();
            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse<TrackerDto>> GetTracker(string id)
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
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            bool hasAccess = tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id);

            if (isAdmin)
            {
                hasAccess = hasAccess || tracker.TrackerTypeId != null;
            }

            if (!hasAccess)
            {
                return ServiceResponse.Failure(StatusCodeEnum.Forbidden);
            }

            return ServiceResponse.Success(mapper.Map<Tracker, TrackerDto>(tracker));
        }

        public async Task<ServiceResponse<List<TrackerDto>>> GetTrackerList(string filter)
        {
            var user = authorizationService.GetCurrentUserDto();

            if (filter == TrackerFilters.Owned)
            {
                var ownedTrackers = await db.Trackers
                .Include(x => x.ApplicationUserTrackers)
                .Include(x => x.Owner)
                .Where(x => x.TrackerTypeId == null && x.OwnerId == user.Id)
                .ToListAsync();
                return ServiceResponse.Success(mapper.Map<List<Tracker>, List<TrackerDto>>(ownedTrackers));
            }
            else if (filter == TrackerFilters.Collaborating)
            {
                var trackers = await db.Trackers
                    .Include(x => x.ApplicationUserTrackers)
                    .Include(x => x.Owner)
                    .Where(x => x.TrackerTypeId == null && x.OwnerId != user.Id && x.ApplicationUserTrackers.Any(a => a.ApplicationUserId == user.Id))
                    .ToListAsync();
                return ServiceResponse.Success(mapper.Map<List<Tracker>, List<TrackerDto>>(trackers));
            }

            return ServiceResponse.Failure(StatusCodeEnum.BadRequest, "Invalid filter.");
        }

        public async Task<ServiceResponse<List<TrackerDto>>> GetAllTemplateTrackerList()
        {
            var trackers = await db.Trackers
                .Include(x => x.Fields)
                .Include(x => x.TrackerType)
                .Where(x => x.TrackerTypeId == (int)TrackerTypeEnum.PublicTemplate || x.TrackerTypeId == (int)TrackerTypeEnum.TemplateDraft)
                .ToListAsync();
            return ServiceResponse.Success(mapper.Map<List<Tracker>, List<TrackerDto>>(trackers));
        }

        public async Task<ServiceResponse<List<TrackerDto>>> GetPublicTemplateTrackerList()
        {
            var trackers = await db.Trackers
                .Include(x => x.Fields)
                .Include(x => x.TrackerType)
                .Where(x => x.TrackerTypeId == (int)TrackerTypeEnum.PublicTemplate)
                .ToListAsync();
            return ServiceResponse.Success(mapper.Map<List<Tracker>, List<TrackerDto>>(trackers));
        }

        public async Task<ServiceResponse<TrackerDto>> UpdateTracker(string id, UpdateTrackerDto tracker)
        {
            var user = authorizationService.GetCurrentUserDto();
            var originalTracker = await db.Trackers.FindAsync(id);

            if (originalTracker?.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            if (tracker.TrackerTypeId != null && !await authorizationService.HasRole("Admin"))
            {
                return ServiceResponse.Failure(StatusCodeEnum.Forbidden, "You don't have permissions to create template trackers.");
            }

            mapper.Map(tracker, originalTracker);
            db.Trackers.Update(originalTracker);
            await db.SaveChangesAsync();

            var updatedTracker = await GetTracker(originalTracker.Id);
            return ServiceResponse.Success(updatedTracker.Data);
        }

        public async Task<ServiceResponse<List<FieldAnalyticsDto>>> GetTrackerAnalytics(string trackerId, string? viewId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers
                .Include(x => x.ApplicationUserTrackers)
                .FirstOrDefaultAsync(x => x.Id == trackerId);

            var hasAccess = tracker != null && (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (tracker == null || !hasAccess)
            {
                return ServiceResponse.Failure(StatusCodeEnum.Forbidden);
            }

            View? view = null;
            if (!string.IsNullOrEmpty(viewId))
            {
                view = await db.Views
                    .Include(v => v.Filters)
                    .ThenInclude(s => s.Field)
                    .FirstOrDefaultAsync(v => v.Id == viewId && v.TrackerId == trackerId);

                if (view == null)
                {
                    return ServiceResponse.Failure(StatusCodeEnum.NotFound, "View not found or doesn't belong to this tracker");
                }
            }

            var entriesQuery = db.Entries
                .Include(x => x.FieldValues)
                .ThenInclude(x => x.Field)
                .Where(x => x.TrackerId == trackerId);

            if (view != null && view.Filters.Count != 0)
            {
                entriesQuery = ViewHelpers.ApplyViewFilters(entriesQuery, view.Filters);
            }

            var entries = await entriesQuery.ToListAsync();

            // Process analytics in parallel with proper null handling
            var analyticsResult = entries
                .SelectMany(e => e.FieldValues)
                .GroupBy(fv => fv.Field.Id)
                .AsParallel()
                .Select(group =>
                {
                    var field = group.First().Field;
                    var fieldValues = group.ToList();

                    var analytics = field.Type switch
                    {
                        DataTypes.Number => TrackerAnalyticsHelpers.GetNumericAnalytics(fieldValues),
                        DataTypes.DateTime => TrackerAnalyticsHelpers.GetDateTimeAnalytics(fieldValues),
                        DataTypes.Date => TrackerAnalyticsHelpers.GetDateAnalytics(fieldValues),
                        DataTypes.TimeSpan => TrackerAnalyticsHelpers.GetTimeSpanAnalytics(fieldValues),
                        DataTypes.Bool => TrackerAnalyticsHelpers.GetBooleanAnalytics(fieldValues),
                        _ => null
                    };

                    if (analytics != null)
                    {
                        analytics.FieldName = field.Name;
                        analytics.FieldType = field.Type;
                        analytics.FieldOrder = field.Order;
                    }

                    return analytics;
                })
                .Where(analytics => analytics != null)
                .Cast<FieldAnalyticsDto>()
                .OrderBy(a => a.FieldOrder)
                .ToList();

            return ServiceResponse.Success(analyticsResult);
        }

        public async Task<ServiceResponse> UpdateDefaultView(string trackerId, string? defaultViewId)
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
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            tracker.DefaultViewId = defaultViewId;
            db.Update(tracker);
            await db.SaveChangesAsync();

            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse> AddUserToTracker(string trackerId, ModifyUserTrackerDto addUserToTracker)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || !user.Owns(tracker))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound, "Tracker not found.");
            }

            var userToAdd = await db.ApplicationUsers.FirstOrDefaultAsync(x => x.UserName == addUserToTracker.Username);

            if (userToAdd == null)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound, "User not found");
            }

            if (userToAdd.Id == user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, "Cannot add yourself to your own tracker.");
            }

            var userTrackerRelation = await db.ApplicationUserTrackers.FirstOrDefaultAsync(x => x.TrackerId == trackerId && x.ApplicationUserId == userToAdd.Id);

            if (userTrackerRelation != null)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, "User is already in tracker.");
            }

            ApplicationUserTracker newRelation = new()
            {
                ApplicationUserId = userToAdd.Id,
                TrackerId = trackerId,
            };

            await db.ApplicationUserTrackers.AddAsync(newRelation);
            await db.SaveChangesAsync();

            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse<List<PublicApplicationUserDto>>> GetApplicationUserTrackerList(string trackerId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers
              .Include(x => x.ApplicationUserTrackers)
              .FirstOrDefaultAsync(x => x.Id == trackerId);

            var hasAccess = tracker != null && (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (tracker == null || !hasAccess)
            {
                return ServiceResponse.Failure(StatusCodeEnum.Forbidden);
            }

            var appUserTrackerList = await db.ApplicationUserTrackers
                .Include(x => x.ApplicationUser)
                .Where(x => x.TrackerId == trackerId)
                .OrderBy(x => x.ApplicationUser.UserName)
                .Select(x => x.ApplicationUser)
                .ToListAsync();

            return ServiceResponse.Success(mapper.Map<List<ApplicationUser>, List<PublicApplicationUserDto>>(appUserTrackerList));
        }

        public async Task<ServiceResponse> RemoveUserFromTracker(string trackerId, ModifyUserTrackerDto addUserToTracker)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || !user.Owns(tracker))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound, "Tracker not found.");
            }

            var userToRemove = await db.ApplicationUsers.FirstOrDefaultAsync(x => x.UserName == addUserToTracker.Username);

            if (userToRemove == null)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound, "User not found");
            }

            var userTrackerRelation = await db.ApplicationUserTrackers.FirstOrDefaultAsync(x => x.TrackerId == trackerId && x.ApplicationUserId == userToRemove.Id);

            if (userTrackerRelation == null)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, "User is not in tracker.");
            }

            db.ApplicationUserTrackers.Remove(userTrackerRelation);
            await db.SaveChangesAsync();

            return ServiceResponse.Success();
        }
    }
}
