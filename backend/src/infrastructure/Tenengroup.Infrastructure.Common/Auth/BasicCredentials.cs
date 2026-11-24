using System.Security.Cryptography;
using System.Text;

namespace Tenengroup.Infrastructure.Common.Auth;

public static class BasicCredentials
{
    private const string Prefix = "Basic ";

    public static bool TryRead(string? authorizationHeader, out string username, out string password)
    {
        username = string.Empty;
        password = string.Empty;

        if (string.IsNullOrEmpty(authorizationHeader) ||
            !authorizationHeader.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorizationHeader[Prefix.Length..].Trim()));
            var parts = decoded.Split(':', 2);
            if (parts.Length != 2)
            {
                return false;
            }

            username = parts[0];
            password = parts[1];
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool Matches(string? authorizationHeader, string? expectedUsername, string? expectedPassword)
    {
        if (string.IsNullOrEmpty(expectedUsername) || string.IsNullOrEmpty(expectedPassword))
        {
            return false;
        }

        return TryRead(authorizationHeader, out var username, out var password)
               && FixedTimeEquals(username, expectedUsername)
               & FixedTimeEquals(password, expectedPassword);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }
}
