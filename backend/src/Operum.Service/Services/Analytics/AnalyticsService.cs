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
    public class AnalyticsService(OperumContext db, IAuthorizationService authorizationService) : IAnalyticsService
    {
        public async Task<ServiceResponse<SingleFieldAnalyticsDto>> GetSingleFieldAnalytics(string trackerId, string fieldId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var field = await db.Fields
                .Include(x => x.Tracker)
                .FirstOrDefaultAsync(x => x.TrackerId == trackerId && x.Id == fieldId);

            if (field == null || !user.Owns(field))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            return field.Type switch
            {
                DataTypes.Number => await GetSingleFieldNumberAnalytics(fieldId),
                DataTypes.DateTime => await GetSingleFieldDateTimeAnalytics(fieldId),
                DataTypes.Date => await GetSingleFieldDateAnalytics(fieldId),
                DataTypes.TimeSpan => await GetSingleFieldTimeSpanAnalytics(fieldId),
                DataTypes.Bool => await GetSingleFieldBoolAnalytics(fieldId),
                _ => ServiceResponse.Failure(StatusCodeEnum.BadRequest, $"Type {field.Type} does not support single field analytics.")
            };
        }


        private async Task<ServiceResponse<SingleFieldAnalyticsDto>> GetSingleFieldNumberAnalytics(string fieldId)
        {
            var analyticsData = await db.FieldValues
                .Where(x => x.FieldId == fieldId && x.NumberValue.HasValue)
                .GroupBy(x => true)
                .Select(g => new
                {
                    Count = g.Count(),
                    Min = g.Min(x => x.NumberValue),
                    Max = g.Max(x => x.NumberValue),
                    Average = g.Average(x => x.NumberValue),
                    SumOfSquares = g.Sum(x => x.NumberValue * x.NumberValue)
                })
                .FirstOrDefaultAsync();

            if (analyticsData == null || analyticsData.Count == 0)
            {
                return ServiceResponse.Success(new SingleFieldAnalyticsDto());
            }

            double avg = analyticsData.Average ?? 0;
            double variance = (analyticsData.SumOfSquares ?? 0 - analyticsData.Count * avg * avg) / analyticsData.Count;
            double stdDev = Math.Sqrt(variance);

            var analytics = new SingleFieldAnalyticsDto
            {
                Count = analyticsData.Count,
                Min = analyticsData.Min,
                Max = analyticsData.Max,
                Average = Math.Round(avg, 2),
                StdDev = Math.Round(stdDev, 2)
            };

            return ServiceResponse.Success(analytics);
        }

        private async Task<ServiceResponse<SingleFieldAnalyticsDto>> GetSingleFieldTimeSpanAnalytics(string fieldId)
        {
            var fieldValues = await db.FieldValues
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
                return ServiceResponse.Success(new SingleFieldAnalyticsDto());
            }

            return ServiceResponse.Success(new SingleFieldAnalyticsDto
            {
                Count = result.Count,
                MinTimeSpan = TimeSpan.FromTicks(result.MinTicks),
                MaxTimeSpan = TimeSpan.FromTicks(result.MaxTicks),
                AverageTimeSpan = TimeSpan.FromTicks((long)Math.Round(result.AvgTicks))
            });
        }

        private async Task<ServiceResponse<SingleFieldAnalyticsDto>> GetSingleFieldDateTimeAnalytics(string fieldId)
        {
            var result = await db.FieldValues
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
                return ServiceResponse.Success(new SingleFieldAnalyticsDto());
            }

            return ServiceResponse.Success(new SingleFieldAnalyticsDto
            {
                Count = result.Count,
                MinDateTime = result.Min,
                MaxDateTime = result.Max
            });
        }

        private async Task<ServiceResponse<SingleFieldAnalyticsDto>> GetSingleFieldDateAnalytics(string fieldId)
        {
            var result = await db.FieldValues
                .Where(x => x.FieldId == fieldId && x.DateTimeValue.HasValue)
                .Select(x => x.DateTimeValue!.Value.Date)
                .GroupBy(d => true)
                .Select(g => new
                {
                    Count = g.Count(),
                    Min = g.Min(),
                    Max = g.Max()
                })
                .FirstOrDefaultAsync();

            if (result == null || result.Count == 0)
            {
                return ServiceResponse.Success(new SingleFieldAnalyticsDto());
            }

            return ServiceResponse.Success(new SingleFieldAnalyticsDto
            {
                Count = result.Count,
                MinDate = result.Min,
                MaxDate = result.Max
            });
        }


        private async Task<ServiceResponse<SingleFieldAnalyticsDto>> GetSingleFieldBoolAnalytics(string fieldId)
        {
            var result = await db.FieldValues
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
                return ServiceResponse.Success(new SingleFieldAnalyticsDto());
            }

            double truePercentage = (double)result.TrueCount / result.Count * 100;

            return ServiceResponse.Success(new SingleFieldAnalyticsDto
            {
                Count = result.Count,
                TrueCount = result.TrueCount,
                FalseCount = result.FalseCount,
                TruePercentage = Math.Round(truePercentage, 2)
            });
        }

    }
}
