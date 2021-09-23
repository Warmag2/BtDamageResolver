using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Common.Options;
using Faemiyah.BtDamageResolver.Services.Database;
using Faemiyah.BtDamageResolver.Services.Events;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;
using Orleans.Core;
using Orleans.Runtime;

namespace Faemiyah.BtDamageResolver.Services
{
    /// <summary>
    /// Provides stateful logging methods for grains.
    /// </summary>
    [Reentrant]
    public class LoggingService : GrainService, ILoggingService
    {
        private const int LoggingDelayMilliseconds = 15000; // Check for logs to write 4 times a minute

        private readonly ILogger<LoggingService> _logger;
        private readonly FaemiyahLoggingOptions _loggingOptions;
        private readonly LoggingRepository _loggingRepository;
        private readonly ConcurrentQueue<GameLogEntry> _gameLogEntries;
        private readonly ConcurrentQueue<PlayerLogEntry> _playerLogEntries;
        private readonly ConcurrentQueue<UnitLogEntry> _unitLogEntries;
        
        public LoggingService(
            ILogger<LoggingService> logger,
            IOptions<FaemiyahClusterOptions> clusterOptions,
            IOptions<FaemiyahLoggingOptions> loggingOptions,
            IGrainIdentity grainId,
            Silo silo,
            ILoggerFactory loggerFactory) : base(grainId, silo, loggerFactory)
        {
            _logger = logger;
            _loggingOptions = loggingOptions.Value;
            _gameLogEntries = new ConcurrentQueue<GameLogEntry>();
            _playerLogEntries = new ConcurrentQueue<PlayerLogEntry>();
            _unitLogEntries = new ConcurrentQueue<UnitLogEntry>();
            _loggingRepository = new LoggingRepository(loggerFactory.CreateLogger<LoggingRepository>(), clusterOptions.Value);
        }

        /// <inheritdoc />
        public override Task Start()
        {
            Task.Run(LogWriteLoop);

            return base.Start();
        }

        private async Task LogWriteLoop()
        {
            while (true)
            {
                if (_gameLogEntries.IsEmpty && _playerLogEntries.IsEmpty && _unitLogEntries.IsEmpty)
                {
                    _logger.LogDebug("LoggingService has nothing to do, sleeping for {delay} milliseconds.", LoggingDelayMilliseconds);
                    await Task.Delay(LoggingDelayMilliseconds);
                }
                else
                {
                    if (!_gameLogEntries.IsEmpty)
                    {
                        _logger.LogDebug("LoggingService writing {count} game log entries.", _gameLogEntries.Count);
                        await _loggingRepository.WriteGameLogEntries(_gameLogEntries);
                    }

                    if (!_playerLogEntries.IsEmpty)
                    {
                        _logger.LogDebug("LoggingService writing {count} user log entries.", _playerLogEntries.Count);
                        await _loggingRepository.WriteUserLogEntries(_playerLogEntries);
                    }

                    if (!_unitLogEntries.IsEmpty)
                    {
                        _logger.LogDebug("LoggingService writing {count} unit log entries.", _unitLogEntries.Count);
                        await _loggingRepository.WriteUnitLogEntries(_unitLogEntries);
                    }
                }
            }
        }

        /// <inheritdoc />
        public Task LogGameAction(DateTime timeStamp, string gameId, GameActionType gameActionType, int actionData)
        {
            if (_loggingOptions.LogToDatabase)
            {
                _gameLogEntries.Enqueue(new GameLogEntry {ActionData = actionData, ActionType = gameActionType, GameId = gameId, TimeStamp = timeStamp});
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task LogPlayerAction(DateTime timeStamp, string userId, PlayerActionType playerActionType, int actionData)
        {
            if (_loggingOptions.LogToDatabase)
            {
                _playerLogEntries.Enqueue(new PlayerLogEntry {ActionData = actionData, ActionType = playerActionType, PlayerId = userId, TimeStamp = timeStamp});
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task LogUnitAction(DateTime timeStamp, string unitId, UnitActionType unitActionType, int actionData)
        {
            if (_loggingOptions.LogToDatabase)
            {
                _unitLogEntries.Enqueue(new UnitLogEntry { ActionData = actionData, ActionType = unitActionType, UnitId = unitId, TimeStamp = timeStamp });
            }

            return Task.CompletedTask;
        }
    }
}
