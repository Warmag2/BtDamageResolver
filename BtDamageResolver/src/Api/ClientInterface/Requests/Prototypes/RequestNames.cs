namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Requests.Prototypes
{
    /// <summary>
    /// Names for all player -> server -interaction operations.
    /// </summary>
    public static class RequestNames
    {
        public const string Connect = nameof(Connect);

        public const string Disconnect = nameof(Disconnect);

        public const string GetDamageReports = nameof(GetDamageReports);

        public const string GetGameOptions = nameof(GetGameOptions);

        public const string GetGameState = nameof(GetGameState);

        public const string GetPlayerOptions = nameof(GetPlayerOptions);

        public const string ForceReady = nameof(ForceReady);

        public const string JoinGame = nameof(JoinGame);

        public const string KickPlayer = nameof(KickPlayer);

        public const string LeaveGame = nameof(LeaveGame);

        public const string MoveUnit = nameof(MoveUnit);

        public const string SendDamageInstanceRequest = nameof(SendDamageInstanceRequest);

        public const string SendGameOptions = nameof(SendGameOptions);

        public const string SendPlayerOptions = nameof(SendPlayerOptions);

        public const string SendPlayerState = nameof(SendPlayerState);
    }
}