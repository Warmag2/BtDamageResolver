using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// The damage report.
/// </summary>
[Serializable]
public class DamageReport
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DamageReport"/> class.
    /// </summary>
    public DamageReport()
    {
        Id = Guid.NewGuid();
        ConsumablesAttackers = [];
        ConsumablesDefender = new();
        AttackLog = new();
        TimeStamp = DateTime.UtcNow;
    }

    /// <summary>
    /// The attack log.
    /// </summary>
    public AttackLog AttackLog { get; set; }

    /// <summary>
    /// The ammo used by the attacker(s).
    /// </summary>
    public Dictionary<Guid, Consumables> ConsumablesAttackers { get; set; }

    /// <summary>
    /// The resources used by the defender.
    /// </summary>
    public Consumables ConsumablesDefender { get; set; }

    /// <summary>
    /// The damage paper doll.
    /// </summary>
    public DamagePaperDoll DamagePaperDoll { get; set; }

    /// <summary>
    /// Firing unit ID.
    /// </summary>
    public HashSet<Guid> FiringUnitIds { get; set; }

    /// <summary>
    /// Firing unit name.
    /// </summary>
    public Dictionary<Guid, string> FiringUnitNames { get; set; }

    /// <summary>
    /// The ID of this damage report.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Troopers before attack event.
    /// </summary>
    public int InitialTroopers { get; set; }

    /// <summary>
    /// The phase this damage report occurred in.
    /// </summary>
    public Phase Phase { get; set; }

    /// <summary>
    /// Target unit ID.
    /// </summary>
    public Guid TargetUnitId { get; set; }

    /// <summary>
    /// Target unit name.
    /// </summary>
    public string TargetUnitName { get; set; }

    /// <summary>
    /// The game turn for this damage report.
    /// </summary>
    public int Turn { get; set; }

    /// <summary>
    /// The update timestamp.
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// Create a blank copy of this <see cref="DamageReport"/>.
    /// </summary>
    /// <returns>A blank copy of this <see cref="DamageReport"/>.</returns>
    public DamageReport BlankCopy()
    {
        var returnedDamageReport = new DamageReport();
        returnedDamageReport.DamagePaperDoll = new(DamagePaperDoll.PaperDoll);
        returnedDamageReport.FiringUnitIds = [];
        returnedDamageReport.FiringUnitNames = [];
        returnedDamageReport.Phase = Phase;
        returnedDamageReport.TargetUnitId = TargetUnitId;
        returnedDamageReport.TargetUnitName = TargetUnitName;
        returnedDamageReport.Turn = Turn;

        return returnedDamageReport;
    }

    /// <summary>
    /// Append an attack log entry to the attack log of this damage report.
    /// </summary>
    /// <param name="entry">The attack log entry to append.</param>
    public void Log(AttackLogEntry entry)
    {
        AttackLog.Append(entry);
    }

    /// <summary>
    /// Merge this damage report with another damage report.
    /// </summary>
    /// <param name="damageReport">The damage report to merge with.</param>
    /// <exception cref="InvalidOperationException">Thrown when the damage report does not match the merged damage report.</exception>
    public void Merge(DamageReport damageReport)
    {
        if (damageReport == null)
        {
            return;
        }

        if (TargetUnitId != damageReport.TargetUnitId)
        {
            throw new InvalidOperationException("Target unit does not match. Trying to merge damage reports from different targets.");
        }

        if (Phase != damageReport.Phase)
        {
            throw new InvalidOperationException("Phase does not match. Trying to merge damage reports from different phases.");
        }

        FiringUnitIds.UnionWith(damageReport.FiringUnitIds);
        foreach(var firingUnitName in damageReport.FiringUnitNames)
        {
            FiringUnitNames.TryAdd(firingUnitName.Key, firingUnitName.Value);
        }

        AttackLog.Append(damageReport.AttackLog);

        foreach (var consumable in damageReport.ConsumablesAttackers)
        {
            if (ConsumablesAttackers.TryGetValue(consumable.Key, out Consumables value))
            {
                value.Merge(consumable.Value);
            }
            else
            {
                ConsumablesAttackers.Add(consumable.Key, consumable.Value);
            }
        }

        DamagePaperDoll.Merge(damageReport.DamagePaperDoll);
    }

    /// <summary>
    /// Spend attacker ammo.
    /// </summary>
    /// <param name="attackerId">The ID of the attacker.</param>
    /// <param name="ammoType">The ammo type to spend.</param>
    /// <param name="ammoAmount">The amount to spend.</param>
    public void SpendAmmoAttacker(Guid attackerId, string ammoType, int ammoAmount)
    {
        if (ConsumablesAttackers.TryGetValue(attackerId, out Consumables consumables))
        {
            consumables.SpendAmmo(ammoType, ammoAmount);
        }
        else
        {
            var newConsumables = new Consumables();
            newConsumables.SpendAmmo(ammoType, ammoAmount);
            ConsumablesAttackers.Add(attackerId, newConsumables);
        }
    }

    /// <summary>
    /// Spend defender ammo.
    /// </summary>
    /// <param name="ammoType">The ammo type to spend.</param>
    /// <param name="ammoAmount">The amount to spend.</param>
    public void SpendAmmoDefender(string ammoType, int ammoAmount)
    {
        ConsumablesDefender.SpendAmmo(ammoType, ammoAmount);
    }

    /// <summary>
    /// Spend attacker heat.
    /// </summary>
    /// <param name="attackerId">The ID of the attacker.</param>
    /// <param name="heat">The heat to spend.</param>
    public void SpendAttackerHeat(Guid attackerId, int heat)
    {
        if (ConsumablesAttackers.TryGetValue(attackerId, out var consumables))
        {
            consumables.Heat += heat;
        }
        else
        {
            ConsumablesAttackers.Add(attackerId, new() { Heat = heat });
        }
    }
}