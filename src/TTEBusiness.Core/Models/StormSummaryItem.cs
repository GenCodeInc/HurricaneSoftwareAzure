namespace TTENET.TTEBusiness.Core.Models;

public class StormSummaryItem
{
    public int Id { get; set; }

    public int StormId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NameYear { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public int Year { get; set; }

    public bool Active { get; set; }

    public int StormType { get; set; }

    public bool EmailAlertsSent { get; set; }

    public int StormNumber { get; set; }
}