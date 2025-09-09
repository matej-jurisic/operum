using Operum.Model.Common;
using Operum.Model.DTOs.Views;
using Operum.Model.DTOs.Views.Requests;

namespace Operum.Service.Services.Views
{
    public interface IViewsService
    {
        public Task<ServiceResponse<ViewDto>> CreateView(string trackerId, CreateViewDto view);
        public Task<ServiceResponse<ViewDto>> GetView(string trackerId, string viewId);
        public Task<ServiceResponse<List<ViewDto>>> GetViewList(string trackerId);
        public Task<ServiceResponse> DeleteView(string trackerId, string viewId);
    }
}
