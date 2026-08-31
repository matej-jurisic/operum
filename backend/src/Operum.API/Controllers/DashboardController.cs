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

        [HttpPost("{dashboardId}/items/view")]
        public async Task<IActionResult> AddViewItem([FromRoute] string dashboardId, [FromBody] AddDashboardViewItemDto dto)
        {
            return GetApiResponse(await dashboardService.AddViewItem(dashboardId, dto));
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

        // Only the widget's name and how each of its sources is filtered — the definition it
        // was built from stays as it was placed.
        [HttpPut("{dashboardId}/items/{itemId}")]
        public async Task<IActionResult> UpdateDashboardItem([FromRoute] string dashboardId, [FromRoute] string itemId, [FromBody] UpdateDashboardItemDto dto)
        {
            return GetApiResponse(await dashboardService.UpdateDashboardItem(dashboardId, itemId, dto));
        }

        [HttpPut("{dashboardId}/items/{itemId}/view-selection")]
        public async Task<IActionResult> SetViewWidgetSelection([FromRoute] string dashboardId, [FromRoute] string itemId, [FromBody] SetViewWidgetSelectionDto dto)
        {
            return GetApiResponse(await dashboardService.SetViewWidgetSelection(dashboardId, itemId, dto));
        }

        // A View selector's starting view and the full set of board widgets that follow it —
        // the same links each following widget's own form can set from its side.
        [HttpPut("{dashboardId}/items/{itemId}/view")]
        public async Task<IActionResult> UpdateViewItem([FromRoute] string dashboardId, [FromRoute] string itemId, [FromBody] UpdateDashboardViewItemDto dto)
        {
            return GetApiResponse(await dashboardService.UpdateViewItem(dashboardId, itemId, dto));
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
