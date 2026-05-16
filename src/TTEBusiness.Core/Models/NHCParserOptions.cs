namespace TTENET.TTEBusiness.Core.Models;

public sealed class NHCParserOptions
{
    public const string SectionName = "NHCParser";

    public int SourceTimeoutSeconds { get; set; } = 30;

    public bool LogSuccessfulRuns { get; set; }

    public bool CurrentYearOnly { get; set; } = true;

    public bool ProbeDatabaseOnStartup { get; set; }

    public int DatabaseProbeMaxUrlsToLog { get; set; } = 5;

    public string? SqlConnectionString { get; set; }

    public List<int> DatabaseProbeRegions { get; set; } = new();

    public List<AdvisorySource> Sources { get; set; } = new();
}