using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;
using static Faemiyah.BtDamageResolver.Api.Extensions.EnumExtensions;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Logic;

/// <summary>
/// Methods for keeping track of and modifying user state.
/// </summary>
public class UserStateController
{
    private readonly ConcurrentDictionary<Guid, TargetNumberUpdate> _targetNumbers;
    private Dictionary<string, GameEntry> _gameEntries;
    private GameState _gameState;
    private HashSet<Guid> _invalidUnitIds = new();
    private ConcurrentDictionary<Guid, (string PlayerId, UnitEntry Unit)> _unitList;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserStateController"/> class.
    /// </summary>
    public UserStateController()
    {
        DamageReportCollection = new DamageReportCollection();
        _gameEntries = new Dictionary<string, GameEntry>();
        _unitList = new ConcurrentDictionary<Guid, (string PlayerId, UnitEntry Unit)>();
        _targetNumbers = new ConcurrentDictionary<Guid, TargetNumberUpdate>();
    }

    /// <summary>
    /// Event for when a player updates the game options.
    /// </summary>
    public event Action OnGameOptionsUpdated;

    /// <summary>
    /// Event for when a player updates his or her options.
    /// </summary>
    public event Action OnPlayerOptionsUpdated;

    /// <summary>
    /// Event for when available unit list changes.
    /// </summary>
    public event Action OnGameUnitListUpdated;

    /// <summary>
    /// Event for when a player updates his or her state.
    /// </summary>
    public event Action OnPlayerStateUpdated;

    /// <summary>
    /// Event for when the player requests a damage instance.
    /// </summary>
    public event Action OnDamageInstanceRequested;

    /// <summary>
    /// Event for when damage reports get updated.
    /// </summary>
    public event Action OnDamageReportsUpdated;

    /// <summary>
    /// Event for when player unit list gets changed.
    /// </summary>
    public event Action OnPlayerUnitListUpdated;

    /// <summary>
    /// Event for when game entries are received.
    /// </summary>
    public event Action OnGameEntriesReceived;

    /// <summary>
    /// Event for when target numbers are received.
    /// </summary>
    public event Action OnTargetNumbersUpdated;

    /// <summary>
    /// Index of the dragged unit.
    /// </summary>
    public int? DraggedUnitIndex { get; set; }

    /// <summary>
    /// Index of the dragged weapon.
    /// </summary>
    public int? DraggedWeaponIndex { get; set; }

    /// <summary>
    /// Index of the dragged weapon bay.
    /// </summary>
    public int? DraggedWeaponBayIndex { get; set; }

    /// <summary>
    /// The damage instance.
    /// </summary>
    public DamageInstance DamageInstance { get; private set; }

    /// <summary>
    /// The game options.
    /// </summary>
    public GameOptions GameOptions { get; set; }

    /// <summary>
    /// The player options.
    /// </summary>
    public PlayerOptions PlayerOptions { get; set; }

    /// <summary>
    /// The player name.
    /// </summary>
    public string PlayerName { get; set; }

    /// <summary>
    /// Indicates whether the player is connected to a game.
    /// </summary>
    public bool IsConnectedToGame => GameState?.GameId != null;

    /// <summary>
    /// Indicates whether the player is connected to the server.
    /// </summary>
    public bool IsConnectedToServer { get; set; }

    /// <summary>
    /// The game state.
    /// </summary>
    public GameState GameState
    {
        get => _gameState;
        set
        {
            if (_gameState == null || value == null)
            {
                _gameState = value;
            }
            else
            {
                if (_gameState.TimeStamp < value.TimeStamp)
                {
                    _gameState = value;
                }
            }

            // The below method checks whether it is actually necessary to invoke UI refresh.
            // Most of the time, this is not the case
            UpdateUnitList();

            NotifyPlayerUnitListUpdated();
        }
    }

    /// <summary>
    /// IDs for any invalid units as reported by the server.
    /// </summary>
    public HashSet<Guid> InvalidUnitIds
    {
        get => _invalidUnitIds;
        set
        {
            if (value != null)
            {
                _invalidUnitIds = value;
                NotifyPlayerUnitListUpdated();
            }
        }
    }

    /// <summary>
    /// Unit list, with player IDs included.
    /// </summary>
    public ConcurrentDictionary<Guid, (string PlayerId, UnitEntry Unit)> UnitList => _unitList;

    /// <summary>
    /// The player state.
    /// </summary>
    public PlayerState PlayerState
    {
        get
        {
            if (PlayerName == null)
            {
                return null;
            }

            return GameState?.Players?.ContainsKey(PlayerName) == true ? GameState.Players[PlayerName] : null;
        }
    }

    /// <summary>
    /// The damage reports.
    /// </summary>
    public DamageReportCollection DamageReportCollection { get; }

