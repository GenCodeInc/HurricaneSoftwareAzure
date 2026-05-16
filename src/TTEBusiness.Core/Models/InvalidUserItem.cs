namespace TTENET.TTEBusiness.Core.Models;

public sealed class InvalidUserItem
{
    public int Id { get; set; }

    public string RedirectToError { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string OsBinaryTime { get; set; } = string.Empty;

    public string RemoteHost { get; set; } = string.Empty;
}