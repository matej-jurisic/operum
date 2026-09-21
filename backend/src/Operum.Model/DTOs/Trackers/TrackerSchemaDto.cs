using System.Text.Json.Serialization;

namespace Operum.Model.DTOs.Trackers
{
    // A tracker described by names alone: no ids and no entries, safe to paste into a chat or prompt.
    public class TrackerSchemaDto
    {
        public string Name { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }
        public List<TrackerSchemaFieldDto> Fields { get; set; } = [];
        public List<TrackerSchemaViewDto> Views { get; set; } = [];
    }

    public class TrackerSchemaFieldDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Required { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? SelectOptions { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Formula { get; set; }
        // Reference fields only: the name of the tracker they point at.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? References { get; set; }
    }

    public class TrackerSchemaViewDto
    {
        public string Name { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }
        // Field names, in the order the view shows them. Empty means every field.
        public List<string> Columns { get; set; } = [];
        public List<TrackerSchemaFilterDto> Filters { get; set; } = [];
        public List<TrackerSchemaSortDto> Sorts { get; set; } = [];
    }

    public class TrackerSchemaFilterDto
    {
        public string Field { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Operator { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Value { get; set; }
    }

    public class TrackerSchemaSortDto
    {
        public string Field { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Descending { get; set; }
    }
}
