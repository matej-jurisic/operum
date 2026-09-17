namespace Operum.Model.DTOs.Dashboard
{
    // Each tab's id is referenced by children via DashboardItem.ParentTabId. Always at least one tab.
    public class TabsContainerConfigDto
    {
        public string? Title { get; set; }
        public List<TabDefDto> Tabs { get; set; } = [];
    }

    public class TabDefDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
