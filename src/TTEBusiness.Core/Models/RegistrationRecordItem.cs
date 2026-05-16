namespace TTENET.TTEBusiness.Core.Models;

public sealed class RegistrationRecordItem
{
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string CellPhoneAlert { get; set; } = string.Empty;

    public string EmailAlert { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string RegistrationNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int QtyOrdered { get; set; }

    public int ReferredBy { get; set; }

    public DateTime DateRegistered { get; set; }

    public DateTime DateExpire { get; set; }
}