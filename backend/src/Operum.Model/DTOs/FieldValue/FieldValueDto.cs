namespace Operum.Model.DTOs.FieldValue
{
    public class FieldValueDto
    {
        public string FieldId { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public object? Value { get; set; }
    }
}
