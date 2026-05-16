namespace TTENET.TTEBusiness.Core.Models;

public sealed class CoordinateItem
{
    public int StormId { get; set; }

    public string AdvisoryNumber { get; set; } = string.Empty;

    public float Latitude { get; set; }

    public float Longitude { get; set; }

    public int WindSpeed { get; set; }

    public int SpeedTravel { get; set; }

    public int Pressure { get; set; }

    public int Direction { get; set; }

    public string Heading { get; set; } = string.Empty;

    public string UtcOffset { get; set; } = string.Empty;

    public int StormType { get; set; }

    public DateTime CoordinateDate { get; set; }

    public int CoordinateType { get; set; }
}