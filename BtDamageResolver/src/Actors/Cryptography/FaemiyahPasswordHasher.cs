using System.Security.Cryptography;
using System.Text;

namespace Faemiyah.BtDamageResolver.Actors.Cryptography;

/// <summary>
/// Shitty implementation of a password hasher.
/// </summary>
public class FaemiyahPasswordHasher : IHasher
{
    private const int SaltLength = 32;
    private readonly SHA512 _sha;

    /// <summary>
    /// Initializes a new instance of the <see cref="FaemiyahPasswordHasher"/> class.
    /// </summary>
    public FaemiyahPasswordHasher()
    {
        _sha = SHA512.Create();
    }

    /// <inheritdoc/>
    public (byte[] Hash, byte[] Salt) Hash(string password)
    {
        var salt = GetSalt();

        var hash = _sha.ComputeHash(GetSaltedBytes(password, salt));

        return (hash, salt);
    }

    /// <inheritdoc/>
    public bool Verify(string password, byte[] salt, byte[] referenceHash)
    {
        var hash = _sha.ComputeHash(GetSaltedBytes(password, salt));

        return CompareHashes(hash, referenceHash);
    }

    private static bool CompareHashes(byte[] hash1, byte[] hash2)
    {
        if (hash1.Length != hash2.Length)
        {
            return false;
        }

        for (int ii = 0; ii < hash1.Length; ii++)
        {
            if (hash1[ii] != hash2[ii])
            {
                return false;
            }
        }

        return true;
    }

    private static byte[] GetSalt()
    {
        return RandomNumberGenerator.GetBytes(SaltLength);
    }

    private static byte[] GetSaltedBytes(string password, byte[] salt)
    {
        return [.. Encoding.UTF8.GetBytes(password), .. salt];
    }
}