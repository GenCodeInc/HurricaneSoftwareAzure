using NHCParser.Core.Models;

namespace NHCParser.Core.Services;

public sealed class NhcAdvisoryParser : INhcAdvisoryParser
{
    private readonly NormalAdvisoryParser normalAdvisoryParser = new();
    private readonly IntermediateAdvisoryParser intermediateAdvisoryParser = new();

    public ParsedAdvisory Parse(string content, IReadOnlyCollection<string>? validNames = null)
    {
        var kind = NhcParserText.DetectKind(content);
        return kind switch
        {
            NhcAdvisoryKind.Normal => normalAdvisoryParser.Parse(content, validNames),
            NhcAdvisoryKind.Intermediate => intermediateAdvisoryParser.Parse(content, validNames),
            NhcAdvisoryKind.TropicalWeatherDiscussion => throw new NotSupportedException("Tropical weather discussion parsing is not implemented yet."),
            _ => throw new InvalidOperationException("Could not determine advisory type from advisory content."),
        };
    }
}