namespace TTENET.TTEBusiness.Core.Models;

public sealed class AdvisoryRecord
{
    public int ID { get; init; }

    public int StormID { get; init; }

    public int AdvisoryType { get; init; }

    public string URL { get; init; } = string.Empty;

    public int AdvisoryIndex { get; init; }

    public string Title { get; init; } = string.Empty;

    public string SubTitle { get; init; } = string.Empty;
}