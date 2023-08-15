namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes;

/// <summary>
/// Names for all player -> server -interaction operations.
/// </summary>
public static class RequestNames
{
    /// <summary>
    /// The connection request.
    /// </summary>
    public const string Connect = nameof(Connect);

    /// <summary>
    /// The disconnection request.
    /// </summary>
    public const string Disconnect = nameof(Disconnect);

    /// <summary>
    /// A request for getting all damage reports from the game.
    /// </summary>
    public const string GetDamageReports = nameof(GetDamageReports);

    /// <summary>
    /// A requet for getting the game options.
    /// </summary>
    public const string GetGameOptions = nameof(GetGameOptions);

    /// <summary>
    /// A request for getting the current game state.
    /// </summary>
    public const string GetGameState = nameof(GetGameState);

    /// <summary>
    /// A request for getting the player options.
    /// </summary>
    public const string GetPlayerOptions = nameof(GetPlayerOptions);

    /// <summary>
    /// A request to force ready all players in the game.
    /// </summary>
    public const string ForceReady = nameof(ForceReady);

    /// <summary>
    /// A request to join a game.
    /// </summary>
    public const string JoinGame = nameof(JoinGame);

    /// <summary>
    /// A request to kick a player.
    /// </summary>
    public const string KickPlayer = nameof(KickPlayer);

    /// <summary>
    /// A request to leave a game.
    /// </summary>
    public const string LeaveGame = nameof(LeaveGame);

    /// <summary>
    /// A request to move an unit from a player to another.
    /// </summary>
    public const string MoveUnit = nameof(MoveUnit);

    /// <summary>
    /// A request to process a damage request.
    /// </summary>
    public const string SendDamageInstanceRequest = nameof(SendDamageInstanceRequest);

    /// <summary>
    /// A request to process new game options.
    /// </summary>
    public const string SendGameOptions = nameof(SendGameOptions);

    /// <summary>
    /// A request to process new player options.
    /// </summary>
    public const string SendPlayerOptions = nameof(SendPlayerOptions);

    /// <summary>
    /// A request to process new player state.
    /// </summary>
    public const string SendPlayerState = nameof(SendPlayerState);
}