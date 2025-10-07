namespace Operum.Model.DTOs.Analytics
{
    public class AnalyticConfigDto
    {
        public List<ResultTypeDto> ResultTypes { get; set; } = [];
    }

    public class ResultTypeDto
    {
        public string Name { get; set; } = default!;
        public List<CodeDto> Codes { get; set; } = [];
    }

    public class CodeDto
    {
        public string Name { get; set; } = default!;
        public List<PurposeDto> Purposes { get; set; } = [];
    }

    public class PurposeDto
    {
        public string Name { get; set; } = default!;
        public List<string> AllowedDataTypes { get; set; } = [];
    }
}
