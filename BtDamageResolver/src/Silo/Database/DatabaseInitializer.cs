using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Faemiyah.BtDamageResolver.Silo.Database;

/// <summary>
/// Creates the Orleans clustering and persistence schema on an empty PostgreSQL database.
/// </summary>
/// <remarks>
/// Orleans' ADO.NET providers do not create their own schema, so the silo applies the embedded
/// Orleans SQL scripts itself on startup. Initialization is skipped once the schema exists (detected
/// via the <c>OrleansQuery</c> table), which makes repeated starts idempotent, and a transaction-level
/// advisory lock serialises first-time creation across concurrently starting silos.
/// </remarks>
internal static class DatabaseInitializer
{
    /// <summary>
    /// Arbitrary application-specific key for the transaction-level advisory lock that serialises
    /// first-time schema creation across concurrently starting silos.
    /// </summary>
    private const long AdvisoryLockKey = 0x4274_4452_0001L;

    /// <summary>
    /// Ensures the Orleans schema exists, creating it from the embedded scripts if the database is empty.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task which finishes when the database has been initialized.</returns>
    public static async Task EnsureInitializedAsync(string connectionString, ILogger logger, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        if (await IsInitializedAsync(connection, null, cancellationToken))
        {
            logger.LogInformation("Orleans schema already present. Skipping database initialization.");
            return;
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var lockCommand = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@key)", connection, transaction))
        {
            lockCommand.Parameters.AddWithValue("key", AdvisoryLockKey);
            await lockCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        // Re-check after acquiring the lock in case another silo created the schema while we waited.
        if (await IsInitializedAsync(connection, transaction, cancellationToken))
        {
            logger.LogInformation("Orleans schema created by another silo while waiting for the initialization lock. Skipping.");
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        foreach (var (name, script) in GetScripts())
        {
            logger.LogInformation("Applying Orleans schema script {ScriptName}.", name);
            await using var command = new NpgsqlCommand(script, connection, transaction);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Orleans schema initialization completed.");
    }

    private static async Task<bool> IsInitializedAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT to_regclass('orleansquery') IS NOT NULL", connection, transaction);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static IEnumerable<(string Name, string Script)> GetScripts()
    {
        var assembly = Assembly.GetExecutingAssembly();

        return assembly
            .GetManifestResourceNames()
            .Where(name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => (Name: name, Script: ReadResource(assembly, name)));
    }

    private static string ReadResource(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded schema script '{name}' could not be loaded.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
