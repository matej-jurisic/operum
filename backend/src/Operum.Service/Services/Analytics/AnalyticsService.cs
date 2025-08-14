using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Enums;
using Operum.Service.Helpers;
using Operum.Service.Services.Authorization;

namespace Operum.Service.Services.Analytics
{
    public class AnalyticsService(OperumContext db, IDbContextFactory<OperumContext> dbFactory, IAuthorizationService authorizationService) : IAnalyticsService
    {
        public async Task<ServiceResponse<List<FieldAnalyticsDto>>> GetTrackerAnalytics(string trackerId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || !user.Owns(tracker))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            var fields = await db.Fields
                .Where(x => x.TrackerId == trackerId)
                .Select(f => new { f.Id, f.Type, f.Name })
                .ToListAsync();

            // Process analytics in parallel for better performance
            var analyticsTasks = fields.Select(async field =>
            {
                await using var taskDb = await dbFactory.CreateDbContextAsync();

                var analytics = field.Type switch
                {
                    OperumTypes.Number => await GetNumericAnalytics(taskDb, field.Id),
                    OperumTypes.DateTime => await GetDateTimeAnalytics(taskDb, field.Id),
                    OperumTypes.Date => await GetDateAnalytics(taskDb, field.Id),
                    OperumTypes.TimeSpan => await GetTimeSpanAnalytics(taskDb, field.Id),
                    OperumTypes.Bool => await GetBooleanAnalytics(taskDb, field.Id),
                    _ => null
                };

                if (analytics?.Data != null)
                {
                    analytics.Data.FieldName = field.Name;
                }

                return analytics?.Data;
            });

            var results = await Task.WhenAll(analyticsTasks);
            List<FieldAnalyticsDto> analyticsResult = results.Where(r => r != null).ToList()!;

            return ServiceResponse.Success(analyticsResult);
        }

        private async Task<ServiceResponse<FieldAnalyticsDto>> GetNumericAnalytics(OperumContext taskDb, string fieldId)
        {
            var result = await taskDb.FieldValues
                .Where(x => x.FieldId == fieldId && x.NumberValue.HasValue)
                .GroupBy(x => true)
                .Select(g => new
                {
                    Count = g.Count(),
                    Min = g.Min(x => x.NumberValue),
                    Max = g.Max(x => x.NumberValue),
                    Sum = g.Sum(x => x.NumberValue),
                    Average = g.Average(x => x.NumberValue),
                    SumOfSquares = g.Sum(x => x.NumberValue * x.NumberValue)
                })
                .FirstOrDefaultAsync();

            if (result == null || result.Count == 0)
            {
                return ServiceResponse.Success(new FieldAnalyticsDto());
            }

            var avg = result.Average ?? 0;
            var variance = (result.SumOfSquares ?? 0) / result.Count - (avg * avg);
            var stdDev = Math.Sqrt(Math.Max(0, variance));

            return ServiceResponse.Success(new FieldAnalyticsDto
            {
                Count = result.Count,
                Min = result.Min,
                Max = result.Max,
                Sum = result.Sum,
                Average = Math.Round(avg, 2),
                StdDev = Math.Round(stdDev, 2)
            });
        }

        private async Task<ServiceResponse<FieldAnalyticsDto>> GetTimeSpanAnalytics(OperumContext taskDb, string fieldId)
        {
            var fieldValues = await taskDb.FieldValues
                .Where(x => x.FieldId == fieldId && x.TimeSpanValue.HasValue)
                .ToListAsync();

            var result = fieldValues
                .GroupBy(x => true)
                .Select(g => new
                {
                    Count = g.Count(),
                    MinTicks = g.Min(x => x.TimeSpanValue!.Value.Ticks),
                    MaxTicks = g.Max(x => x.TimeSpanValue!.Value.Ticks),
                    AvgTicks = g.Average(x => (double)x.TimeSpanValue!.Value.Ticks)
                })
                .FirstOrDefault();


            if (result == null || result.Count == 0)
            {
                return ServiceResponse.Success(new FieldAnalyticsDto());
            }

            return ServiceResponse.Success(new FieldAnalyticsDto
            {
                Count = result.Count,
                MinTimeSpan = TimeSpan.FromTicks(result.MinTicks),
                MaxTimeSpan = TimeSpan.FromTicks(result.MaxTicks),
                AverageTimeSpan = TimeSpan.FromTicks((long)Math.Round(result.AvgTicks))
            });
        }

        private async Task<ServiceResponse<FieldAnalyticsDto>> GetDateTimeAnalytics(OperumContext taskDb, string fieldId)
        {
            var result = await taskDb.FieldValues
                .Where(x => x.FieldId == fieldId && x.DateTimeValue.HasValue)
                .GroupBy(x => true)
                .Select(g => new
                {
                    Count = g.Count(),
                    Min = g.Min(x => x.DateTimeValue),
                    Max = g.Max(x => x.DateTimeValue)
                })
                .FirstOrDefaultAsync();

            if (result == null || result.Count == 0)
            {
                return ServiceResponse.Success(new FieldAnalyticsDto());
            }

            return ServiceResponse.Success(new FieldAnalyticsDto
            {
                Count = result.Count,
                MinDateTime = result.Min,
                MaxDateTime = result.Max
            });
        }

        private async Task<ServiceResponse<FieldAnalyticsDto>> GetDateAnalytics(OperumContext taskDb, string fieldId)
        {
            var result = await taskDb.FieldValues
                .Where(x => x.FieldId == fieldId && x.DateTimeValue.HasValue)
                .Select(x => x.DateTimeValue!.Value.Date)
                .GroupBy(x => true)
                .Select(g => new
                {
                    Count = g.Count(),
                    Min = g.Min(),
                    Max = g.Max()
                })
                .FirstOrDefaultAsync();

            if (result == null || result.Count == 0)
            {
                return ServiceResponse.Success(new FieldAnalyticsDto());
            }

            return ServiceResponse.Success(new FieldAnalyticsDto
            {
                Count = result.Count,
                MinDate = result.Min,
                MaxDate = result.Max
            });
        }

        private async Task<ServiceResponse<FieldAnalyticsDto>> GetBooleanAnalytics(OperumContext taskDb, string fieldId)
        {
            var result = await taskDb.FieldValues
                .Where(x => x.FieldId == fieldId && x.BooleanValue.HasValue)
                .GroupBy(x => true)
                .Select(g => new
                {
                    Count = g.Count(),
                    TrueCount = g.Count(x => x.BooleanValue == true),
                    FalseCount = g.Count(x => x.BooleanValue == false)
                })
                .FirstOrDefaultAsync();

            if (result == null || result.Count == 0)
            {
                return ServiceResponse.Success(new FieldAnalyticsDto());
            }

            var truePercentage = (double)result.TrueCount / result.Count;

            return ServiceResponse.Success(new FieldAnalyticsDto
            {
                Count = result.Count,
                TrueCount = result.TrueCount,
                FalseCount = result.FalseCount,
                TruePercentage = Math.Round(truePercentage, 2)
            });
        }
    }
}