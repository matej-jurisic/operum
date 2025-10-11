using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics
{
    public interface IDonutChartProcessor
    {
        List<DonutChartPointDto> Process(List<DonutChartPointDto> dataPoints);
    }
}
