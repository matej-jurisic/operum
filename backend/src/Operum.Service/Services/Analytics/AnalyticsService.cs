using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Enums;
using Operum.Service.Helpers;
using Operum.Service.Services.Authorization;

namespace Operum.Service.Services.Analytics
{
    public class AnalyticsService(OperumContext db, IAuthorizationService authorizationService) : IAnalyticsService
    {
        public async Task<ServiceResponse<SingleFieldNumericAnalyticsDto>> GetSingleFieldNumericAnalytics(string trackerId, string fieldId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var field = await db.Fields
                .Include(x => x.Tracker)
                .FirstOrDefaultAsync(x => x.TrackerId == trackerId && x.Id == fieldId);

            if (field == null || !user.Owns(field))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            var analyticsData = await db.FieldValues
                .Where(x => x.FieldId == fieldId && x.NumberValue.HasValue)
                .GroupBy(x => true)
                .Select(g => new
                {
                    Min = g.Min(x => x.NumberValue),
                    Max = g.Max(x => x.NumberValue),
                    Average = g.Average(x => x.NumberValue),
                    Count = g.Count()
                })
                .FirstOrDefaultAsync();

            if (analyticsData == null)
            {
                return ServiceResponse.Success(new SingleFieldNumericAnalyticsDto());
            }

            SingleFieldNumericAnalyticsDto analytics = new()
            {
                Max = analyticsData.Max,
                Min = analyticsData.Min,
                Average = Math.Round(analyticsData.Average ?? 0, 2),
                Count = analyticsData.Count
            };

            if (analytics.Average.HasValue)
            {
                var fieldValues = await db.FieldValues
                    .Where(x => x.FieldId == fieldId && x.NumberValue.HasValue)
                    .Select(x => x.NumberValue ?? 0)
                    .ToListAsync();

                analytics.StdDev = Math.Round(Math.Sqrt(fieldValues.Average(v => Math.Pow(v - analytics.Average.Value, 2))), 2);
            }

            return ServiceResponse.Success(analytics);
        }
    }
}
