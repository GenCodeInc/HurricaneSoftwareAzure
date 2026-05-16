namespace TropicalStorms.Api.LegacyCompat;

public sealed class LegacySoapOptions
{
    public bool Enabled { get; set; } = true;

    public string Path { get; set; } = "/Services/TropicalStorms.asmx";

    public string[] AllowHttpHostNames { get; set; } = [];
}