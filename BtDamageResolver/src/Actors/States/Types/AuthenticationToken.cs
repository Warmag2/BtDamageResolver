using System;

namespace Faemiyah.BtDamageResolver.Actors.States.Types;

/// <summary>
/// A single authentication token issued to one browser session.
/// </summary>
/// <remarks>
/// Only the token value and its creation time are stored. The expiration time is calculated from
/// the creation time and <see cref="Lifetime"/>, so the lifetime can be changed without migrating
/// persisted state.
/// </remarks>
[Serializable]
public class AuthenticationToken
{
    /// <summary>
    /// The amount of time an authentication token remains valid after it is created.
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationToken"/> class.
    /// </summary>
    /// <remarks>
    /// Parameterless constructor for serialization.
    /// </remarks>
    public AuthenticationToken()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationToken"/> class.
    /// </summary>
    /// <param name="token">The token value.</param>
    /// <param name="created">The UTC time at which the token was created.</param>
    public AuthenticationToken(Guid token, DateTime created)
    {
        Token = token;
        Created = created;
    }

    /// <summary>
    /// The token value handed to the client.
    /// </summary>
    public Guid Token { get; set; }

    /// <summary>
    /// The UTC time at which this token was created.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Checks whether this token has expired relative to the given reference time.
    /// </summary>
    /// <param name="referenceTimeUtc">The UTC reference time to compare against, typically <see cref="DateTime.UtcNow"/>.</param>
    /// <returns><b>True</b> if the token has expired, <b>false</b> otherwise.</returns>
    public bool IsExpired(DateTime referenceTimeUtc)
    {
        return referenceTimeUtc - Created > Lifetime;
    }
}
