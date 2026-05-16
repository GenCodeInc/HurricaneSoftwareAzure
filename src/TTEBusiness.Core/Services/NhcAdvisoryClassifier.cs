using TTENET.TTEBusiness.Core.Models;

namespace TTENET.TTEBusiness.Core.Services;

public sealed class NhcAdvisoryClassifier
{
    public AdvisoryType Classify(string content)
    {
        foreach (var line in content.Split('\n'))
        {
            if (line.Contains("TCMAT", StringComparison.OrdinalIgnoreCase) || line.Contains("TCMEP", StringComparison.OrdinalIgnoreCase) || line.Contains("TCMCP", StringComparison.OrdinalIgnoreCase))
            {
                return AdvisoryType.Normal;
            }

            if (line.Contains("TCPAT", StringComparison.OrdinalIgnoreCase) || line.Contains("TCPEP", StringComparison.OrdinalIgnoreCase) || line.Contains("TCPCP", StringComparison.OrdinalIgnoreCase))
            {
                return AdvisoryType.Intermediate;
            }

            if (line.Contains("TWDEP", StringComparison.OrdinalIgnoreCase) || line.Contains("TWDAT", StringComparison.OrdinalIgnoreCase))
            {
                return AdvisoryType.TropicalWeatherDiscussion;
            }
        }

        return AdvisoryType.Unknown;
    }
}