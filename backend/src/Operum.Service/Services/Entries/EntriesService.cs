using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Model.DTOs.Entry;
using Operum.Model.DTOs.Entry.Requests;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Helpers;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Services.Authorization;
using System.Globalization;

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

            var entryCount = await db.Entries.Where(x => x.TrackerId == trackerId).CountAsync();
            if (entryCount >= DataLimits.MaxEntryCount)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, $"Maximum number of entries {DataLimits.MaxEntryCount} reached.");
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

        public async Task<ServiceResponse<List<EntryDto>>> GetEntries(string trackerId, string? viewId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);
            if (tracker == null || !user.Owns(tracker))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            View? view = null;
            if (!string.IsNullOrEmpty(viewId))
            {
                view = await db.Views
                    .Include(v => v.Filters)
                    .ThenInclude(s => s.Field)
                    .Include(v => v.Sorts)
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
            if (view != null && view.Sorts.Count != 0)
            {
                entriesQuery = ViewHelpers.ApplyViewSorting(entriesQuery, view.Sorts);
            }

            var entries = await entriesQuery.ToListAsync();
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
                .FirstOrDefaultAsync(x => x.Id == entryId && x.TrackerId == trackerId);

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

            var allFields = await db.Fields
                .Where(f => f.TrackerId == trackerId)
                .AsNoTracking()
                .ToListAsync();

            var newFieldValues = new List<FieldValue>();

            foreach (var field in allFields)
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

        public async Task<ServiceResponse> DeleteEntry(string trackerId, string entryId)
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

        public async Task<ServiceResponse> DeleteEntries(string trackerId, List<string> entryIdList)
        {
            var user = authorizationService.GetCurrentUserDto();
            var entries = await db.Entries
                .Include(x => x.Tracker)
                .Where(x => entryIdList.Contains(x.Id) && x.TrackerId == trackerId && x.Tracker.OwnerId == user.Id)
                .ExecuteDeleteAsync();

            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse<List<EntryDto>>> ImportEntriesFromCsv(string trackerId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, "File is empty.");
            }

            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || !user.Owns(tracker))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            // Check entry count limit upfront
            var currentEntryCount = await db.Entries.Where(x => x.TrackerId == trackerId).CountAsync();

            using var stream = file.OpenReadStream();
            var fields = await db.Fields.Where(x => x.TrackerId == trackerId).ToListAsync();
            var fieldsByName = fields.ToDictionary(f => f.Name, f => f);
            var requiredFields = fields.Where(f => f.Required).ToList();

            // First pass: Parse and validate all records
            var parsedRecords = new List<Dictionary<string, string>>();
            var validationErrors = new List<string>();

            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<dynamic>();
                int rowIndex = 1; // Start from 1 for user-friendly error messages

                foreach (var record in records)
                {
                    var dict = (IDictionary<string, object>)record;
                    var parsedRecord = new Dictionary<string, string>();

                    // Validate required fields
                    var missingRequiredFields = requiredFields
                        .Where(f => !dict.ContainsKey(f.Name) || string.IsNullOrWhiteSpace(dict[f.Name]?.ToString()))
                        .Select(f => f.Name)
                        .ToList();

                    if (missingRequiredFields.Count > 0)
                    {
                        validationErrors.Add($"Row {rowIndex}: Missing required fields: {string.Join(", ", missingRequiredFields)}");
                        rowIndex++;
                        continue;
                    }

                    // Parse known fields only
                    foreach (var field in fields)
                    {
                        if (dict.ContainsKey(field.Name) && dict[field.Name] != null)
                        {
                            var value = dict[field.Name]?.ToString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                parsedRecord[field.Name] = value;
                            }
                        }
                    }

                    parsedRecords.Add(parsedRecord);
                    rowIndex++;
                }
            }

            // Return validation errors if any
            if (validationErrors.Count > 0)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest,
                    $"Validation errors:\n{string.Join("\n", validationErrors)}");
            }

            // Check if importing would exceed the limit
            if (currentEntryCount + parsedRecords.Count > DataLimits.MaxEntryCount)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest,
                    $"Import would exceed maximum entry limit. Current: {currentEntryCount}, " +
                    $"Import: {parsedRecords.Count}, Max: {DataLimits.MaxEntryCount}");
            }

            // Batch create entries and field values
            var newEntries = new List<Entry>();
            var allFieldValues = new List<FieldValue>();
            var createdAt = DateTime.UtcNow;

            foreach (var parsedRecord in parsedRecords)
            {
                var newEntry = new Entry
                {
                    TrackerId = trackerId,
                    CreatedAt = createdAt,
                };
                newEntries.Add(newEntry);

                // Create field values for this entry
                foreach (var kvp in parsedRecord)
                {
                    if (fieldsByName.TryGetValue(kvp.Key, out var field))
                    {
                        var fieldValue = new FieldValue
                        {
                            EntryId = newEntry.Id,
                            FieldId = field.Id,
                        };
                        fieldValue.SetFieldValue(field, kvp.Value);
                        allFieldValues.Add(fieldValue);
                    }
                }
            }

            // Single database transaction for all operations
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                // Add all entries in one batch
                await db.Entries.AddRangeAsync(newEntries);
                await db.SaveChangesAsync(); // This generates the Entry IDs

                // Add all field values in one batch
                await db.FieldValues.AddRangeAsync(allFieldValues);
                await db.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            return ServiceResponse.Success($"Entries imported successfully!");
        }
    }
}
