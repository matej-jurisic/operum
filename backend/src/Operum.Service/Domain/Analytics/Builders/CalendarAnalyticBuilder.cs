using Operum.Model.Common;
using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Extensions;

namespace Operum.Service.Domain.Analytics.Builders
{
    public class CalendarAnalyticBuilder : AnalyticResultBuilderBase
    {
        public override string SupportedType => AnalyticTypes.Calendar;

        protected override Result<AnalyticDto> BuildResult(AnalyticResultBuilderRequest request)
        {
            var result = new CalendarAnalyticDto
            {
                Name = request.Analytic.Code,
                Description = request.Analytic.Description,
                Id = request.Analytic.Id,
            };

            var whenField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.When);
            var whatField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.What);

            if (whenField == null || whatField == null)
                return Result.Success<AnalyticDto>(result);

            var calendarPoints = request.Entries
                .Select(e => new CalendarPointDto()
                {
                    EntryId = e.Id,
                    Date = e.FieldValues.FirstOrDefault(f => f.FieldId == whenField.Id)?.DateTimeValue,
                    Name = e.FieldValues.FirstOrDefault(f => f.FieldId == whatField.Id)?.GetValueAsString()
                })
                .Where(p => p.Date != null && p.Name != null)
                .ToList();

            result.Points = calendarPoints;
            result.WhenField = new()
            {
                Id = whenField.Id,
                Name = whenField.Name,
                Description = whenField.Description,
                Required = whenField.Required,
                Type = whenField.Type,
            };
            result.WhatField = new()
            {
                Id = whatField.Id,
                Name = whatField.Name,
                Description = whatField.Description,
                Required = whatField.Required,
                Type = whatField.Type,
            };

            return Result.Success<AnalyticDto>(result);
        }
    }
}
