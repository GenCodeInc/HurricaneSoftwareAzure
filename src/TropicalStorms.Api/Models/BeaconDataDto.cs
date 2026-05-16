namespace TropicalStorms.Api.Models;

public sealed class BeaconDataDto
{
    public bool Found { get; set; }

    public int Rssi { get; set; }

    public double Distance { get; set; }
}