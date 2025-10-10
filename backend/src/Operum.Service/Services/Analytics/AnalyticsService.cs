using Operum.Model.Common;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Analytics
{
    public class AnalyticsService : IAnalyticsService
    {
        public Result<AnalyticConfig> GetAnalyticConfig()
        {
            var config = new AnalyticConfig
            {
                ResultTypes = [.. AnalyticDefinitionList.ByResultType.Select(rt => new AnalyticConfigType
                {
                    Name = rt.Key,
                    Codes = [.. rt.Value.Codes.Select(code => new AnalyticConfigCode
                    {
                        Name = code.Key,
                        Purposes = [.. code.Value.AllowedDataTypes
                            .Select(p => new AnalyticConfigPurpose
                            {
                                Name = p.Key,
                                AllowedDataTypes = [.. p.Value]
                            })]
                    })]
                })]
            };

            return Result.Success(config);
        }
    }
}
