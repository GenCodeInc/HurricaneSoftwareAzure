namespace TropicalStorms.Api.Services;

public sealed class TropicalStormsEmailOptions
{
    public bool Enabled { get; set; }

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 25;

    public bool UseSsl { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;

    public string AdminAddress { get; set; } = string.Empty;
}
