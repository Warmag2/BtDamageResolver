using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators;

/// <summary>
/// The client-to-server communication interface.
/// </summary>
public interface IClientToServerCommunicator
{
    /// <summary>
    /// Sends data to the server.
    /// </summary>
    /// <remarks>
    /// Envelope type is a processing hint for the recipient.
    /// </remarks>
    /// <param name="envelopeType">The type of the envelope.</param>
    /// <param name="data">The data to send.</param>
    /// <typeparam name="TType">The type of the data in the envelope.</typeparam>
    public void Send<TType>(string envelopeType, TType data)
        where TType : class;

    /// <summary>
    /// Handle an incoming <see cref="ConnectionResponse"/>.
    /// </summary>
    /// <param name="connectionResponse">The connection response.</param>
    /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
    /// <returns><b>True</b> if the connection response was successfully handled, <b>false</b> otherwise.</returns>
    public Task<bool> HandleConnectionResponse(byte[] connectionResponse, Guid correlationId);

    /// <summary>
    /// Handle incoming <see cref="DamageReport"/>s.
    /// </summary>
    /// <param name="damageReports">The damage reports.</param>
    /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
    /// <returns><b>True</b> if the damage reports were successfully handled, <b>false</b> otherwise.</returns>
    public Task<bool> HandleDamageReports(byte[] damageReports, Guid correlationId);

    /// <summary>
    /// Handle an incoming error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
    /// <returns><b>True</b> if the error message was successfully handled, <b>false</b> otherwise.</returns>
    public Task<bool> HandleErrorMessage(byte[] errorMessage, Guid correlationId);

    /// <summary>
    /// Handle incoming list of <see cref="GameEntry"/>ies.
    /// </summary>
    /// <param name="gameEntries">The game entries.</param>
    /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
    /// <returns><b>True</b> if the game entries were successfully handled, <b>false</b> otherwise.</returns>
    public Task<bool> HandleGameEntries(byte[] gameEntries, Guid correlationId);

    /// <summary>
    /// Handle incoming <see cref="GameOptions"/>.
    /// </summary>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
    /// <returns><b>True</b> if the game options were successfully handled, <b>false</b> otherwise.</returns>
    public Task<bool> HandleGameOptions(byte[] gameOptions, Guid correlationId);

    /// <summary>
    /// Handle an incoming <see cref="GameState"/>.
    /// </summary>
    /// <param name="gameState">The game state.</param>
    /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
    /// <returns><b>True</b> if the game state was successfully handled, <b>false</b> otherwise.</returns>
    public Task<bool> HandleGameState(byte[] gameState, Guid correlationId);

    /// <summary>
    /// Handle incoming <see cref="PlayerOptions"/>.
    /// </summary>
    /// <param name="playerOptions">The player options.</param>
    /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
    /// <returns><b>True</b> if the player options were successfully handled, <b>false</b> otherwise.</returns>
    public Task<bool> HandlePlayerOptions(byte[] playerOptions, Guid correlationId);

    /// <summary>
    /// Handle incoming <see cref="TargetNumberUpdate"/>s.
    /// </summary>
    /// <param name="targetNumbers">The target numbers.</param>
    /// <param name="correlationId">The correlation ID this event is related to (if any).</param>
    /// <returns><b>True</b> if the target numbers were successfully handled, <b>false</b> otherwise.</returns>
    public Task<bool> HandleTargetNumberUpdates(byte[] targetNumbers, Guid correlationId);
}