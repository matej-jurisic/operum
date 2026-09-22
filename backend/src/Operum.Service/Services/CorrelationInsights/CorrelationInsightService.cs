using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.CorrelationInsights;
using Operum.Model.Enums;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.CorrelationInsights
{
    public class CorrelationInsightService(
        ICurrentUserService currentUserService,
        ICorrelationInsightEvaluator evaluator,
        OperumContext db) : ICorrelationInsightService
    {
        public async Task<Result<List<CorrelationInsightDto>>> GetInsights()
        {
            var user = currentUserService.GetCurrentUser();
            return Result.Success(await LoadDtos(user.Id));
        }

        public async Task<Result<List<CorrelationInsightDto>>> RunNow(CancellationToken ct = default)
        {
            var user = currentUserService.GetCurrentUser();
            await evaluator.RunForUserAsync(user.Id, ct);
            return Result.Success(await LoadDtos(user.Id));
        }

        public async Task<Result> Dismiss(string insightId)
        {
            var user = currentUserService.GetCurrentUser();
            var insight = await db.FieldCorrelationInsights
                .FirstOrDefaultAsync(i => i.Id == insightId && i.UserId == user.Id);

            if (insight == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("insight"));

            insight.Dismissed = true;
            await db.SaveChangesAsync();
            return Result.Success();
        }

        private async Task<List<CorrelationInsightDto>> LoadDtos(string userId) =>
            await db.FieldCorrelationInsights
                .Where(i => i.UserId == userId && !i.Dismissed)
                .Include(i => i.TrackerA)
                .Include(i => i.TrackerB)
                .Include(i => i.ValueFieldA)
                .Include(i => i.ValueFieldB)
                .OrderByDescending(i => Math.Abs(i.Coefficient))
                .Select(i => new CorrelationInsightDto
                {
                    Id = i.Id,
                    TrackerAId = i.TrackerAId,
                    TrackerAName = i.TrackerA.Name,
                    TrackerAColor = i.TrackerA.Color,
                    MatchFieldAId = i.MatchFieldAId,
                    ValueFieldAId = i.ValueFieldAId,
                    ValueFieldAName = i.ValueFieldA.Name,
                    TrackerBId = i.TrackerBId,
                    TrackerBName = i.TrackerB.Name,
                    TrackerBColor = i.TrackerB.Color,
                    MatchFieldBId = i.MatchFieldBId,
                    ValueFieldBId = i.ValueFieldBId,
                    ValueFieldBName = i.ValueFieldB.Name,
                    Coefficient = i.Coefficient,
                    SampleSize = i.SampleSize,
                    ComputedAt = i.ComputedAt
                })
                .ToListAsync();
    }
}
