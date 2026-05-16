namespace TTENET.TTEBusiness.Core.Utilities;

public static class RegistrationCodeUtility
{
    public static bool Validate(string userId, string registrationNumber)
        => string.Equals(GetRegCode(userId), registrationNumber?.Trim(), StringComparison.OrdinalIgnoreCase);

    public static string GetRegCode(string userId)
    {
        var normalizedUserId = userId?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalizedUserId.Length <= 4)
        {
            return "Less than 4 characters";
        }

        var secondCharacter = normalizedUserId[1];
        var fourthCharacter = normalizedUserId[3];

        var a = secondCharacter * 2;
        var b = fourthCharacter + 27;

        return $"E7{a}23{b}YE";
    }
}