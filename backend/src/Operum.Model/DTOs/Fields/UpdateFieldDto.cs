namespace Operum.Model.DTOs.Fields
{
    public class UpdateFieldDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}
