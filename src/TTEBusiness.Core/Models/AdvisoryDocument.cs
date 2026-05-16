namespace TTENET.TTEBusiness.Core.Models;

public sealed class AdvisoryDocument
{
    public required AdvisorySource Source { get; init; }

    public required string Content { get; init; }

    public required DateTimeOffset FetchedAtUtc { get; init; }

    public AdvisoryType AdvisoryType { get; init; }
}