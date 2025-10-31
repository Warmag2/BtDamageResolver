namespace Faemiyah.BtDamageResolver.Actors.Cryptography;

/// <summary>
/// Hashes passwords and salts.
/// </summary>
public interface IHasher
{
    /// <summary>
    /// Produces a hash from a password and gives the associated salt.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <returns>A byte array representing the hash and a byte array representing the generated salt.</returns>
    (byte[] Hash, byte[] Salt) Hash(string password);

    /// <summary>
    /// Verifies a hash from a password and a salt.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <param name="salt">The salt.</param>
    /// <param name="referenceHash">The hash to compare against.</param>
    /// <returns><b>True</b> if the password and salt match the hash, <b>false</b> otherwise.</returns>
    bool Verify(string password, byte[] salt, byte[] referenceHash);
}