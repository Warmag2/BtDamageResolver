using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Options;

using static Faemiyah.BtDamageResolver.Api.Extensions.EnumExtensions;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Logic
{
    public class UserStateController
    {
        private Dictionary<string, GameEntry> _gameEntries;
        private GameState _gameState;
        private ConcurrentDictionary<Guid, (string playerId, UnitEntry unit)> _unitList;
        private readonly ConcurrentDictionary<Guid, TargetNumberUpdate> _targetNumbers;
        
        public UserStateController()
        {
            DamageReportCollection = new DamageReportCollection();
            _gameEntries = new Dictionary<string, GameEntry>();
            _unitList = new ConcurrentDictionary<Guid, (string playerId, UnitEntry unit)>();
            _targetNumbers = new ConcurrentDictionary<Guid, TargetNumberUpdate>();
        }

        public event Action OnGameOptionsUpdated;

        public event Action OnPlayerOptionsUpdated;

        public event Action OnPlayerStateUpdated;

        public event Action OnDamageInstanceRequested;

        public event Action OnPlayerUnitListChanged;

        public event Action OnGameEntriesReceived;

        public event Action OnTargetNumbersReceived;
        
        public int DraggedUnitIndex { get; set; }

        public int DraggedWeaponIndex { get; set; }

        public DamageInstance DamageInstance { get; private set; }

        public GameOptions GameOptions { get; set; }

        public PlayerOptions PlayerOptions { get; set; }

        public string PlayerName { get; set; }

        public bool ConnectedToGame => GameState?.GameId != null;

        public bool ConnectedToServer { get; set; }

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

                UpdateUnitList();

                OnPlayerUnitListChanged?.Invoke();
            }
        }

        public ConcurrentDictionary<Guid, (string PlayerId, UnitEntry unit)> UnitList => _unitList;

        public PlayerState PlayerState => PlayerName == null ? null : GameState?.Players?.ContainsKey(PlayerName) == true ? GameState.Players[PlayerName] : null;

        public void AddUnit(UnitEntry unit = null)
        {
            if (unit == null)
            {
                var newUnit = PlayerState.UnitEntries.Any() ? PlayerState.UnitEntries.Last().Copy() : CommonData.GetBlankUnit();
                PlayerState.UnitEntries.Add(newUnit);
            }
            else
            {
                PlayerState.UnitEntries.Add(unit);
            }

            NotifyPlayerDataUpdated();
        }

        public void RemoveUnit(UnitEntry unit)
        {
            PlayerState.UnitEntries.Remove(unit);
            NotifyPlayerDataUpdated();
        }

        public DamageReportCollection DamageReportCollection { get; }

        public Dictionary<string, GameEntry> GameEntries
        {
            get => _gameEntries;
            set
            {
                _gameEntries = value;
                OnGameEntriesReceived?.Invoke();
            }
        }

        private void UpdateUnitList()
        {
            var newUnitList = new ConcurrentDictionary<Guid, (string playerId, UnitEntry unit)>();

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

            newUnitList.TryAdd(Guid.Empty, ("N/A", new UnitEntry {Id = Guid.Empty, Name = "NO TARGET"}));

            // Only perform dictionary swap if the list has actually changed
            // TODO: Be careful about this optimization. Might be wisest to always change the unit list.
            if (_unitList.Any(u => !newUnitList.ContainsKey(u.Key)) || newUnitList.Any(u => !_unitList.ContainsKey(u.Key)))
            {
                _unitList = newUnitList;
            }
            else
            {
                // We are also forced to check deeper if the units do not belong to the same players or their names or types have changed
                foreach (var newUnit in newUnitList)
                {
                    var oldUnit = _unitList[newUnit.Key];
                    if (oldUnit.playerId != newUnit.Value.playerId || oldUnit.unit.Name != newUnit.Value.unit.Name || oldUnit.unit.Type != newUnit.Value.unit.Type)
                    {
                        _unitList = newUnitList;
                        break;
                    }
                }
            }
        }

        public UnitType GetUnitType(Guid unitId)
        {
            if (_unitList.TryGetValue(unitId, out var unit))
            {
                return unit.unit.Type;
            }

            return UnitType.Building;
        }

        public string GetUnitName(Guid unitId)
        {
            if (_unitList.TryGetValue(unitId, out var unit))
            {
                return unit.unit.Name;
            }

            return "N/A";
        }

        public SortedDictionary<string, Guid> GetTargetsForUnit(Guid unitId)
        {
            var targetsForUnit = new SortedDictionary<string, Guid>();
            
            foreach (var (playerId, unit) in _unitList.Values)
            {
                if (unit.Id != unitId) targetsForUnit.TryAdd($"{unit.Name} ({playerId})", unit.Id);
            }

            return targetsForUnit;
        }

        public SortedDictionary<string, Guid> GetUnitIds()
        {
            var dictionary = new SortedDictionary<string, Guid>();

            foreach (var (playerId, unit) in _unitList.Values)
            {
                dictionary.TryAdd($"{unit.Name} ({playerId})", unit.Id);
            }

            return dictionary;
        }

        public void NotifyPlayerDataUpdated()
        {
            if (PlayerState != null)
            {
                PlayerState.TimeStamp = DateTime.UtcNow;
                OnPlayerStateUpdated?.Invoke();
                OnPlayerUnitListChanged?.Invoke();
            }
        }

        public void NotifyDamageRequestCreated(DamageInstance damageInstance)
        {
            DamageInstance = damageInstance;
            OnDamageInstanceRequested?.Invoke();
        }

        public void NotifyGameOptionsChanged()
        {
            GameOptions.TimeStamp = DateTime.UtcNow;
            OnGameOptionsUpdated?.Invoke();
        }

        public void NotifyPlayerOptionsChanged()
        {
            PlayerOptions.TimeStamp = DateTime.UtcNow;
            OnPlayerOptionsUpdated?.Invoke();
        }

        public Dictionary<AttackLogEntryType, bool> GetAttackLogEntryVisibilityCopy()
        {
            if (PlayerOptions != null)
            {
                return PlayerOptions.AttackLogEntryVisibility.ToDictionary(entry => entry.Key, entry => entry.Value);
            }

            return GetEnumValueList<AttackLogEntryType>().ToDictionary(enumValue => enumValue, _ => true);
        }

        public SortedDictionary<string, string> GetPlayerIds()
        {
            return new SortedDictionary<string, string>(GameState.Players.Keys.ToDictionary(p => p));
        }

        public bool DamageReportConcernsPlayer(DamageReport damageReport)
        {
            return damageReport.FiringUnitId == Guid.Empty ||
                   PlayerState.UnitEntries.Any(u => u.Id == damageReport.FiringUnitId) ||
                   PlayerState.UnitEntries.Any(u => u.Id == damageReport.TargetUnitId);
        }

        public void RecordTargetNumberUpdates(List<TargetNumberUpdate> targetNumberUpdates)
        {
            foreach (var targetNumberUpdate in targetNumberUpdates)
            {
                _targetNumbers.AddOrUpdate(targetNumberUpdate.WeaponEntryId, targetNumberUpdate, (_, update) => update.TimeStamp > targetNumberUpdate.TimeStamp ? update : targetNumberUpdate);
            }

            OnTargetNumbersReceived?.Invoke();
        }

        public TargetNumberUpdate GetTargetNumber(Guid weaponEntryId)
        {
            return _targetNumbers.TryGetValue(weaponEntryId, out var targetNumberUpdate) ? targetNumberUpdate : null;
        }
    }
}