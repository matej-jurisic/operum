using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.DTOs.Entry;
using Operum.Model.DTOs.Entry.Requests;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Helpers;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Services.Authorization;

namespace Operum.Service.Services.Entries
{
    public class EntriesService(IAuthorizationService authorizationService, OperumContext db, IMapper mapper) : IEntriesService
    {
        public async Task<ServiceResponse<EntryDto>> CreateEntry(string trackerId, CreateEntryDto entry)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);
            if (tracker == null || !user.Owns(tracker))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            var fields = await db.Fields.Where(x => x.TrackerId == trackerId).ToListAsync();

            Entry newEntry = new()
            {
                TrackerId = trackerId,
                CreatedAt = DateTime.UtcNow,
            };
            await db.Entries.AddAsync(newEntry);

            var entryFieldValues = new List<FieldValue>();
            var fieldDict = entry.FieldValues;

            foreach (var field in fields)
            {
                if (fieldDict.TryGetValue(field.Name, out string? value))
                {
                    FieldValue fieldValue = new()
                    {
                        EntryId = newEntry.Id,
                        FieldId = field.Id,
                    };
                    fieldValue.SetFieldValue(field, value);
                    entryFieldValues.Add(fieldValue);
                }
                else if (field.Required)
                {
                    return ServiceResponse.Failure(StatusCodeEnum.BadRequest, $"Field {field.Name} is required!");
                }
            }

            await db.FieldValues.AddRangeAsync(entryFieldValues);
            await db.SaveChangesAsync();

            var created = await GetEntry(trackerId, newEntry.Id);

            return ServiceResponse.Success(created.Data, "Entry created successfully!");
        }

        public async Task<ServiceResponse<List<EntryDto>>> GetEntries(string trackerId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);
            if (tracker == null || !user.Owns(tracker))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            var entries = await db.Entries
                .Include(x => x.FieldValues)
                .ThenInclude(x => x.Field)
                .Where(x => x.TrackerId == trackerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return ServiceResponse.Success(mapper.Map<List<Entry>, List<EntryDto>>(entries));
        }

        public async Task<ServiceResponse<EntryDto>> GetEntry(string trackerId, string entryId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);
            if (tracker == null || !user.Owns(tracker))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            var entry = await db.Entries
                .Include(x => x.FieldValues)
                .ThenInclude(x => x.Field)
                .FirstOrDefaultAsync(x => x.Id == entryId && x.TrackerId == trackerId);

            if (entry == null)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            return ServiceResponse.Success(mapper.Map<Entry, EntryDto>(entry));
        }

        public async Task<ServiceResponse<EntryDto>> UpdateEntry(string trackerId, string entryId, UpdateEntryDto updateEntry)
        {
            var user = authorizationService.GetCurrentUserDto();
            var entry = await db.Entries
                .Include(x => x.Tracker)
                .FirstOrDefaultAsync(x => x.Id == entryId);

            if (entry == null || !user.Owns(entry))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            var fieldValues = await db.FieldValues
                .Include(x => x.Field)
                .Where(x => x.EntryId == entryId)
                .AsTracking()
                .ToListAsync();

            var fieldValuesDict = fieldValues.ToDictionary(x => x.FieldId);

            var allTrackerFields = await db.Fields
                .Where(f => f.TrackerId == trackerId)
                .AsNoTracking()
                .ToListAsync();

            var newFieldValues = new List<FieldValue>();

            foreach (var field in allTrackerFields)
            {
                fieldValuesDict.TryGetValue(field.Id, out var existingFieldValue);
                bool hasNewValue = updateEntry.FieldValues.TryGetValue(field.Name, out string? newValue);

                if (hasNewValue)
                {
                    if (existingFieldValue != null)
                    {
                        existingFieldValue.SetFieldValue(field, newValue);
                    }
                    else
                    {
                        var newFieldValue = new FieldValue
                        {
                            EntryId = entryId,
                            FieldId = field.Id,
                        };
                        newFieldValue.SetFieldValue(field, newValue);
                        newFieldValues.Add(newFieldValue);
                    }
                }
                else if (existingFieldValue != null)
                {
                    db.FieldValues.Remove(existingFieldValue);
                }
            }

            await db.FieldValues.AddRangeAsync(newFieldValues);
            await db.SaveChangesAsync();
            var updatedEntry = await GetEntry(trackerId, entryId);
            return ServiceResponse.Success(updatedEntry.Data);
        }

        public async Task<ServiceResponse<EntryDto>> DeleteEntry(string trackerId, string entryId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var entry = await db.Entries
                .Include(x => x.Tracker)
                .FirstOrDefaultAsync(x => x.Id == entryId && x.TrackerId == trackerId);

            if (entry == null || !user.Owns(entry))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            db.Entries.Remove(entry);
            await db.SaveChangesAsync();

            return ServiceResponse.Success();
        }
    }
}
