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
        public async Task<ServiceResponse<AnalyticDto>> CreateAnalytic(CreateAnalyticRequestDto createAnalytic)
        {
            var newAnalytic = mapper.Map<CreateAnalyticRequestDto, Analytic>(createAnalytic);
            await db.Analytics.AddAsync(newAnalytic);
            await db.SaveChangesAsync();
            return await GetAnalytic(newAnalytic.Id);
        }

        public async Task<ServiceResponse> DeleteAnalytic(string analyticId)
        {
            await db.Analytics
                .Where(x => x.Id == analyticId)
                .ExecuteDeleteAsync();

            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse<AnalyticDto>> GetAnalytic(string analyticId)
        {
            var analytic = await db.Analytics
                .Include(x => x.AnalyticRequiredDataTypes)
                .Include(x => x.AnalyticType)
                .FirstOrDefaultAsync(x => x.Id == analyticId);

            if (analytic == null)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound, "Analytic not found.");
            }

            return ServiceResponse.Success(mapper.Map<Analytic, AnalyticDto>(analytic));
        }

        public async Task<ServiceResponse<List<AnalyticDto>>> GetAnalyticList()
        {
            var analytics = await db.Analytics
                .Include(x => x.AnalyticRequiredDataTypes)
                .Include(x => x.AnalyticType)
                .ToListAsync();

            return ServiceResponse.Success(mapper.Map<List<Analytic>, List<AnalyticDto>>(analytics));
        }
    }
}