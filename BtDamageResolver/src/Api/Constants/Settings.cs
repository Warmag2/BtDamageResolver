namespace Faemiyah.BtDamageResolver.Api.Constants;

/// <summary>
/// Common configuration constants for Orleans usage.
/// </summary>
public static class Settings
{
    /// <summary>
    /// The name of the options block which configures on-wire compression for Redis pub/sub payloads.
    /// </summary>
    public const string CompressionOptionsBlockName = "CompressionOptions";

    /// <summary>
    /// The name of the connection string for the Redis communication bus.
    /// </summary>
    public const string RedisConnectionStringName = "Redis";

    /// <summary>
    /// The name of the connection string for the Postgres database (Orleans clustering, grain storage, and logging).
    /// </summary>
    public const string PostgresConnectionStringName = "Postgres";

    /// <summary>
    /// The ADO.NET invariant name for the Postgres database provider.
    /// </summary>
    public const string PostgresInvariantName = "Npgsql";

    /// <summary>
    /// Maximum age of game entries in hours.
    /// </summary>
    public const int MaximumGameEntryAgeHours = 24;
}