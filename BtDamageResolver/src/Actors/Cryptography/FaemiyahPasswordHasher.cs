using System.Security.Cryptography;

namespace Faemiyah.BtDamageResolver.Actors.Cryptography;

/// <summary>
/// PBKDF2 (Rfc2898) password hasher with a random salt.
/// Uses the static <see cref="Rfc2898DeriveBytes.Pbkdf2(string, byte[], int, HashAlgorithmName, int)"/>
/// method, which is stateless and safe for concurrent use without any locking.
/// Uses <see cref="CryptographicOperations.FixedTimeEquals"/> for constant-time comparison
/// to prevent timing-oracle attacks.
/// </summary>
/// <remarks>
/// The stored hash format is not self-describing (no embedded algorithm/iteration tag), so hashes
/// produced by a different algorithm or iteration count will simply fail to verify.
/// </remarks>
public class FaemiyahPasswordHasher : IHasher
{
    private const int SaltLength = 32;
    private const int HashLength = 32;
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    /// <inheritdoc/>
    public (byte[] Hash, byte[] Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, HashLength);
        return (hash, salt);
    }

    /// <inheritdoc/>
    public bool Verify(string password, byte[] salt, byte[] referenceHash)
    {
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, HashLength);
        return CryptographicOperations.FixedTimeEquals(hash, referenceHash);
    }
}