using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Common.Options;
using Faemiyah.BtDamageResolver.Services.Database;
using Faemiyah.BtDamageResolver.Services.Events;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace Faemiyah.BtDamageResolver.Services;

/// <summary>
/// Provides stateful logging methods for grains.
/// </summary>
public class LoggingService : GrainService, ILoggingService
{
    private readonly ILogger<LoggingService> _logger;
    private readonly FaemiyahLoggingOptions _loggingOptions;
    private readonly LoggingRepository _loggingRepository;
    private readonly ConcurrentQueue<GameLogEntry> _gameLogEntries;
    private readonly ConcurrentQueue<PlayerLogEntry> _playerLogEntries;
    private readonly int _loggingIntervalMilliseconds;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _writeLoopTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingService"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="clusterOptions">The cluster options.</param>
    /// <param name="loggingOptions">The logging options.</param>
    /// <param name="grainId">The grain ID.</param>
    /// <param name="silo">The silo.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public LoggingService(
        ILogger<LoggingService> logger,
        IOptions<FaemiyahClusterOptions> clusterOptions,
        IOptions<FaemiyahLoggingOptions> loggingOptions,
        GrainId grainId,
        Silo silo,
        ILoggerFactory loggerFactory) : base(grainId, silo, loggerFactory)
    {
        _logger = logger;
        _loggingOptions = loggingOptions.Value;
        _loggingIntervalMilliseconds = _loggingOptions.LoggingIntervalMilliseconds > 0 ? _loggingOptions.LoggingIntervalMilliseconds : 15000;
        _gameLogEntries = new ConcurrentQueue<GameLogEntry>();
        _playerLogEntries = new ConcurrentQueue<PlayerLogEntry>();
        _loggingRepository = new LoggingRepository(loggerFactory.CreateLogger<LoggingRepository>(), clusterOptions.Value);
    }

    /// <inheritdoc />
    public override Task Start()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _writeLoopTask = Task.Run(() => LogWriteLoop(_cancellationTokenSource.Token));
        _logger.LogInformation("{Service} running log writing loop.", GetType());

        return base.Start();
    }

    /// <inheritdoc />
    public override async Task Stop()
    {
        await _cancellationTokenSource.CancelAsync();
        await _writeLoopTask;
        _cancellationTokenSource.Dispose();

        // Final flush: drain any entries enqueued between the last loop iteration and shutdown.
        await _loggingRepository.WriteLogEntries(_gameLogEntries);
        await _loggingRepository.WriteLogEntries(_playerLogEntries);

        await base.Stop();
    }

    /// <inheritdoc />
    public Task LogGameAction(DateTime timeStamp, string gameId, GameActionType gameActionType, int actionData)
    {
        if (_loggingOptions.LogToDatabase)
        {
            _gameLogEntries.Enqueue(new GameLogEntry { ActionData = actionData, ActionType = gameActionType, GameId = gameId, TimeStamp = timeStamp });
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogPlayerAction(DateTime timeStamp, string userId, PlayerActionType playerActionType, int actionData)
    {
        if (_loggingOptions.LogToDatabase)
        {
            _playerLogEntries.Enqueue(new PlayerLogEntry { ActionData = actionData, ActionType = playerActionType, PlayerId = userId, TimeStamp = timeStamp });
        }

        return Task.CompletedTask;
    }

    private async Task LogWriteLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_gameLogEntries.IsEmpty && _playerLogEntries.IsEmpty)
                {
                    _logger.LogDebug("LoggingService has nothing to do, sleeping for {Delay} milliseconds.", _loggingIntervalMilliseconds);
                    await Task.Delay(_loggingIntervalMilliseconds, cancellationToken);
                }
                else
                {
                    if (!_gameLogEntries.IsEmpty)
                    {
                        _logger.LogDebug("LoggingService writing {Count} game log entries.", _gameLogEntries.Count);
                        await _loggingRepository.WriteLogEntries(_gameLogEntries);
                    }

                    if (!_playerLogEntries.IsEmpty)
                    {
                        _logger.LogDebug("LoggingService writing {Count} user log entries.", _playerLogEntries.Count);
                        await _loggingRepository.WriteLogEntries(_playerLogEntries);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception in log write loop. Retrying after delay.");
                try
                {
                    await Task.Delay(_loggingIntervalMilliseconds, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
