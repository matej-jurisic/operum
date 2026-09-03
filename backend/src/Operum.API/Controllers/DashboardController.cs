using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.Dashboard.Requests;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController(IDashboardService dashboardService) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetDashboards()
        {
            return GetApiResponse(await dashboardService.GetDashboards());
        }

        [HttpPost]
        public async Task<IActionResult> CreateDashboard([FromBody] CreateDashboardDto dto)
        {
            return GetApiResponse(await dashboardService.CreateDashboard(dto));
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderDashboards([FromBody] ReorderDashboardsDto dto)
        {
            return GetApiResponse(await dashboardService.ReorderDashboards(dto));
        }

        [HttpGet("{dashboardId}")]
        public async Task<IActionResult> GetDashboard([FromRoute] string dashboardId)
        {
            return GetApiResponse(await dashboardService.GetDashboard(dashboardId));
        }

        [HttpPut("{dashboardId}")]
        public async Task<IActionResult> UpdateDashboard([FromRoute] string dashboardId, [FromBody] UpdateDashboardDto dto)
        {
            return GetApiResponse(await dashboardService.UpdateDashboard(dashboardId, dto));
        }

        [HttpDelete("{dashboardId}")]
        public async Task<IActionResult> DeleteDashboard([FromRoute] string dashboardId)
        {
            return GetApiResponse(await dashboardService.DeleteDashboard(dashboardId));
        }

        [HttpGet("{dashboardId}/widgets")]
        public async Task<IActionResult> GetDashboardWidgets([FromRoute] string dashboardId)
        {
            return GetApiResponse(await dashboardService.GetDashboardWidgets(dashboardId));
        }

        [HttpPost("{dashboardId}/items")]
        public async Task<IActionResult> CreateAndPlaceWidget([FromRoute] string dashboardId, [FromBody] CreateAndPlaceWidgetDto dto)
        {
            return GetApiResponse(await dashboardService.CreateAndPlaceWidget(dashboardId, dto));
        }

        // Places an existing Widget Library chart by reference -- see PlaceWidgetDto. The
        // widget keeps rendering here if it's edited or deleted from the Library, unlike the
        // old copy-on-add "from-analytic" path this replaces.
        [HttpPost("{dashboardId}/items/place-widget")]
        public async Task<IActionResult> PlaceWidget([FromRoute] string dashboardId, [FromBody] PlaceWidgetDto dto)
        {
            return GetApiResponse(await dashboardService.PlaceWidget(dashboardId, dto));
        }

        [HttpPost("{dashboardId}/items/quick-add")]
        public async Task<IActionResult> AddQuickAddItem([FromRoute] string dashboardId, [FromBody] AddDashboardQuickAddItemDto dto)
        {
            return GetApiResponse(await dashboardService.AddQuickAddItem(dashboardId, dto));
        }

        [HttpPost("{dashboardId}/items/filter")]
        public async Task<IActionResult> AddFilterItem([FromRoute] string dashboardId, [FromBody] SaveFilterItemDto dto)
        {
            return GetApiResponse(await dashboardService.AddFilterItem(dashboardId, dto));
        }

        [HttpGet("{dashboardId}/views")]
        public async Task<IActionResult> GetDashboardViews([FromRoute] string dashboardId)
        {
            return GetApiResponse(await dashboardService.GetDashboardViews(dashboardId));
        }

        [HttpPost("{dashboardId}/views")]
        public async Task<IActionResult> AddDashboardView([FromRoute] string dashboardId, [FromBody] SaveDashboardViewDto dto)
        {
            return GetApiResponse(await dashboardService.AddDashboardView(dashboardId, dto));
        }

        [HttpPut("{dashboardId}/views/reorder")]
        public async Task<IActionResult> ReorderDashboardViews([FromRoute] string dashboardId, [FromBody] ReorderDashboardViewsDto dto)
        {
            return GetApiResponse(await dashboardService.ReorderDashboardViews(dashboardId, dto));
        }

        [HttpPut("{dashboardId}/views/{viewId}")]
        public async Task<IActionResult> UpdateDashboardView([FromRoute] string dashboardId, [FromRoute] string viewId, [FromBody] SaveDashboardViewDto dto)
        {
            return GetApiResponse(await dashboardService.UpdateDashboardView(dashboardId, viewId, dto));
        }

        [HttpDelete("{dashboardId}/views/{viewId}")]
        public async Task<IActionResult> DeleteDashboardView([FromRoute] string dashboardId, [FromRoute] string viewId)
        {
            return GetApiResponse(await dashboardService.DeleteDashboardView(dashboardId, viewId));
        }

        [HttpPost("{dashboardId}/items/entries")]
        public async Task<IActionResult> CreateAndPlaceEntriesWidget([FromRoute] string dashboardId, [FromBody] CreateAndPlaceEntriesWidgetDto dto)
        {
            return GetApiResponse(await dashboardService.CreateAndPlaceEntriesWidget(dashboardId, dto));
        }

        // Places an existing Widget Library Entries table by reference -- see
        // PlaceEntriesWidgetDto.
        [HttpPost("{dashboardId}/items/place-entries-widget")]
        public async Task<IActionResult> PlaceEntriesWidget([FromRoute] string dashboardId, [FromBody] PlaceEntriesWidgetDto dto)
        {
            return GetApiResponse(await dashboardService.PlaceEntriesWidget(dashboardId, dto));
        }

        [HttpPost("{dashboardId}/items/header")]
        public async Task<IActionResult> AddHeaderItem([FromRoute] string dashboardId, [FromBody] AddDashboardHeaderItemDto dto)
        {
            return GetApiResponse(await dashboardService.AddHeaderItem(dashboardId, dto));
        }

        [HttpPost("{dashboardId}/items/divider")]
        public async Task<IActionResult> AddDividerItem([FromRoute] string dashboardId)
        {
            return GetApiResponse(await dashboardService.AddDividerItem(dashboardId));
        }

        [HttpPost("{dashboardId}/items/note")]
        public async Task<IActionResult> AddNoteItem([FromRoute] string dashboardId, [FromBody] AddDashboardNoteItemDto dto)
        {
            return GetApiResponse(await dashboardService.AddNoteItem(dashboardId, dto));
        }

        [HttpPost("{dashboardId}/items/container")]
        public async Task<IActionResult> AddContainerItem([FromRoute] string dashboardId)
        {
            return GetApiResponse(await dashboardService.AddContainerItem(dashboardId));
        }

        // Only the widget's name and how each of its sources is filtered — the definition it
        // was built from stays as it was placed.
        [HttpPut("{dashboardId}/items/{itemId}")]
        public async Task<IActionResult> UpdateDashboardItem([FromRoute] string dashboardId, [FromRoute] string itemId, [FromBody] UpdateDashboardItemDto dto)
        {
            return GetApiResponse(await dashboardService.UpdateDashboardItem(dashboardId, itemId, dto));
        }

        // A filter widget's current per-clause values alone -- the inputs on the board.
        [HttpPut("{dashboardId}/items/{itemId}/filter-values")]
        public async Task<IActionResult> SetFilterValues([FromRoute] string dashboardId, [FromRoute] string itemId, [FromBody] SetFilterValuesDto dto)
        {
            return GetApiResponse(await dashboardService.SetFilterValues(dashboardId, itemId, dto));
        }

        // A filter widget's own clauses, presets and the full set of widgets that follow
        // it (in either facet) with their per-clause field maps.
        [HttpPut("{dashboardId}/items/{itemId}/filter")]
        public async Task<IActionResult> UpdateFilterItem([FromRoute] string dashboardId, [FromRoute] string itemId, [FromBody] SaveFilterItemDto dto)
        {
            return GetApiResponse(await dashboardService.UpdateFilterItem(dashboardId, itemId, dto));
        }

        // Only how an Entries widget is filtered, and whether it collapses to a button — the
        // tracker it reads from stays as it was placed.
        [HttpPut("{dashboardId}/items/{itemId}/entries")]
        public async Task<IActionResult> UpdateEntriesItem([FromRoute] string dashboardId, [FromRoute] string itemId, [FromBody] UpdateDashboardEntriesItemDto dto)
        {
            return GetApiResponse(await dashboardService.UpdateEntriesItem(dashboardId, itemId, dto));
        }

        [HttpPut("{dashboardId}/items/{itemId}/text")]
        public async Task<IActionResult> SetTextWidgetContent([FromRoute] string dashboardId, [FromRoute] string itemId, [FromBody] SetTextWidgetContentDto dto)
        {
            return GetApiResponse(await dashboardService.SetTextWidgetContent(dashboardId, itemId, dto));
        }

        [HttpDelete("{dashboardId}/items/{itemId}")]
        public async Task<IActionResult> RemoveDashboardItem([FromRoute] string dashboardId, [FromRoute] string itemId)
        {
            return GetApiResponse(await dashboardService.RemoveDashboardItem(dashboardId, itemId));
        }

        [HttpPut("{dashboardId}/layout")]
        public async Task<IActionResult> UpdateDashboardLayout([FromRoute] string dashboardId, [FromBody] UpdateDashboardLayoutDto dto)
        {
            return GetApiResponse(await dashboardService.UpdateDashboardLayout(dashboardId, dto));
        }
    }
}
