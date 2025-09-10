using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.DTOs.Views;
using Operum.Model.DTOs.Views.Requests;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Services.Authorization;

namespace Operum.Service.Services.Views
{
    public class ViewsService(IAuthorizationService authorizationService, OperumContext db, IMapper mapper) : IViewsService
    {
        public async Task<ServiceResponse<ViewDto>> CreateView(string trackerId, CreateViewDto view)
        {
            var user = authorizationService.GetCurrentUserDto();

            var tracker = await db.Trackers.FindAsync(trackerId);
            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            var userView = mapper.Map<CreateViewDto, View>(view);
            userView.TrackerId = trackerId;

            await db.Views.AddAsync(userView);
            await db.SaveChangesAsync();

            var created = await GetView(trackerId, userView.Id);
            return ServiceResponse.Success(created.Data);
        }

        public async Task<ServiceResponse> DeleteView(string trackerId, string viewId)
        {
            var user = authorizationService.GetCurrentUserDto();

            var tracker = await db.Trackers.FindAsync(trackerId);
            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            await db.Views.Where(x => x.Id == viewId && x.TrackerId == trackerId).ExecuteDeleteAsync();

            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse<ViewDto>> GetView(string trackerId, string viewId)
        {
            var user = authorizationService.GetCurrentUserDto();

            var userView = await db.Views
                .Include(x => x.Tracker)
                .Include(x => x.Sorts.OrderBy(s => s.Order))
                .ThenInclude(x => x.Field)
                .FirstOrDefaultAsync(x => x.Id == viewId && x.TrackerId == trackerId);


            if (userView == null || userView.Tracker.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            return ServiceResponse.Success(mapper.Map<View, ViewDto>(userView));
        }

        public async Task<ServiceResponse<List<ViewDto>>> GetViewList(string trackerId)
        {
            var user = authorizationService.GetCurrentUserDto();

            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || !user.Owns(tracker))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            var userViews = await db.Views
                .Include(x => x.Tracker)
                .Where(x => x.TrackerId == trackerId)
                .ToListAsync();

            return ServiceResponse.Success(mapper.Map<List<View>, List<ViewDto>>(userViews));
        }
    }
}
