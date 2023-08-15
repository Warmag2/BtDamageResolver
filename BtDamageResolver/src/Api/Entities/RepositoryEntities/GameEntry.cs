using System;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;

namespace Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;

/// <summary>
/// A game entry for storing information about ongoing games.
/// </summary>
[Serializable]
public class GameEntry : NamedEntity
{
    /// <summary>
    /// Is the game password protected.
    /// </summary>
    public bool PasswordProtected { get; set; }

    /// <summary>
    /// Number of players in the game.
    /// </summary>
    public int Players { get; set; }

    /// <summary>
    /// The update timestamp.
    /// </summary>
    public DateTime TimeStamp { get; set; }
}