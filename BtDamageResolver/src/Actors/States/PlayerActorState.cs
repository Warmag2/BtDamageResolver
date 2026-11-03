using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Actors.States.Types;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Actors.States;

/// <summary>
/// The internal state of a player actor.
/// </summary>
[Serializable]
public class PlayerActorState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerActorState"/> class.
    /// </summary>
    public PlayerActorState()
    {
        AuthenticationTokens = new Dictionary<Guid, AuthenticationToken>();
        Options = new PlayerOptions();
        PasswordHash = null;
        PasswordSalt = null;
        UnitEntries = new UnitList();
        UpdateTimeStamp = DateTime.MinValue; // When a player is created, set this to zero so that we get all updates
    }

    /// <summary>
    /// The authentication tokens of the player, keyed by the client id of the browser session that owns each token.
    /// </summary>
    /// <remarks>
    /// Each browser session has its own token, so logging in from one browser does not invalidate the sessions of
    /// other browsers. Tokens expire according to <see cref="AuthenticationToken.Lifetime"/>.
    /// </remarks>
    public Dictionary<Guid, AuthenticationToken> AuthenticationTokens { get; set; }

    /// <summary>
    /// The ID of the game this player is in.
    /// </summary>
    public string GameId { get; set; }

    /// <summary>
    /// The password hash for the player.
    /// </summary>
    public byte[] PasswordHash { get; set; }

    /// <summary>
    /// The password hash for the player.
    /// </summary>
    public byte[] PasswordSalt { get; set; }

    /// <summary>
    /// Is the player ready to proceed to the next turn.
    /// </summary>
    public bool IsReady { get; set; }

    /// <summary>
    /// The ordered list of unit IDs this player possesses.
    /// </summary>
    public UnitList UnitEntries { get; set; }

    /// <summary>
    /// The update timestamp.
    /// </summary>
    public DateTime UpdateTimeStamp { get; set; }

    /// <summary>
    /// The player options for this player.
    /// </summary>
    public PlayerOptions Options { get; set; }
}