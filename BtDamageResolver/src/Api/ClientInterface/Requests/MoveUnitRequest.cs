using System;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests;

/// <summary>
/// Request for moving an unit from one player to another.
/// </summary>
public class MoveUnitRequest : AuthenticatedRequest
{
    /// <summary>
    /// The player to move the unit to.
    /// </summary>
    public string ReceivingPlayer { get; set; }

    /// <summary>
    /// The unit to move.
    /// </summary>
    public Guid UnitId { get; set; }
}