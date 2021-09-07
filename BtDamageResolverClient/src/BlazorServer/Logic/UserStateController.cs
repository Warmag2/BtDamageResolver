﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Events;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Logic
{
    public class UserStateController
    {
        private GameState _gameState;
        private ConcurrentDictionary<Guid, (string playerId, UnitEntry unit)> _unitList;
        private readonly ConcurrentDictionary<Guid, TargetNumberUpdate> _targetNumbers;
        private static readonly SemaphoreSlim StateUpdateSemaphore = new SemaphoreSlim(1, 1);
        
        public UserStateController()
        {
            DamageReportCollection = new SortedDictionary<int, DamageReportList>();
            _unitList = new ConcurrentDictionary<Guid, (string playerId, UnitEntry unit)>();
            _targetNumbers = new ConcurrentDictionary<Guid, TargetNumberUpdate>();
        }

        public event Action OnDataUpdated;

        public event Action OnGameOptionsUpdated;

        public event Action OnPlayerOptionsUpdated;

        public event Action OnDamageRequestRequested;

        public event Action OnDamageReportChange;

        public event Action OnUnitListChange;

        public int DebugPlayerStateChanges { get; set; }

        public int DraggedUnitIndex { get; set; }

        public int DraggedWeaponIndex { get; set; }

        public DamageRequest DamageRequest { get; private set; }

        public GameOptions GameOptions { get; set; }

        public PlayerOptions PlayerOptions { get; set; }

        public string PlayerName { get; set; }

        public bool ConnectedToGame => GameState?.GameId != null;

        public bool ConnectedToServer { get; set; }

        public GameState GameState
        {
            // ReSharper disable once InconsistentlySynchronizedField
            get => _gameState;
            set
            {
                StateUpdateSemaphore.Wait();

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

                StateUpdateSemaphore.Release();

                NotifyUnitListChange();
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
            NotifyUnitListChange();
            NotifyPlayerDataUpdated();
        }

        public SortedDictionary<int, DamageReportList> DamageReportCollection { get; }

        public void AddDamageReports(List<DamageReport> damageReports)
        {
            StateUpdateSemaphore.Wait();
            foreach (var damageReport in damageReports)
            {
                if (DamageReportCollection.ContainsKey(damageReport.Turn))
                {
                    DamageReportCollection[damageReport.Turn].Add(damageReport);
                }
                else
                {
                    DamageReportCollection.Add(damageReport.Turn, new DamageReportList(damageReport));
                }
            }

            StateUpdateSemaphore.Release();

            NotifyDamageReportChange();
        }

        public void DeleteDamageReport(DamageReport damageReport)
        {
            DamageReportCollection[damageReport.Turn].Remove(damageReport);

            if (DamageReportCollection[damageReport.Turn].Empty())
            {
                DeleteDamageReports(damageReport.Turn);
            }

            NotifyDamageReportChange();
        }

        public void DeleteDamageReports(int turn)
        {
            DamageReportCollection.Remove(turn);
            NotifyDamageReportChange();
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

            // TODO: Be careful about this optimization. Might be wisest to always change the unit list.
            // Only perform dictionary swap if the list has actually changed
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
                DebugPlayerStateChanges++;
                PlayerState.TimeStamp = DateTime.UtcNow;
                OnDataUpdated?.Invoke();
                NotifyUnitListChange();
            }
        }

        public void NotifyUnitListChange()
        {
            OnUnitListChange?.Invoke();
        }

        public void NotifyDamageReportChange()
        {
            OnDamageReportChange?.Invoke();
        }

        public void NotifyDamageRequestCreated(DamageRequest damageRequest)
        {
            DamageRequest = damageRequest;
            OnDamageRequestRequested?.Invoke();
        }

        public void NotifyGameOptionsChanged()
        {
            OnGameOptionsUpdated?.Invoke();
        }

        public void NotifyPlayerOptionsChanged()
        {
            OnPlayerOptionsUpdated?.Invoke();
        }

        public Dictionary<AttackLogEntryType, bool> GetAttackLogEntryVisibilityCopy()
        {
            return PlayerOptions.AttackLogEntryVisibility.ToDictionary(entry => entry.Key, entry => entry.Value);
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
        }

        public TargetNumberUpdate GetTargetNumberUpdate(Guid weaponEntryId)
        {
            return _targetNumbers.TryGetValue(weaponEntryId, out var targetNumberUpdate) ? targetNumberUpdate : null;
        }
    }
}