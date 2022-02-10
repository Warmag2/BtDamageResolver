namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Events
{
    /// <summary>
    /// Names for all server -> player -interaction operations.
    /// </summary>
    public static class EventNames
    {
        /// <summary>
        /// The connection response event.
        /// </summary>
        public const string ConnectionResponse = nameof(ConnectionResponse);

        /// <summary>
        /// The damage report sending event.
        /// </summary>
        public const string DamageReports = nameof(DamageReports);

        /// <summary>
        /// The error message sending event.
        /// </summary>
        public const string ErrorMessage = nameof(ErrorMessage);

        /// <summary>
        /// The game entries sending event.
        /// </summary>
        public const string GameEntries = nameof(GameEntries);

        /// <summary>
        /// The game options sending event.
        /// </summary>
        public const string GameOptions = nameof(GameOptions);

        /// <summary>
        /// The game state sending event.
        /// </summary>
        public const string GameState = nameof(GameState);

        /// <summary>
        /// The player options sending event.
        /// </summary>
        public const string PlayerOptions = nameof(PlayerOptions);

        /// <summary>
        /// The target numbers sending event.
        /// </summary>
        public const string TargetNumbers = nameof(TargetNumbers);
    }
}