using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Services.Analytics
{
    public class AnalyticsService(OperumContext db, IMapper mapper) : IAnalyticsService
    {
        public async Task<Result> CreateAnalytic(CreateAnalyticRequestDto createAnalytic)
        {
            foreach (var requiredFields in createAnalytic.AnalyticRequiredDataTypesList)
            {
                var newAnalytic = mapper.Map<CreateAnalyticRequestDto, Analytic>(createAnalytic);
                newAnalytic.AnalyticRequiredDataTypes = mapper.Map<List<CreateAnalyticRequiredDataTypeDto>, List<AnalyticRequiredDataType>>(requiredFields);
                await db.Analytics.AddAsync(newAnalytic);
                await db.SaveChangesAsync();
            }
            return Result.Success();
        }

        public async Task<Result> DeleteAnalytic(string analyticId)
        {
            await db.Analytics
                .Where(x => x.Id == analyticId)
                .ExecuteDeleteAsync();

            return Result.Success();
        }

        public async Task<Result<AnalyticDto>> GetAnalytic(string analyticId)
        {
            var analytic = await db.Analytics
                .Include(x => x.AnalyticRequiredDataTypes)
                .Include(x => x.AnalyticType)
                .FirstOrDefaultAsync(x => x.Id == analyticId);

            if (analytic == null)
            {
                return Result.Failure(StatusCodeEnum.NotFound, "Analytic not found.");
            }

            return Result.Success(mapper.Map<Analytic, AnalyticDto>(analytic));
        }

        public async Task<Result<List<AnalyticDto>>> GetAnalyticList()
        {
            var analytics = await db.Analytics
                .Include(x => x.AnalyticRequiredDataTypes)
                .Include(x => x.AnalyticType)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Result.Success(mapper.Map<List<Analytic>, List<AnalyticDto>>(analytics));
        }

        public async Task<Result<List<AnalyticDto>>> GetPublicAnalyticList()
        {
            var analytics = await db.Analytics
                .Include(x => x.AnalyticRequiredDataTypes)
                .Include(x => x.AnalyticType)
                .OrderBy(x => x.Name)
                .Where(x => x.AnalyticTypeId == (int)AnalyticTypeEnum.PublicAnalytic)
                .ToListAsync();

            return Result.Success(mapper.Map<List<Analytic>, List<AnalyticDto>>(analytics));
        }
    }
}