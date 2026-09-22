namespace Operum.Model.DTOs.Fields.Requests
{
    public class ResolveDefaultValuesDto
    {
        // Keyed by field name, raw string form values as submitted from the entry-creation form.
        public Dictionary<string, string?> FieldValues { get; set; } = [];
    }

    public class ResolvedDefaultValueDto
    {
        public string FieldId { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public class ResolvedFieldVisibilityDto
    {
        public string FieldId { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public bool Visible { get; set; }
    }

    public class ResolveDefaultValuesResponseDto
    {
        public List<ResolvedDefaultValueDto> Defaults { get; set; } = [];
        public List<ResolvedFieldVisibilityDto> Visibility { get; set; } = [];
    }
}
