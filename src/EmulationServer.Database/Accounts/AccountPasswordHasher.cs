
using System.Security.Cryptography;
using System.Text;

namespace EmulationServer.Database.Accounts;

public static class AccountPasswordHasher
{
    public static string ComputeShaPassHash(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        string normalized = $"{username.Trim().ToUpperInvariant()}:{password.ToUpperInvariant()}";
        byte[] digest = SHA1.HashData(Encoding.UTF8.GetBytes(normalized));

        return Convert.ToHexString(digest).ToLowerInvariant();
    }
}
