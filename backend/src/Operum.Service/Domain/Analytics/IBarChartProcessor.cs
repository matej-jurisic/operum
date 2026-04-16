using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics
{
    public interface IBarChartProcessor
    {
        List<DonutChartPointDto> Process(List<DonutChartPointDto> dataPoints);
    }
}
