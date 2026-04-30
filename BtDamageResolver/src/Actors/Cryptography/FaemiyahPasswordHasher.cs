using System.Security.Cryptography;
using System.Text;

namespace Faemiyah.BtDamageResolver.Actors.Cryptography;

/// <summary>
/// SHA-512 password hasher with a random salt.
/// Uses the static <see cref="SHA512.HashData"/> method, which is stateless and safe for concurrent
/// use without any locking — each call gets its own internal hash context from the runtime.
/// Uses <see cref="CryptographicOperations.FixedTimeEquals"/> for constant-time comparison
/// to prevent timing-oracle attacks.
/// Note: consider upgrading to PBKDF2 or Argon2 for stronger brute-force resistance in the future.
/// </summary>
public class FaemiyahPasswordHasher : IHasher
{
    private const int SaltLength = 32;

    /// <inheritdoc/>
    public (byte[] Hash, byte[] Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = SHA512.HashData(GetSaltedBytes(password, salt));
        return (hash, salt);
    }

    /// <inheritdoc/>
    public bool Verify(string password, byte[] salt, byte[] referenceHash)
    {
        var hash = SHA512.HashData(GetSaltedBytes(password, salt));
        return CryptographicOperations.FixedTimeEquals(hash, referenceHash);
    }

    private static byte[] GetSaltedBytes(string password, byte[] salt)
    {
        return [.. Encoding.UTF8.GetBytes(password), .. salt];
    }
}