using Microsoft.AspNetCore.Mvc;
using TropicalStorms.Api.Models;

namespace TropicalStorms.Api.Controllers;

[ApiController]
[Route("api/xamarin-beacon/[action]")]
public sealed class XamarinBeaconController : ControllerBase
{
    [HttpGet]
    public ActionResult<BeaconDownVersionResultsDto> VersionResults() => Ok(new BeaconDownVersionResultsDto
    {
        LatestVersion = true,
        OlderVersonMessage = string.Empty,
    });

    [HttpGet]
    public ActionResult<IReadOnlyList<BeaconDataDto>> GetEmulatedData(string version) => Ok(new List<BeaconDataDto>
    {
        new()
        {
            Distance = 100d,
            Found = true,
            Rssi = -30,
        },
        new()
        {
            Distance = 99d,
            Found = true,
            Rssi = -20,
        },
    });
}