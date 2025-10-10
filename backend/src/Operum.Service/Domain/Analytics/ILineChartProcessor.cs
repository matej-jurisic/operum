using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics
{
    public interface ILineChartProcessor
    {
        List<LineChartPointDto> Process(List<LineChartPointDto> dataPoints);
    }
}
