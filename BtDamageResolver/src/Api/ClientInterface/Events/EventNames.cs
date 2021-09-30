namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Events
{
    /// <summary>
    /// Names for all server -> player -interaction operations.
    /// </summary>
    public static class EventNames
    {
        public const string ConnectionResponse = nameof(ConnectionResponse);

        public const string DamageReports = nameof(DamageReports);

        public const string ErrorMessage = nameof(ErrorMessage);

        public const string GameEntries = nameof(GameEntries);

        public const string GameOptions = nameof(GameOptions);

        public const string GameState = nameof(GameState);

        public const string PlayerOptions = nameof(PlayerOptions);

        public const string TargetNumbers = nameof(TargetNumbers);
    }
}