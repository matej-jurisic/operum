namespace Operum.Model.DTOs.Views.Requests
{
    public class CreateViewDto
    {
        public string Name { get; set; } = string.Empty;

        public List<CreateViewSortDto> Sorts { get; set; } = [];
    }
}
