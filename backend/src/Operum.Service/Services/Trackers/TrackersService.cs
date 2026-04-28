using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.DTOs.Trackers;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.DTOs.Users;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Domain.Analytics;
using Operum.Service.Domain.Views;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Services.Trackers
{
    public class TrackersService(IAuthorizationService authorizationService, ICurrentUserService currentUserService, OperumContext db, IMapper mapper) : ITrackersService
    {
        public async Task<Result<TrackerDto>> CreateTracker(CreateTrackerDto tracker)
        {
            var user = currentUserService.GetCurrentUser();
            var trackerCount = await db.Trackers.Where(x => x.OwnerId == user.Id).CountAsync();
            if (trackerCount >= DataLimits.MaxTrackerCount)
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("trackers", DataLimits.MaxTrackerCount));
            }

            if (tracker.TrackerTypeId != null && !await authorizationService.HasRole(RoleNames.Admin))
            {
                return Result.Failure(ResultStatusCodes.Forbidden);
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
                    .Include(t => t.Analytics)
                        .ThenInclude(a => a.AnalyticFields)
                    .FirstOrDefaultAsync(t => t.Id == tracker.TemplateTrackerId);

                if (templateTracker == null || templateTracker.TrackerTypeId != (int)PublicityEnum.Public)
                {
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("template tracker"));
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

            // Dictionary to map old analytic IDs to new analytic IDs
            var analyticIdMapping = new Dictionary<string, string>();
            // Copy analytics
            foreach (var templateAnalytic in templateTracker.Analytics)
            {
                var newAnalytic = new Analytic
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = templateAnalytic.Name,
                    Description = templateAnalytic.Description,
                    Order = templateAnalytic.Order,
                    Code = templateAnalytic.Code,
                    ResultType = templateAnalytic.ResultType,
                    TrackerId = newTracker.Id
                };
                analyticIdMapping[templateAnalytic.Id] = newAnalytic.Id;
                await db.Analytics.AddAsync(newAnalytic);
            }
            // Save analytics first so they exist for analytic field references
            await db.SaveChangesAsync();

            // Copy analytic fields
            foreach (var templateAnalytic in templateTracker.Analytics)
            {
                foreach (var templateAnalyticField in templateAnalytic.AnalyticFields)
                {
                    // Only create analytic field if both the analytic and field were copied
                    if (analyticIdMapping.TryGetValue(templateAnalytic.Id, out var newAnalyticId) &&
                        fieldIdMapping.TryGetValue(templateAnalyticField.FieldId, out var newFieldId))
                    {
                        var newAnalyticField = new AnalyticField
                        {
                            Id = Guid.NewGuid().ToString(),
                            Purpose = templateAnalyticField.Purpose,
                            AnalyticId = newAnalyticId,
                            FieldId = newFieldId
                        };
                        await db.AnalyticFields.AddAsync(newAnalyticField);
                    }
                }
            }

            // Save all
            await db.SaveChangesAsync();
        }

        public async Task<Result> DeleteTracker(string id)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers.FindAsync(id);

            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            db.Trackers.Remove(tracker);
            await db.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result<TrackerDto>> GetTracker(string id)
        {
            var user = currentUserService.GetCurrentUser();

            var isAdmin = await authorizationService.HasRole(RoleNames.Admin);

            var tracker = await db.Trackers
                .Include(x => x.Fields)
                .Include(x => x.Owner)
                .Include(x => x.ApplicationUserTrackers)
                .Include(x => x.TrackerType)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (tracker == null)
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            bool hasAccess = tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id);

            if (isAdmin)
            {
                hasAccess = hasAccess || tracker.TrackerTypeId != null;
            }

            if (!hasAccess)
            {
                return Result.Failure(ResultStatusCodes.Forbidden);
            }

            var dto = mapper.Map<Tracker, TrackerDto>(tracker);
            if (tracker.OwnerId == user.Id)
            {
                dto.CurrentUserCanEditData = true;
                dto.CurrentUserCanEditSchema = true;
            }
            else
            {
                var ut = tracker.ApplicationUserTrackers.FirstOrDefault(x => x.ApplicationUserId == user.Id);
                dto.CurrentUserCanEditData = ut?.CanEditData ?? false;
                dto.CurrentUserCanEditSchema = ut?.CanEditSchema ?? false;
            }
            return Result.Success(dto);
        }

        public async Task<Result<List<TrackerDto>>> GetTrackerList(string filter)
        {
            var user = currentUserService.GetCurrentUser();

            if (filter == TrackerFilters.Owned)
            {
                var ownedTrackers = await db.Trackers
                .Include(x => x.Fields)
                .Include(x => x.ApplicationUserTrackers)
                .Include(x => x.Owner)
                .Where(x => x.TrackerTypeId == null && x.OwnerId == user.Id)
                .OrderBy(x => x.Order ?? int.MaxValue)
                .ToListAsync();
                return Result.Success(mapper.Map<List<Tracker>, List<TrackerDto>>(ownedTrackers));
            }
            else if (filter == TrackerFilters.Collaborating)
            {
                var trackers = await db.Trackers
                    .Include(x => x.Fields)
                    .Include(x => x.ApplicationUserTrackers)
                    .Include(x => x.Owner)
                    .Where(x => x.TrackerTypeId == null && x.OwnerId != user.Id && x.ApplicationUserTrackers.Any(a => a.ApplicationUserId == user.Id))
                    .ToListAsync();

                var ordered = trackers
                    .OrderBy(x =>
                    {
                        var ut = x.ApplicationUserTrackers.FirstOrDefault(a => a.ApplicationUserId == user.Id);
                        return ut?.Order ?? int.MaxValue;
                    })
                    .ToList();

                return Result.Success(mapper.Map<List<Tracker>, List<TrackerDto>>(ordered));
            }

            return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("filter"));
        }

        public async Task<Result<List<TrackerDto>>> GetAllTemplateTrackerList()
        {
            var trackers = await db.Trackers
                .Include(x => x.Fields)
                .Include(x => x.TrackerType)
                .Where(x => x.TrackerTypeId == (int)PublicityEnum.Public || x.TrackerTypeId == (int)PublicityEnum.Draft)
                .ToListAsync();
            return Result.Success(mapper.Map<List<Tracker>, List<TrackerDto>>(trackers));
        }

        public async Task<Result<List<TrackerDto>>> GetPublicTemplateTrackerList()
        {
            var trackers = await db.Trackers
                .Include(x => x.Fields)
                .Include(x => x.TrackerType)
                .Where(x => x.TrackerTypeId == (int)PublicityEnum.Public)
                .ToListAsync();
            return Result.Success(mapper.Map<List<Tracker>, List<TrackerDto>>(trackers));
        }

        public async Task<Result<TrackerDto>> UpdateTracker(string id, UpdateTrackerDto tracker)
        {
            var user = currentUserService.GetCurrentUser();
            var originalTracker = await db.Trackers.FindAsync(id);

            if (originalTracker?.OwnerId != user.Id)
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            if (tracker.TrackerTypeId != null && !await authorizationService.HasRole(RoleNames.Admin))
            {
                return Result.Failure(ResultStatusCodes.Forbidden);
            }

            mapper.Map(tracker, originalTracker);
            db.Trackers.Update(originalTracker);
            await db.SaveChangesAsync();

            var updatedTracker = await GetTracker(originalTracker.Id);
            return Result.Success(updatedTracker.Data);
        }

        public async Task<Result<List<AnalyticDto>>> GetTrackerAnalytics(string trackerId, List<string> viewIds)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                .Include(x => x.ApplicationUserTrackers)
                .FirstOrDefaultAsync(x => x.Id == trackerId);

            var hasAccess = tracker != null && (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (tracker == null || !hasAccess)
            {
                return Result.Failure(ResultStatusCodes.Forbidden);
            }

            var views = new List<View>();
            foreach (var viewId in viewIds)
            {
                var view = await db.Views
                    .Include(v => v.Filters)
                    .ThenInclude(v => v.Field)
                    .Include(v => v.Sorts)
                    .ThenInclude(s => s.Field)
                    .FirstOrDefaultAsync(v => v.Id == viewId && v.TrackerId == trackerId);

                if (view == null)
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));

                views.Add(view);
            }

            var entriesQuery = db.Entries
                .Include(x => x.FieldValues)
                .ThenInclude(x => x.Field)
                .Where(x => x.TrackerId == trackerId);

            if (views.Count > 0)
            {
                entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, ViewQueryBuilder.MergeViewFilters(views));
                entriesQuery = ViewQueryBuilder.ApplyViewSorting(entriesQuery, ViewQueryBuilder.MergeViewSorts(views));
            }

            var entries = await entriesQuery.ToListAsync();

            var analytics = await db.Analytics
                .Include(x => x.AnalyticFields)
                    .ThenInclude(x => x.Field)
                .Where(x => x.TrackerId == trackerId)
                .OrderBy(x => x.Order)
                .ToListAsync();

            List<AnalyticDto> analyticResults = [];

            foreach (var analytic in analytics)
            {
                var request = new AnalyticResultBuilderRequest()
                {
                    Analytic = analytic,
                    Entries = entries,
                    FieldMap = analytic.AnalyticFields.ToDictionary(f => f.Purpose, f => f.Field)
                };
                var calculationResult = AnalyticResultBuilder.GetAnalyticResult(request);
                if (calculationResult.IsSuccess)
                {
                    calculationResult.Data.Id = analytic.Id;
                    calculationResult.Data.Order = analytic.Order;
                    analyticResults.Add(calculationResult.Data);
                }
            }

            return Result.Success(analyticResults);
        }

        public async Task<Result> UpdateDefaultView(string trackerId, List<string>? viewIds)
        {
            var user = currentUserService.GetCurrentUser();

            var tracker = await db.Trackers
                .Include(t => t.Views)
                .FirstOrDefaultAsync(t => t.Id == trackerId);

            if (tracker == null || user.Id != tracker.OwnerId)
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            if (viewIds != null && viewIds.Count > 0)
            {
                var trackerViewIds = tracker.Views.Select(v => v.Id).ToHashSet();
                if (!viewIds.All(id => trackerViewIds.Contains(id)))
                {
                    return Result.Failure(ResultStatusCodes.BadRequest);
                }
            }

            tracker.DefaultViewIds = viewIds != null && viewIds.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(viewIds)
                : null;

            db.Update(tracker);
            await db.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> AddUserToTracker(string trackerId, AddUserToTrackerDto addUserToTracker)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || user.Id != tracker.OwnerId)
            {
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("tracker"));
            }

            var userToAdd = await db.Users.FirstOrDefaultAsync(x => x.UserName == addUserToTracker.Username);

            if (userToAdd == null)
            {
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("user"));
            }

            if (userToAdd.Id == user.Id)
            {
                return Result.Failure(ResultStatusCodes.BadRequest);
            }

            var userTrackerRelation = await db.UserTrackers.FirstOrDefaultAsync(x => x.TrackerId == trackerId && x.ApplicationUserId == userToAdd.Id);

            if (userTrackerRelation != null)
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.AlreadyInTracker);
            }

            UserTracker newRelation = new()
            {
                ApplicationUserId = userToAdd.Id,
                TrackerId = trackerId,
                CanEditData = addUserToTracker.CanEditData,
                CanEditSchema = addUserToTracker.CanEditSchema,
            };

            await db.UserTrackers.AddAsync(newRelation);
            await db.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<List<TrackerCollaboratorDto>>> GetApplicationUserTrackerList(string trackerId)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
              .Include(x => x.ApplicationUserTrackers)
              .FirstOrDefaultAsync(x => x.Id == trackerId);

            var hasAccess = tracker != null && tracker.OwnerId == user.Id;

            if (tracker == null || !hasAccess)
            {
                return Result.Failure(ResultStatusCodes.Forbidden);
            }

            var userTrackers = await db.UserTrackers
                .Include(x => x.ApplicationUser)
                .Where(x => x.TrackerId == trackerId)
                .OrderBy(x => x.ApplicationUser.UserName)
                .ToListAsync();

            var result = userTrackers.Select(ut => new TrackerCollaboratorDto
            {
                Id = ut.ApplicationUser.Id,
                UserName = ut.ApplicationUser.UserName,
                CanEditData = ut.CanEditData,
                CanEditSchema = ut.CanEditSchema,
            }).ToList();

            return Result.Success(result);
        }

        public async Task<Result> RemoveUserFromTracker(string trackerId, RemoveUserFromTrackerDto addUserToTracker)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || user.Id != tracker.OwnerId)
            {
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("tracker"));
            }

            var userToRemove = await db.Users.FirstOrDefaultAsync(x => x.UserName == addUserToTracker.Username);

            if (userToRemove == null)
            {
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("user"));
            }

            var userTrackerRelation = await db.UserTrackers.FirstOrDefaultAsync(x => x.TrackerId == trackerId && x.ApplicationUserId == userToRemove.Id);

            if (userTrackerRelation == null)
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.NotInTracker);
            }

            db.UserTrackers.Remove(userTrackerRelation);
            await db.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> UpdateCollaboratorPermissions(string trackerId, UpdateCollaboratorPermissionsDto dto)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || user.Id != tracker.OwnerId)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("tracker"));

            var targetUser = await db.Users.FirstOrDefaultAsync(x => x.UserName == dto.Username);
            if (targetUser == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("user"));

            var relation = await db.UserTrackers.FirstOrDefaultAsync(x => x.TrackerId == trackerId && x.ApplicationUserId == targetUser.Id);
            if (relation == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.NotInTracker);

            relation.CanEditData = dto.CanEditData;
            relation.CanEditSchema = dto.CanEditSchema;
            db.UserTrackers.Update(relation);
            await db.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> AddAnalytic(string trackerId, CreateAnalyticDto addAnalytic)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);
            var isOwner = tracker?.OwnerId == user.Id;
            var userTracker = tracker?.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (tracker == null || (!isOwner && userTracker?.CanEditSchema != true))
            {
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("tracker"));
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
                    return Result.Failure(ResultStatusCodes.NotFound,
                        Messages.Required($"field for purpose {field.Purpose}"));
                }
                if (!AnalyticDefinitionList.IsValidDataType(addAnalytic.Type, addAnalytic.Code, field.Purpose, existingField.Type))
                {
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("data type for purpose"));
                }
            }

            var count = await db.Analytics.Where(x => x.TrackerId == trackerId).CountAsync();

            if (count >= DataLimits.MaxAnalyticCount)
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("analytics", DataLimits.MaxAnalyticCount));
            }

            Analytic analytic = new()
            {
                TrackerId = tracker.Id,
                Code = addAnalytic.Code,
                ResultType = addAnalytic.Type,
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
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);
            var isOwner = tracker?.OwnerId == user.Id;
            var userTracker = tracker?.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (tracker == null || (!isOwner && userTracker?.CanEditSchema != true))
            {
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("tracker"));
            }

            var analytic = await db.Analytics.FindAsync(trackerAnalyticId);
            if (analytic == null || analytic.TrackerId != trackerId)
            {
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("analytic"));
            }

            db.Analytics.Remove(analytic);
            await db.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> ReorderAnalytics(string trackerId, ReorderAnalyticsDto reorderAnalyticsDto)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);
            var isOwner = tracker?.OwnerId == user.Id;
            var userTracker = tracker?.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);

            if (tracker == null || (!isOwner && userTracker?.CanEditSchema != true))
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            var existingTrackerAnalytics = await db.Analytics
                .Where(x => x.TrackerId == trackerId)
                .Select(x => x.Id)
                .ToListAsync();

            var requestedTrackerAnalyticIds = reorderAnalyticsDto.AnalyticIds.ToHashSet();
            var existingTrackerAnalyticIds = existingTrackerAnalytics.ToHashSet();

            if (!requestedTrackerAnalyticIds.SetEquals(existingTrackerAnalyticIds))
            {
                return Result.Failure(ResultStatusCodes.BadRequest);
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
                return Result.Failure(ResultStatusCodes.Error);
            }
        }

        public async Task<Result> ReorderTrackers(ReorderTrackersDto dto)
        {
            var user = currentUserService.GetCurrentUser();

            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                if (dto.Filter == TrackerFilters.Owned)
                {
                    var existingIds = await db.Trackers
                        .Where(x => x.OwnerId == user.Id && x.TrackerTypeId == null)
                        .Select(x => x.Id)
                        .ToHashSetAsync();

                    if (!dto.TrackerIds.ToHashSet().SetEquals(existingIds))
                        return Result.Failure(ResultStatusCodes.BadRequest);

                    for (int i = 0; i < dto.TrackerIds.Count; i++)
                    {
                        var tracker = await db.Trackers.FindAsync(dto.TrackerIds[i]);
                        if (tracker != null && tracker.OwnerId == user.Id)
                        {
                            tracker.Order = i + 1;
                            db.Trackers.Update(tracker);
                        }
                    }
                }
                else if (dto.Filter == TrackerFilters.Collaborating)
                {
                    var existingIds = await db.UserTrackers
                        .Where(x => x.ApplicationUserId == user.Id)
                        .Select(x => x.TrackerId)
                        .ToHashSetAsync();

                    if (!dto.TrackerIds.ToHashSet().SetEquals(existingIds))
                        return Result.Failure(ResultStatusCodes.BadRequest);

                    for (int i = 0; i < dto.TrackerIds.Count; i++)
                    {
                        var ut = await db.UserTrackers.FirstOrDefaultAsync(x => x.TrackerId == dto.TrackerIds[i] && x.ApplicationUserId == user.Id);
                        if (ut != null)
                        {
                            ut.Order = i + 1;
                            db.UserTrackers.Update(ut);
                        }
                    }
                }
                else
                {
                    return Result.Failure(ResultStatusCodes.BadRequest);
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Result.Success();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return Result.Failure(ResultStatusCodes.Error);
            }
        }
    }
}
