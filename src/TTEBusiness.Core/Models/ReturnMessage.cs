namespace TTENET.TTEBusiness.Core.Models;

public sealed class ReturnMessage
{
    public int MessageNumber { get; set; } = -1;

    public string Message { get; set; } = string.Empty;

    public ReturnMessage()
    {
    }

    public ReturnMessage(int messageNumber, string message)
    {
        MessageNumber = messageNumber;
        Message = message;
    }
}