using NHCParser.Core.Models;

namespace NHCParser.Core.Services;

public interface INhcAdvisoryParser
{
    ParsedAdvisory Parse(string content, IReadOnlyCollection<string>? validNames = null);
}