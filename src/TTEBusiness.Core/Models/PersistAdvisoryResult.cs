namespace TTENET.TTEBusiness.Core.Models;

public sealed class PersistAdvisoryResult
{
    public required bool Skipped { get; init; }

    public string? SkipReason { get; init; }

    public int? StormId { get; init; }

    public bool StormCreated { get; init; }

    public bool StormUpdated { get; init; }

    public bool CoordinateInserted { get; init; }

    public int AdvisoryRowsUpdated { get; init; }

    public string StormCenterItemAction { get; init; } = "Unchanged";
}
