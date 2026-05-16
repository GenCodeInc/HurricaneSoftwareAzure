using NHCParser.Core.Models;

namespace NHCParser.Core.Services;

public interface INhcTropicalWeatherDiscussionParser
{
    ParsedTropicalWeatherDiscussion Parse(string content);
}