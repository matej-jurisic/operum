using Operum.Model.Common;
using Operum.Model.DTOs.Views;
using Operum.Model.DTOs.Views.Requests;

namespace Operum.Service.Interfaces
{
    public interface IViewsService
    {
        public Task<Result<ViewDto>> CreateView(string trackerId, CreateViewDto view);
        public Task<Result<ViewDto>> GetView(string trackerId, string viewId);
        public Task<Result<List<ViewDto>>> GetViewList(string trackerId);
        public Task<Result<ViewDto>> UpdateView(string trackerId, string viewId, UpdateViewDto view);
        public Task<Result> DeleteView(string trackerId, string viewId);
        public Task<Result> ReorderViews(string trackerId, ReorderViewsDto reorderViews);
    }
}
