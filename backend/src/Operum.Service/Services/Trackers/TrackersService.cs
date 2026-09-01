using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.DTOs.Queries;
using Operum.Model.DTOs.Trackers;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.DTOs.Users;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Domain.Analytics;
using Operum.Service.Domain.Queries;
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
                        .ThenInclude(v => v.ViewQueries.OrderBy(vq => vq.Order))
                            .ThenInclude(vq => vq.Query)
                    .FirstOrDefaultAsync(t => t.Id == tracker.TemplateTrackerId);
                // ViewQuery.FieldId is a plain column read directly below, so it needs no Include.

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
            // Save fields first so they exist for view/query references
            await db.SaveChangesAsync();
            // Dictionary to map old view IDs to new view IDs
            var viewIdMapping = new Dictionary<string, string>();
            // Copy views (shells only — their Queries are copied and linked below)
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

            // Save the view shells first so they exist for the link references below.
            await db.SaveChangesAsync();

            // Re-link each template ViewQuery: its clause is field-agnostic and pooled under
            // the new tracker's owner, and it is bound to the copied field. A link whose
            // field was not copied is dropped whole.
            foreach (var templateView in templateTracker.Views)
            {
                if (!viewIdMapping.TryGetValue(templateView.Id, out var newViewId))
                    continue;

                foreach (var templateViewQuery in templateView.ViewQueries)
                {
                    if (!fieldIdMapping.TryGetValue(templateViewQuery.FieldId, out var newFieldId))
                        continue;

                    var tq = templateViewQuery.Query;
                    var pooled = await QueryPool.GetOrCreate(db, newTracker.OwnerId, new ClauseDto
                    {
                        Kind = tq.Kind,
                        DataType = tq.DataType,
                        Operator = tq.Operator,
                        Value = tq.Value,
                        Descending = tq.Descending,
                    });

                    await db.ViewQueries.AddAsync(new ViewQuery
                    {
                        Id = Guid.NewGuid().ToString(),
                        ViewId = newViewId,
                        QueryId = pooled.Id,
                        FieldId = newFieldId,
                        Order = templateViewQuery.Order
                    });
                }
            }

            // Widgets aren't tracker-owned any more, so there's nothing analogous to copy
            // here -- a tracker created from a template simply starts with none, the same
            // as one created from scratch.
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
                return Result.Success(await WithEntryStats(mapper.Map<List<Tracker>, List<TrackerDto>>(ownedTrackers)));
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

                return Result.Success(await WithEntryStats(mapper.Map<List<Tracker>, List<TrackerDto>>(ordered)));
            }

            else if (filter == TrackerFilters.Accessible)
            {
                var trackers = await db.Trackers
                    .Include(x => x.Fields)
                    .Include(x => x.ApplicationUserTrackers)
                    .Include(x => x.Owner)
                    .Where(x => x.TrackerTypeId == null &&
                        (x.OwnerId == user.Id || x.ApplicationUserTrackers.Any(a => a.ApplicationUserId == user.Id)))
                    .ToListAsync();

                // Owned trackers sort by Tracker.Order, shared ones by this user's
                // UserTracker.Order -- the same keys the two single-filter lists use.
                var ordered = trackers
                    .OrderBy(x => AccessibleOrderKey(x, user.Id))
                    .ThenBy(x => x.Name)
                    .ToList();

                return Result.Success(await WithEntryStats(mapper.Map<List<Tracker>, List<TrackerDto>>(ordered)));
            }

            return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("filter"));
        }

        // Sort key for the "Accessible" list: the owner's Tracker.Order when this user owns
        // it, otherwise the user's own UserTracker.Order. Missing orders sink to the bottom.
        private static int AccessibleOrderKey(Tracker tracker, string userId)
        {
            if (tracker.OwnerId == userId)
                return tracker.Order ?? int.MaxValue;

            var ut = tracker.ApplicationUserTrackers.FirstOrDefault(a => a.ApplicationUserId == userId);
            return ut?.Order ?? int.MaxValue;
        }

        // Fills EntryCount / LastEntryAt for a page of trackers in a single grouped query
        // rather than one round-trip per card.
        private async Task<List<TrackerDto>> WithEntryStats(List<TrackerDto> dtos)
        {
            if (dtos.Count == 0) return dtos;

            var trackerIds = dtos.Select(d => d.Id).ToList();
            var stats = await db.Entries
                .Where(e => trackerIds.Contains(e.TrackerId))
                .GroupBy(e => e.TrackerId)
                .Select(g => new
                {
                    TrackerId = g.Key,
                    Count = g.Count(),
                    LastEntryAt = (DateTime?)g.Max(e => e.CreatedAt),
                })
                .ToDictionaryAsync(x => x.TrackerId);

            foreach (var dto in dtos)
            {
                if (stats.TryGetValue(dto.Id, out var s))
                {
                    dto.EntryCount = s.Count;
                    dto.LastEntryAt = s.LastEntryAt;
                }
            }

            return dtos;
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

        public async Task<Result> UpdateDefaultView(string trackerId, string? viewId)
        {
            var user = currentUserService.GetCurrentUser();

            var tracker = await db.Trackers
                .Include(t => t.Views)
                .FirstOrDefaultAsync(t => t.Id == trackerId);

            if (tracker == null || user.Id != tracker.OwnerId)
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            if (!string.IsNullOrEmpty(viewId) && tracker.Views.All(v => v.Id != viewId))
            {
                return Result.Failure(ResultStatusCodes.BadRequest);
            }

            tracker.DefaultViewId = viewId;

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
                else if (dto.Filter == TrackerFilters.Accessible)
                {
                    // The sidebar list: a mix of owned and shared trackers. Each id updates
                    // whichever order it has -- Tracker.Order for owned, UserTracker.Order
                    // for shared.
                    var accessible = await db.Trackers
                        .Include(x => x.ApplicationUserTrackers)
                        .Where(x => x.TrackerTypeId == null &&
                            (x.OwnerId == user.Id || x.ApplicationUserTrackers.Any(a => a.ApplicationUserId == user.Id)))
                        .ToListAsync();

                    if (!dto.TrackerIds.ToHashSet().SetEquals(accessible.Select(x => x.Id).ToHashSet()))
                        return Result.Failure(ResultStatusCodes.BadRequest);

                    var byId = accessible.ToDictionary(x => x.Id);
                    for (int i = 0; i < dto.TrackerIds.Count; i++)
                    {
                        var tracker = byId[dto.TrackerIds[i]];
                        if (tracker.OwnerId == user.Id)
                        {
                            tracker.Order = i + 1;
                            db.Trackers.Update(tracker);
                        }
                        else
                        {
                            var ut = tracker.ApplicationUserTrackers.FirstOrDefault(a => a.ApplicationUserId == user.Id);
                            if (ut != null)
                            {
                                ut.Order = i + 1;
                                db.UserTrackers.Update(ut);
                            }
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
