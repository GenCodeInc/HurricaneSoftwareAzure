namespace TTENET.TTEBusiness.Core.Models;

public sealed class AdvisorySource
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public bool ReportFailures { get; set; } = true;
}