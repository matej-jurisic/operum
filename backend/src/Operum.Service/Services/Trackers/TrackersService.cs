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
            if (await IsAtTrackerLimit(user.Id))
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
                    .AsSplitQuery()
                    .Include(t => t.Fields)
                    .Include(t => t.TrackerConstants)
                        .ThenInclude(c => c.Values)
                            .ThenInclude(v => v.Filters)
                    .Include(t => t.Views)
                        .ThenInclude(v => v.ViewColumns)
                    .Include(t => t.Views)
                        .ThenInclude(v => v.ViewQueries.OrderBy(vq => vq.Order))
                            .ThenInclude(vq => vq.Query)
                    .FirstOrDefaultAsync(t => t.Id == tracker.TemplateTrackerId);

                if (templateTracker == null || templateTracker.TrackerTypeId != (int)PublicityEnum.Public)
                {
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("template tracker"));
                }
            }

            var trackerModel = mapper.Map<CreateTrackerDto, Tracker>(tracker);
            trackerModel.OwnerId = user.Id;
            trackerModel.Color = trackerModel.Color?.ToLower();

            await db.Trackers.AddAsync(trackerModel);
            await db.SaveChangesAsync();

            if (templateTracker != null)
            {
                await CopyTrackerSchema(templateTracker, trackerModel, keepExternalReferences: false);
            }

            var created = await GetTracker(trackerModel.Id);
            return Result.Success(created.Data);
        }

        public async Task<Result<TrackerDto>> CopyTracker(string trackerId)
        {
            var user = currentUserService.GetCurrentUser();

            var source = await db.Trackers
                .AsSplitQuery()
                .Include(t => t.Fields)
                .Include(t => t.TrackerConstants)
                    .ThenInclude(c => c.Values)
                        .ThenInclude(v => v.Filters)
                .Include(t => t.Views)
                    .ThenInclude(v => v.ViewColumns)
                .Include(t => t.Views)
                    .ThenInclude(v => v.ViewQueries)
                        .ThenInclude(vq => vq.Query)
                .FirstOrDefaultAsync(t => t.Id == trackerId);

            if (source == null || source.OwnerId != user.Id || source.TrackerTypeId != null)
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            if (await IsAtTrackerLimit(user.Id))
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("trackers", DataLimits.MaxTrackerCount));
            }

            const string suffix = " (copy)";
            const int maxNameLength = 100;
            var baseName = source.Name.Length + suffix.Length > maxNameLength
                ? source.Name[..(maxNameLength - suffix.Length)]
                : source.Name;

            var copy = new Tracker
            {
                Name = baseName + suffix,
                Description = source.Description,
                Color = source.Color,
                Icon = source.Icon,
                OwnerId = user.Id,
            };

            await db.Trackers.AddAsync(copy);
            await db.SaveChangesAsync();

            await CopyTrackerSchema(source, copy, keepExternalReferences: true);

            var created = await GetTracker(copy.Id);
            return Result.Success(created.Data);
        }

        // Templates (non-null TrackerTypeId) don't count against a user's personal cap, the
        // same exclusion the list queries make.
        private async Task<bool> IsAtTrackerLimit(string userId) =>
            await db.Trackers.CountAsync(x => x.OwnerId == userId && x.TrackerTypeId == null) >= DataLimits.MaxTrackerCount;

        // Copies fields, constants and views (with their filters, sorts and columns) but no
        // entries. External reference targets are kept only when keepExternalReferences is set, so
        // a public template never points a new tracker at trackers it didn't come with.
        private async Task CopyTrackerSchema(Tracker source, Tracker target, bool keepExternalReferences)
        {
            var constantIdMapping = new Dictionary<string, string>();
            foreach (var constant in source.TrackerConstants)
            {
                var newConstant = new TrackerConstant
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = constant.Name,
                    Type = constant.Type,
                    Value = constant.Value,
                    TrackerId = target.Id,
                };
                constantIdMapping[constant.Id] = newConstant.Id;
                await db.TrackerConstants.AddAsync(newConstant);
            }

            var fieldIdMapping = new Dictionary<string, string>();
            var newFields = new Dictionary<string, Field>();
            foreach (var sourceField in source.Fields)
            {
                var referencesSelf = sourceField.ReferencedTrackerId == source.Id;
                string? newDefaultConstantId = null;
                if (sourceField.DefaultValueConstantId != null)
                    constantIdMapping.TryGetValue(sourceField.DefaultValueConstantId, out newDefaultConstantId);

                var newField = new Field
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = sourceField.Name,
                    Description = sourceField.Description,
                    Type = sourceField.Type,
                    Required = sourceField.Required,
                    Visible = sourceField.Visible,
                    Order = sourceField.Order,
                    SelectOptions = sourceField.SelectOptions,
                    IsCalculated = sourceField.IsCalculated,
                    Formula = sourceField.Formula,
                    ReferencedTrackerId = referencesSelf
                        ? target.Id
                        : keepExternalReferences ? sourceField.ReferencedTrackerId : null,
                    DefaultValue = sourceField.DefaultValue,
                    DefaultValueConstantId = newDefaultConstantId,
                    TrackerId = target.Id,
                };
                fieldIdMapping[sourceField.Id] = newField.Id;
                newFields[sourceField.Id] = newField;
                await db.Fields.AddAsync(newField);
            }

            foreach (var constant in source.TrackerConstants)
            {
                foreach (var value in constant.Values)
                {
                    var newValue = new TrackerConstantValue
                    {
                        Id = Guid.NewGuid().ToString(),
                        TrackerConstantId = constantIdMapping[constant.Id],
                        Priority = value.Priority,
                        Value = value.Value,
                    };
                    await db.TrackerConstantValues.AddAsync(newValue);

                    foreach (var filter in value.Filters)
                    {
                        if (!fieldIdMapping.TryGetValue(filter.FieldId, out var newFilterFieldId))
                            continue;

                        await db.TrackerConstantValueFilters.AddAsync(new TrackerConstantValueFilter
                        {
                            Id = Guid.NewGuid().ToString(),
                            TrackerConstantValueId = newValue.Id,
                            FieldId = newFilterFieldId,
                            Operator = filter.Operator,
                            Value = filter.Value,
                        });
                    }
                }
            }

            // Save fields first so they exist for the field-to-field links, views and queries below.
            await db.SaveChangesAsync();

            foreach (var sourceField in source.Fields)
            {
                var newField = newFields[sourceField.Id];

                if (sourceField.VisibilityFieldId != null
                    && fieldIdMapping.TryGetValue(sourceField.VisibilityFieldId, out var newVisibilityFieldId))
                {
                    newField.VisibilityFieldId = newVisibilityFieldId;
                    newField.VisibilityOperator = sourceField.VisibilityOperator;
                    newField.VisibilityValue = sourceField.VisibilityValue;
                }

                if (sourceField.ReferencedDisplayFieldId != null)
                {
                    if (sourceField.ReferencedTrackerId == source.Id)
                    {
                        newField.ReferencedDisplayFieldId = fieldIdMapping.GetValueOrDefault(sourceField.ReferencedDisplayFieldId);
                    }
                    else if (keepExternalReferences)
                    {
                        newField.ReferencedDisplayFieldId = sourceField.ReferencedDisplayFieldId;
                    }
                }
            }
            await db.SaveChangesAsync();

            var viewIdMapping = new Dictionary<string, string>();
            // Copy view shells only; their Queries are copied and linked below.
            foreach (var sourceView in source.Views)
            {
                var newView = new View
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = sourceView.Name,
                    Description = sourceView.Description,
                    Order = sourceView.Order,
                    TrackerId = target.Id
                };
                viewIdMapping[sourceView.Id] = newView.Id;
                await db.Views.AddAsync(newView);
            }

            // Save the view shells first so they exist for the link references below.
            await db.SaveChangesAsync();

            // Re-link each source ViewQuery: its clause is field-agnostic and pooled under the
            // new tracker's owner, then bound to the copied field. A link whose field was not
            // copied is dropped whole.
            foreach (var sourceView in source.Views)
            {
                if (!viewIdMapping.TryGetValue(sourceView.Id, out var newViewId))
                    continue;

                foreach (var sourceViewQuery in sourceView.ViewQueries)
                {
                    if (!fieldIdMapping.TryGetValue(sourceViewQuery.FieldId, out var newFieldId))
                        continue;

                    var sq = sourceViewQuery.Query;
                    var pooled = await QueryPool.GetOrCreate(db, target.OwnerId, new ClauseDto
                    {
                        Kind = sq.Kind,
                        DataType = sq.DataType,
                        Operator = sq.Operator,
                        Value = sq.Value,
                        Descending = sq.Descending,
                    });

                    await db.ViewQueries.AddAsync(new ViewQuery
                    {
                        Id = Guid.NewGuid().ToString(),
                        ViewId = newViewId,
                        QueryId = pooled.Id,
                        FieldId = newFieldId,
                        Order = sourceViewQuery.Order
                    });
                }

                foreach (var sourceColumn in sourceView.ViewColumns)
                {
                    if (!fieldIdMapping.TryGetValue(sourceColumn.FieldId, out var newColumnFieldId))
                        continue;

                    await db.ViewColumns.AddAsync(new ViewColumn
                    {
                        Id = Guid.NewGuid().ToString(),
                        ViewId = newViewId,
                        FieldId = newColumnFieldId,
                        Order = sourceColumn.Order
                    });
                }
            }

            if (source.DefaultViewId != null && viewIdMapping.TryGetValue(source.DefaultViewId, out var newDefaultViewId))
            {
                target.DefaultViewId = newDefaultViewId;
            }

            // Widgets aren't tracker-owned any more, so the copy simply starts with none, the
            // same as a tracker created from scratch.
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

                // Owned trackers sort by Tracker.Order, shared ones by this user's UserTracker.Order
                // -- the same keys the two single-filter lists use.
                var ordered = trackers
                    .OrderBy(x => AccessibleOrderKey(x, user.Id))
                    .ThenBy(x => x.Name)
                    .ToList();

                return Result.Success(await WithEntryStats(mapper.Map<List<Tracker>, List<TrackerDto>>(ordered)));
            }

            return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("filter"));
        }

        // Sort key for the "Accessible" list: the owner's Tracker.Order when this user owns it,
        // otherwise their own UserTracker.Order. Missing orders sink to the bottom.
        private static int AccessibleOrderKey(Tracker tracker, string userId)
        {
            if (tracker.OwnerId == userId)
                return tracker.Order ?? int.MaxValue;

            var ut = tracker.ApplicationUserTrackers.FirstOrDefault(a => a.ApplicationUserId == userId);
            return ut?.Order ?? int.MaxValue;
        }

        // Fills EntryCount / LastEntryAt for a page of trackers in a single grouped query rather
        // than one round-trip per card.
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

        public async Task<Result<List<TrackerSchemaDto>>> GetTrackerSchema()
        {
            var user = currentUserService.GetCurrentUser();

            var trackers = await db.Trackers
                .AsSplitQuery()
                .Include(x => x.Fields).ThenInclude(f => f.ReferencedTracker)
                .Include(x => x.Views).ThenInclude(v => v.ViewQueries).ThenInclude(q => q.Query)
                .Include(x => x.Views).ThenInclude(v => v.ViewQueries).ThenInclude(q => q.Field)
                .Include(x => x.Views).ThenInclude(v => v.ViewColumns).ThenInclude(c => c.Field)
                .Where(x => x.TrackerTypeId == null &&
                    (x.OwnerId == user.Id || x.ApplicationUserTrackers.Any(a => a.ApplicationUserId == user.Id)))
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Result.Success(trackers.Select(tracker => new TrackerSchemaDto
            {
                Name = tracker.Name,
                Description = tracker.Description,
                Fields = [.. tracker.Fields.OrderBy(f => f.Order).Select(field => new TrackerSchemaFieldDto
                {
                    Name = field.Name,
                    Type = field.Type,
                    Required = field.Required,
                    SelectOptions = field.SelectOptions != null
                        ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(field.SelectOptions)
                        : null,
                    Formula = field.IsCalculated ? field.Formula : null,
                    References = field.ReferencedTracker?.Name
                })],
                Views = [.. tracker.Views.OrderBy(v => v.Order).Select(view => new TrackerSchemaViewDto
                {
                    Name = view.Name,
                    Description = view.Description,
                    Columns = [.. view.ViewColumns.OrderBy(c => c.Order).Select(c => c.Field.Name)],
                    Filters = [.. view.ViewQueries
                        .Where(q => q.Query.Kind == QueryKinds.Filter)
                        .OrderBy(q => q.Order)
                        .Select(q => new TrackerSchemaFilterDto
                        {
                            Field = q.Field.Name,
                            Operator = q.Query.Operator,
                            Value = q.Query.Value
                        })],
                    Sorts = [.. view.ViewQueries
                        .Where(q => q.Query.Kind == QueryKinds.Sort)
                        .OrderBy(q => q.Order)
                        .Select(q => new TrackerSchemaSortDto
                        {
                            Field = q.Field.Name,
                            Descending = q.Query.Descending
                        })]
                })]
            }).ToList());
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
                    // whichever order it has -- Tracker.Order for owned, UserTracker.Order for shared.
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
