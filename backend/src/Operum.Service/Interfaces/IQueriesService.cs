using Operum.Model.Common;
using Operum.Model.DTOs.Queries;
using Operum.Model.DTOs.Queries.Requests;

namespace Operum.Service.Interfaces
{
    public interface IQueriesService
    {
        public Task<Result<QueryDto>> CreateQuery(string trackerId, CreateQueryDto query);
        public Task<Result<QueryDto>> GetQuery(string trackerId, string queryId);
        public Task<Result<List<QueryDto>>> GetQueryList(string trackerId);
        public Task<Result<QueryDto>> UpdateQuery(string trackerId, string queryId, UpdateQueryDto query);
        public Task<Result> DeleteQuery(string trackerId, string queryId);
    }
}
