namespace TTENET.TTEBusiness.Core.Models;

public sealed class AlertRecordItem
{
    public int Id { get; set; }

    public int AlertTypeId { get; set; }

    public int ApplicationTypeId { get; set; }

    public string Value { get; set; } = string.Empty;

    public bool Confirmed { get; set; }

    public string ExternalKey { get; set; } = string.Empty;
}