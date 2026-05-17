namespace TropicalStorms.Api.Services;

public sealed class WebsiteAcsEmailOptions
{
    public bool Enabled { get; set; }

    public string ConnectionString { get; set; } = string.Empty;

    public string SenderAddress { get; set; } = string.Empty;

    public string SenderDisplayName { get; set; } = "Tracking The Eye";

    public string AdminAddress { get; set; } = string.Empty;
}