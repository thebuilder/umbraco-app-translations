using System.Security.Cryptography;
using System.Text;

namespace TheBuilder.AppTranslations.Authorization;

/// <summary>The comparison both of the package's keys use.</summary>
internal static class ApiKey
{
    /// <summary>
    /// Whether the supplied key is the configured one. Both are hashed first so the comparison takes
    /// the same time whatever the lengths, and then compared in constant time.
    /// </summary>
    public static bool Matches(string configuredKey, string? suppliedKey)
    {
        if (string.IsNullOrEmpty(suppliedKey))
            return false;
        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedKey));
        return CryptographicOperations.FixedTimeEquals(configuredHash, suppliedHash);
    }

    /// <summary>
    /// No minimum length: how strong the secret is, is the site's call. Whitespace is refused because
    /// a key with a space or a stray newline from an environment variable can never match the one a
    /// client sends, and would otherwise fail as a puzzling 401.
    /// </summary>
    public static bool IsUsable(string? configuredKey) =>
        string.IsNullOrWhiteSpace(configuredKey) || configuredKey.All(character => !char.IsWhiteSpace(character));
}
