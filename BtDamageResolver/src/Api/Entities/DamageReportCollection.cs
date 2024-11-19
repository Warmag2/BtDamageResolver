using System;
using System.Collections.Generic;
using System.Linq;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// Helper class that contains a set of damage reports and the turns that they happened on.
/// Also contains manipulation methods, which keep track of the timestamps of the damage reports.
/// </summary>
public class DamageReportCollection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DamageReportCollection"/> class.
    /// </summary>
    public DamageReportCollection()
    {
        TimeStamp = DateTime.UtcNow;
        DamageReports = new SortedDictionary<int, List<DamageReport>>();
        Visibility = new SortedDictionary<int, bool>();
    }

    /// <summary>
    /// The damage reports themselves.
    /// </summary>
    public SortedDictionary<int, List<DamageReport>> DamageReports { get; set; }

    /// <summary>
    /// The last update time of this damage report collection.
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// Visibility of individual damage report turns.
    /// </summary>
    public SortedDictionary<int, bool> Visibility { get; set; }

    /// <summary>
    /// Try to add a damage report to this damage report collection.
    /// </summary>
    /// <param name="damageReport">The damage report to add.</param>
    /// <returns><b>True</b> if the damage report was successfully added, <b>false</b> otherwise.</returns>
    public bool Add(DamageReport damageReport)
    {
        if (!DamageReports.TryGetValue(damageReport.Turn, out var damageReportList))
        {
            damageReportList = new List<DamageReport>();
            DamageReports.Add(damageReport.Turn, damageReportList);
            Visibility.Add(damageReport.Turn, true);
        }

        if (damageReportList.TrueForAll(d => d.Id != damageReport.Id))
        {
            damageReportList.Add(damageReport);
            TimeStamp = DateTime.UtcNow;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Try to add multiple entries to this damage report collection.
    /// </summary>
    /// <param name="damageReports">The damage reports to add.</param>
    public void AddRange(List<DamageReport> damageReports)
    {
        foreach (var damageReport in damageReports)
        {
            Add(damageReport);
        }
    }

    /// <summary>
    /// Clear the damage report collection completely.
    /// </summary>
    public void Clear()
    {
        DamageReports.Clear();
        Visibility.Clear();

        TimeStamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets all damage reports in this damage report collection.
    /// </summary>
    /// <returns>All the damage reports in this damage report collection.</returns>
    public List<DamageReport> GetAll()
    {
        return DamageReports.SelectMany(d => d.Value).ToList();
    }

    /// <summary>
    /// Gets all damage reports for a specific turn in this damage report collection.
    /// </summary>
    /// <param name="turn">The turn.</param>
    /// <returns>All the damage reports in this damage report collection for the specified turn, or an empty list, if none found.</returns>
    public List<DamageReport> GetReportsForTurn(int turn)
    {
        return DamageReports.TryGetValue(turn, out var damageReportsForTurn) && damageReportsForTurn != null ? damageReportsForTurn : new List<DamageReport>();
    }

    /// <summary>
    /// Removes a specific damage report from the damage report collection.
    /// </summary>
    /// <param name="damageReport">The damage report to remove.</param>
    /// <returns><b>True</b> if the report was successfully removed, <b>false</b> otherwise.</returns>
    public bool Remove(DamageReport damageReport)
    {
        var damageReportToRemove = DamageReports[damageReport.Turn].SingleOrDefault(d => d.Id == damageReport.Id);

        if (DamageReports[damageReport.Turn].Remove(damageReportToRemove))
        {
            if (DamageReports[damageReport.Turn].Count == 0)
            {
                DamageReports.Remove(damageReport.Turn);
                Visibility.Remove(damageReport.Turn);
            }

            TimeStamp = DateTime.UtcNow;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes all damage reports of a specific turn from this damage report collection.
    /// </summary>
    /// <param name="turn">The turn to remove.</param>
    /// <returns><b>True</b> if the turn was removed, or <b>false</b> if there was nothing to remove.</returns>
    public bool Remove(int turn)
    {
        var changes = Visibility.Remove(turn) && DamageReports.Remove(turn);

        if (changes)
        {
            TimeStamp = DateTime.UtcNow;
        }

        return changes;
    }

    /// <summary>
    /// Returns whether damage reports from a specific turn should be displayed.
    /// </summary>
    /// <param name="turn">The turn to query.</param>
    /// <returns>Whether the damage reports of the given turn should be displayed.</returns>
    public bool Visible(int turn)
    {
        return DamageReports.ContainsKey(turn) && Visibility[turn];
    }

    /// <summary>
    /// Toggles the visibility of the damage reports of a specific turn.
    /// </summary>
    /// <param name="turn">The turn to toggle visibility for.</param>
    public void ToggleVisible(int turn)
    {
        if (DamageReports.ContainsKey(turn))
        {
            Visibility[turn] = !Visibility[turn];
        }

        TimeStamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Is this damage report collection empty.
    /// </summary>
    /// <returns><b>True</b> if the damage report collection is empty, <b>false</b> otherwise.</returns>
    public bool IsEmpty()
    {
        return DamageReports.Count == 0;
    }
}