    /// <summary>
    /// Ongoing games on the server.
    /// </summary>
    public Dictionary<string, GameEntry> GameEntries
    {
        get => _gameEntries;
        set
        {
            _gameEntries = value;
            OnGameEntriesReceived?.Invoke();
        }
    }

    /// <summary>
    /// Add an unit to the player state.
    /// </summary>
    /// <param name="unit">The unit to add.</param>
    public void AddUnit(UnitEntry unit = null)
    {
        if (unit == null)
        {
            var newUnit = PlayerState.UnitEntries.Count == 0 ? new() { Name = "New Unit" } : PlayerState.UnitEntries[^1].Copy();
            PlayerState.UnitEntries.Add(newUnit);
        }
        else
        {
            PlayerState.UnitEntries.Add(unit);
        }

        NotifyPlayerDataUpdated();
    }

    /// <summary>
    /// Remove an unit from player state.
    /// </summary>
    /// <param name="unit">The unit to remove.</param>
    public void RemoveUnit(UnitEntry unit)
    {
        PlayerState.UnitEntries.Remove(unit);
        NotifyPlayerDataUpdated();
    }

    /// <summary>
    /// Get comparison time for field highlighting.
    /// </summary>
    /// <returns>The comparison time for field highlighting.</returns>
    public DateTime GetComparisonTime()
    {
        return PlayerOptions.HighlightUnalteredFields ? GameState.TurnTimeStamp : DateTime.MinValue;
    }

    /// <summary>
    /// Gets the type of the given unit.
    /// </summary>
    /// <param name="unitId">The ID of the unit.</param>
    /// <returns>The type of the unit, or default enum value, if the unit could not be found.</returns>
    public UnitType GetUnitType(Guid unitId)
    {
        if (_unitList.TryGetValue(unitId, out var unit))
        {
            return unit.Unit.Type;
        }

        return default;
    }

    /// <summary>
    /// Gets the name of the unit.
    /// </summary>
    /// <param name="unitId">The unit ID.</param>
    /// <returns>The name of the unit, or \"N/A\" if the unit could not be found.</returns>
    public string GetUnitName(Guid unitId)
    {
        if (_unitList.TryGetValue(unitId, out var unit))
        {
            return unit.Unit.Name;
        }

        return "N/A";
    }

    /// <summary>
    /// Gets all valid targets for an unit.
    /// </summary>
    /// <param name="unitId">The unit ID.</param>
    /// <returns>Dictionary containing the names and IDs of all valid targets for this unit.</returns>
    public SortedDictionary<string, Guid> GetTargetsForUnit(Guid unitId)
    {
        var targetsForUnit = new SortedDictionary<string, Guid>();

        foreach (var (playerId, unit) in _unitList.Values)
        {
            if (unit.Id != unitId)
            {
                targetsForUnit.TryAdd($"{unit.Name} ({playerId})", unit.Id);
            }
        }

        return targetsForUnit;
    }

    /// <summary>
    /// Gets all unit IDs.
    /// </summary>
    /// <returns>Dictionary containing all names and IDs of units.</returns>
    public SortedDictionary<string, Guid> GetUnitIds()
    {
        var dictionary = new SortedDictionary<string, Guid>();

        foreach (var (playerId, unit) in _unitList.Values)
        {
            dictionary.TryAdd($"{unit.Name} ({playerId})", unit.Id);
        }

        return dictionary;
    }

    /// <summary>
    /// Notification for when player data updates.
    /// </summary>
    public void NotifyPlayerDataUpdated()
    {
        if (PlayerState != null)
        {
            PlayerState.TimeStamp = DateTime.UtcNow;
            OnPlayerStateUpdated();
        }
    }

    /// <summary>
    /// Notification for when player list orders change.
    /// </summary>
    public void NotifyPlayerUnitListUpdated()
    {
        if (PlayerState != null)
        {
            OnPlayerUnitListUpdated();
        }
    }

    /// <summary>
    /// Notification for when damage instance is created.
    /// </summary>
    /// <param name="damageInstance">The damage instance.</param>
    public void NotifyDamageInstanceCreated(DamageInstance damageInstance)
    {
        DamageInstance = damageInstance;
        OnDamageInstanceRequested?.Invoke();
    }

    /// <summary>
    /// Notification when damage reports are altered.
    /// </summary>
    public void NotifyDamageReportsChanged()
    {
        OnDamageReportsUpdated?.Invoke();
    }

    /// <summary>
    /// Notification for when game options are altered.
    /// </summary>
    public void NotifyGameOptionsChanged()
    {
        GameOptions.TimeStamp = DateTime.UtcNow;
        OnGameOptionsUpdated?.Invoke();
    }

    /// <summary>
    /// Notification for when player options are altered.
    /// </summary>
    public void NotifyPlayerOptionsChanged()
    {
        PlayerOptions.TimeStamp = DateTime.UtcNow;
        OnPlayerOptionsUpdated?.Invoke();
    }

