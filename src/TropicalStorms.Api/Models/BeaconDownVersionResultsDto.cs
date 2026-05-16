namespace TropicalStorms.Api.Models;

public sealed class BeaconDownVersionResultsDto
{
    public bool LatestVersion { get; set; } = true;

    public string OlderVersonMessage { get; set; } = string.Empty;
}