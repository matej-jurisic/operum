using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Entries;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Domain.Views;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;
using System.Globalization;

namespace Operum.Service.Services.Entries
{
    public class EntriesService(ICurrentUserService currentUserService, IAuthorizationService authorizationService, OperumContext db, IMapper mapper, ILogger<EntriesService> logger) : IEntriesService
    {
        public async Task<Result<EntryDto>> CreateEntry(string trackerId, CreateEntryDto entry)
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

            var entryCount = await db.Entries.Where(x => x.TrackerId == trackerId).CountAsync();
            if (entryCount >= DataLimits.MaxEntryCount)
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("trackers", DataLimits.MaxTrackerCount));
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
                if (fieldDict.TryGetValue(field.Name, out string? value) && !(field.Required && string.IsNullOrEmpty(value)))
                {
                    FieldValue fieldValue = new()
                    {
                        EntryId = newEntry.Id,
                        FieldId = field.Id,
                    };
                    fieldValue.SetFieldValue(field, value);
                    entryFieldValues.Add(fieldValue);
                }
                else
                {
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Required(field.Name));
                }
            }

            await db.FieldValues.AddRangeAsync(entryFieldValues);
            await db.SaveChangesAsync();

            var created = await GetEntry(trackerId, newEntry.Id);

            return Result.Success(created.Data, Messages.Success);
        }

        public async Task<Result<List<EntryDto>>> GetEntries(string trackerId, string? viewId)
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
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));
                }
            }

            var entriesQuery = db.Entries
                .Include(x => x.FieldValues)
                .ThenInclude(x => x.Field)
                .Where(x => x.TrackerId == trackerId);

            if (view != null)
            {
                entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, view.Filters);
                entriesQuery = ViewQueryBuilder.ApplyViewSorting(entriesQuery, view.Sorts);
            }

            var entries = await entriesQuery.ToListAsync();
            return Result.Success(mapper.Map<List<Entry>, List<EntryDto>>(entries));
        }

        public async Task<Result<EntryDto>> GetEntry(string trackerId, string entryId)
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

            var entry = await db.Entries
                .Include(x => x.FieldValues)
                .ThenInclude(x => x.Field)
                .FirstOrDefaultAsync(x => x.Id == entryId && x.TrackerId == trackerId);

            if (entry == null)
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            return Result.Success(mapper.Map<Entry, EntryDto>(entry));
        }

        public async Task<Result<EntryDto>> UpdateEntry(string trackerId, string entryId, UpdateEntryDto updateEntry)
        {
            var user = currentUserService.GetCurrentUser();
            var entry = await db.Entries
                .Include(x => x.Tracker)
                    .ThenInclude(x => x.ApplicationUserTrackers)
                .FirstOrDefaultAsync(x => x.Id == entryId && x.TrackerId == trackerId);

            var hasAccess = entry?.Tracker != null && (entry.Tracker.OwnerId == user.Id || entry.Tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (entry == null || !hasAccess)
            {
                return Result.Failure(ResultStatusCodes.Forbidden);
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
            return Result.Success(updatedEntry.Data);
        }

        public async Task<Result> DeleteEntry(string trackerId, string entryId)
        {
            var user = currentUserService.GetCurrentUser();
            var entry = await db.Entries
                .Include(x => x.Tracker)
                    .ThenInclude(x => x.ApplicationUserTrackers)
                .FirstOrDefaultAsync(x => x.Id == entryId && x.TrackerId == trackerId);

            var hasAccess = entry?.Tracker != null && (entry.Tracker.OwnerId == user.Id || entry.Tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (entry == null || !hasAccess)
            {
                return Result.Failure(ResultStatusCodes.Forbidden);
            }

            db.Entries.Remove(entry);
            await db.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> DeleteEntries(string trackerId, List<string> entryIdList)
        {
            var user = currentUserService.GetCurrentUser();
            var entries = await db.Entries
                .Include(x => x.Tracker)
                    .ThenInclude(x => x.ApplicationUserTrackers)
                .Where(x => entryIdList.Contains(x.Id) && x.TrackerId == trackerId && (x.Tracker.OwnerId == user.Id || x.Tracker.ApplicationUserTrackers.Any(a => a.ApplicationUserId == user.Id)))
                .ExecuteDeleteAsync();

            return Result.Success();
        }

        public async Task<Result<List<EntryDto>>> ImportEntriesFromCsv(string trackerId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.FielIsEmpty);
            }

            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                 .Include(x => x.ApplicationUserTrackers)
                 .FirstOrDefaultAsync(x => x.Id == trackerId);

            var hasAccess = tracker != null && (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (tracker == null || !hasAccess)
            {
                return Result.Failure(ResultStatusCodes.Forbidden);
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

            using var reader = new StreamReader(stream);

            var headerLine = await reader.ReadLineAsync();
            if (headerLine == null)
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.FielIsEmpty);
            }

            var delimiter = headerLine.Contains(';') ? ";" : ",";
            stream.Position = 0;
            reader.DiscardBufferedData();

            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = true,
                BadDataFound = null,
                MissingFieldFound = null,
            };

            using (var csv = new CsvReader(reader, csvConfig))
            {
                var records = csv.GetRecords<dynamic>();
                int rowIndex = 1;

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
                        validationErrors.Add(Messages.CsvMissingFields(rowIndex, missingRequiredFields));
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
                return Result.Failure(ResultStatusCodes.BadRequest,
                    validationErrors.Take(5));
            }

            var isAdmin = await authorizationService.HasRole(RoleNames.Admin);

            // Check if importing would exceed the limit for non admin users
            if (!isAdmin && (currentEntryCount + parsedRecords.Count > DataLimits.MaxEntryCount))
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.CsvMaxNumberReached(currentEntryCount, parsedRecords.Count, DataLimits.MaxEntryCount));
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

            return Result.Success(Messages.Success);
        }

        public async Task<Result<FileContentResult>> ExportEntriesToCsv(string trackerId, string? viewId = null)
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

            // Fetch view if provided
            View? view = null;
            if (!string.IsNullOrEmpty(viewId))
            {
                view = await db.Views
                    .Include(v => v.Filters)
                    .ThenInclude(f => f.Field)
                    .Include(v => v.Sorts)
                    .ThenInclude(s => s.Field)
                    .FirstOrDefaultAsync(v => v.Id == viewId && v.TrackerId == trackerId);

                if (view == null)
                {
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));
                }
            }

            var fields = await db.Fields
                .Where(f => f.TrackerId == trackerId)
                .OrderBy(f => f.Order)
                .ToListAsync();

            var entriesQuery = db.Entries
                .Include(e => e.FieldValues)
                .ThenInclude(fv => fv.Field)
                .Where(e => e.TrackerId == trackerId);

            if (view != null)
            {
                entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, view.Filters);
                entriesQuery = ViewQueryBuilder.ApplyViewSorting(entriesQuery, view.Sorts);
            }

            var entries = await entriesQuery.ToListAsync();

            if (entries.Count == 0)
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.NoEntriesFound);
            }

            try
            {
                using var memoryStream = new MemoryStream();
                using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    // Write header
                    foreach (var field in fields)
                    {
                        csv.WriteField(field.Name);
                    }
                    await csv.NextRecordAsync();

                    // Write each entry
                    foreach (var entry in entries)
                    {
                        foreach (var field in fields)
                        {
                            var value = entry.FieldValues
                                .FirstOrDefault(fv => fv.FieldId == field.Id)
                                ?.GetValueAsString() ?? string.Empty;
                            csv.WriteField(value);
                        }
                        await csv.NextRecordAsync();
                    }

                    await csv.FlushAsync();
                    await writer.FlushAsync();
                }

                memoryStream.Position = 0;
                var fileName = $"Tracker_{tracker.Name}_Entries_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                var fileResult = new FileContentResult(memoryStream.ToArray(), "text/csv")
                {
                    FileDownloadName = fileName
                };

                return Result.Success(fileResult);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception while exporting entries to csv with trackerId={trackerId} and viewId={viewId}.", trackerId, viewId);
                return Result.Failure(ResultStatusCodes.Error, Messages.SomethingWentWrong);
            }
        }
    }
}