    /// <summary>
    /// Gets a copy of the current attack log entry visibility options.
    /// </summary>
    /// <returns>A copy of the current attack log entry visibility options.</returns>
    public Dictionary<AttackLogEntryType, bool> GetAttackLogEntryVisibilityCopy()
    {
        if (PlayerOptions != null)
        {
            return PlayerOptions.AttackLogEntryVisibility.ToDictionary(entry => entry.Key, entry => entry.Value);
        }

        return GetEnumValueList<AttackLogEntryType>().ToDictionary(enumValue => enumValue, _ => true);
    }

    /// <summary>
    /// Gets all player IDs.
    /// </summary>
    /// <returns>A dictionary containing all player IDs.</returns>
    public SortedDictionary<string, string> GetPlayerIds()
    {
        return new SortedDictionary<string, string>(GameState.Players.Keys.ToDictionary(p => p));
    }

    /// <summary>
    /// Gets whether a damage return concerns a given player.
    /// </summary>
    /// <param name="damageReport">The damage report.</param>
    /// <returns><b>True</b> if the damage report concerns the given player, <b>false</b> otherwise. </returns>
    public bool DamageReportConcernsPlayer(DamageReport damageReport)
    {
        return damageReport.FiringUnitId == Guid.Empty ||
               PlayerState.UnitEntries.Exists(u => u.Id == damageReport.FiringUnitId) ||
               PlayerState.UnitEntries.Exists(u => u.Id == damageReport.TargetUnitId);
    }

    /// <summary>
    /// Records target number updates to user state.
    /// </summary>
    /// <param name="targetNumberUpdates">The target number updates to record.</param>
    public void RecordTargetNumberUpdates(List<TargetNumberUpdate> targetNumberUpdates)
    {
        foreach (var targetNumberUpdate in targetNumberUpdates)
        {
            _targetNumbers.AddOrUpdate(targetNumberUpdate.UnitId, targetNumberUpdate, (_, update) => update.TimeStamp > targetNumberUpdate.TimeStamp ? update : targetNumberUpdate);
        }

        OnTargetNumbersUpdated?.Invoke();
    }

    /// <summary>
    /// Gets the target number update for a given unit.
    /// </summary>
    /// <param name="unitEntryId">The unit entry ID.</param>
    /// <returns>The target number update for the unit entry.</returns>
    public TargetNumberUpdate GetTargetNumberUpdate(Guid unitEntryId)
    {
        return _targetNumbers.TryGetValue(unitEntryId, out var targetNumberUpdate) ? targetNumberUpdate : null;
    }

    /// <summary>
    /// Gets the target number update for a given weapon entry.
    /// </summary>
    /// <param name="unitId">The unit ID.</param>
    /// <param name="weaponEntryId">The weapon entry ID.</param>
    /// <returns>The target number for the weapon entry.</returns>
    public TargetNumberUpdateSingleWeapon GetTargetNumberUpdateSingleWeapon(Guid unitId, Guid weaponEntryId)
    {
        if (_targetNumbers.TryGetValue(unitId, out var targetNumberUpdate))
        {
            return targetNumberUpdate.TargetNumbers.TryGetValue(weaponEntryId, out var singleTargetNumberUpdate) ? singleTargetNumberUpdate : null;
        }

        return null;
    }

    private void UpdateUnitList()
    {
        var newUnitList = new ConcurrentDictionary<Guid, (string PlayerId, UnitEntry Unit)>();

        if (GameState?.GameId != null)
        {
            foreach (var player in GameState.Players.Values)
            {
                foreach (var unit in player.UnitEntries)
                {
                    newUnitList.TryAdd(unit.Id, (player.PlayerId, unit));
                }
            }
        }
        else
        {
            _unitList.Clear();
        }

        newUnitList.TryAdd(Guid.Empty, ("N/A", new UnitEntry { Id = Guid.Empty, Name = " NO TARGET" }));

        // Only perform dictionary swap if the list has actually changed
        // Be careful about this optimization. Might be wisest to always change the unit list.
        if (_unitList.Any(u => !newUnitList.ContainsKey(u.Key)) || newUnitList.Any(u => !_unitList.ContainsKey(u.Key)))
        {
            _unitList = newUnitList;

            OnGameUnitListUpdated?.Invoke();
        }
        else
        {
            // We are also forced to check deeper if the units do not belong to the same players or their names or types have changed
            foreach (var newUnit in newUnitList)
            {
                var oldUnit = _unitList[newUnit.Key];
                if (oldUnit.PlayerId != newUnit.Value.PlayerId || oldUnit.Unit.Name != newUnit.Value.Unit.Name || oldUnit.Unit.Type != newUnit.Value.Unit.Type)
                {
                    _unitList = newUnitList;

                    OnGameUnitListUpdated?.Invoke();
                    break;
                }
            }
        }
    }
}