namespace Faemiyah.BtDamageResolver.Common.Constants
{
    /// <summary>
    /// Contains common constants for Orleans usage.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// Name of the data store for actor states.
        /// </summary>
        public const string ActorStateStoreName = "ActorState";

        /// <summary>
        /// Name of the data store for session states.
        /// </summary>
        public const string SessionStateStoreName = "SessionState";

        /// <summary>
        /// The name of the options block which contains the connection settings for RabbitMQ.
        /// </summary>
        public const string ClusterOptionsBlockName = "ClusterOptions";

        /// <summary>
        /// The name of the options block which contains the connection settings for RabbitMQ.
        /// </summary>
        public const string CommunicationOptionsBlockName = "CommunicationOptions";

        /// <summary>
        /// The name of the options block which contains the connection settings for RabbitMQ.
        /// </summary>
        public const string LoggingOptionsBlockName = "LoggingOptions";

        /// <summary>
        /// Maximum age of game entries in hours.
        /// </summary>
        public const int MaximumGameEntryAgeHours = 24;
    }
}
