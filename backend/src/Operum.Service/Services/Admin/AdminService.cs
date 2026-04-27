using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.DTOs.Admin;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Admin
{
    public class AdminService(OperumContext db) : IAdminService
    {
        public async Task<Result<AdminStatsDto>> GetStats()
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);

            var stats = new AdminStatsDto
            {
                TotalUsers = await db.Users.CountAsync(),
                TotalTrackers = await db.Trackers.CountAsync(t => t.TrackerTypeId == null),
                TotalEntries = await db.Entries.CountAsync(),
                EntriesLast30Days = await db.Entries.CountAsync(e => e.CreatedAt >= cutoff),
            };

            return Result.Success(stats);
        }

        public async Task<Result<List<AdminTrackerDto>>> GetAllTrackers()
        {
            var trackers = await db.Trackers
                .Where(t => t.TrackerTypeId == null)
                .Select(t => new AdminTrackerDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    Color = t.Color,
                    OwnerId = t.OwnerId,
                    OwnerName = t.Owner.UserName,
                    FieldCount = t.Fields.Count,
                    CollaboratorCount = t.ApplicationUserTrackers.Count,
                    EntryCount = db.Entries.Count(e => e.TrackerId == t.Id),
                })
                .OrderBy(t => t.OwnerName)
                .ThenBy(t => t.Name)
                .ToListAsync();

            return Result.Success(trackers);
        }
    }
}
