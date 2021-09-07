using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class DamagePaperDoll
    {
        public Dictionary<Location, List<int>> DamageCollection { get; set; }
        
        public Dictionary<Location, List<SpecialDamageEntry>> SpecialDamageCollection { get; set; }

        public Dictionary<Location, List<CriticalDamageEntry>> CriticalDamageCollection { get; set; }

        public PaperDoll PaperDoll;

        public PaperDollType Type => PaperDoll.Type;

        /// <summary>
        /// Blank constructor. Should never be used manually, but only in serialization.
        /// </summary>
        public DamagePaperDoll()
        {
        }

        public DamagePaperDoll(PaperDoll basePaperDoll)
        {
            DamageCollection = new Dictionary<Location, List<int>>();
            SpecialDamageCollection = new Dictionary<Location, List<SpecialDamageEntry>>();
            CriticalDamageCollection = new Dictionary<Location, List<CriticalDamageEntry>>();
            PaperDoll = basePaperDoll;
        }

        public List<Location> GetLocations()
        {
            return DamageCollection.Keys.ToList();
        }

        public void RecordDamage(Location location, int amount)
        {
            InsertDamage(location, amount);
        }

        public void RecordSpecialDamage(Location location, SpecialDamageEntry specialDamageEntry)
        {
            InsertSpecialDamage(location, specialDamageEntry);
        }

        public void RecordCriticalDamage(Location location, int damage, CriticalThreatType threatType, CriticalDamageType criticalDamageType)
        {
            InsertCriticalDamage(location, new CriticalDamageEntry(damage, threatType, criticalDamageType));
        }

        public void RecordCriticalDamage(Location location, int damage, CriticalThreatType threatType, List<CriticalDamageType> criticalDamageTypes)
        {
            foreach (var criticalDamageType in criticalDamageTypes)
            {
                InsertCriticalDamage(location, new CriticalDamageEntry(damage, threatType, criticalDamageType));
            }
        }

        public void Merge(DamagePaperDoll damagePaperDoll)
        {
            if (Type != damagePaperDoll.Type)
            {
                throw new InvalidOperationException("Cannot merge two DamagePaperDolls of different types.");
            }

            foreach (var entry in damagePaperDoll.DamageCollection)
            {
                InsertDamage(entry.Key, entry.Value);
            }

            foreach (var entry in damagePaperDoll.SpecialDamageCollection)
            {
                InsertSpecialDamage(entry.Key, entry.Value);
            }

            foreach (var entry in damagePaperDoll.CriticalDamageCollection)
            {
                InsertCriticalDamage(entry.Key, entry.Value);
            }
        }

        private void InsertDamage(Location location, int amount)
        {
            InsertDamage(location, new List<int> { amount });
        }

        private void InsertDamage(Location location, List<int> amounts)
        {
            if (DamageCollection.ContainsKey(location))
            {
                DamageCollection[location].AddRange(amounts);
            }
            else
            {
                DamageCollection.Add(location, amounts);
            }
        }

        private void InsertCriticalDamage(Location location, CriticalDamageEntry entry)
        {
            InsertCriticalDamage(location, new List<CriticalDamageEntry> { entry });
        }

        private void InsertCriticalDamage(Location location, List<CriticalDamageEntry> entries)
        {
            if (CriticalDamageCollection.ContainsKey(location))
            {
                CriticalDamageCollection[location].AddRange(entries);
            }
            else
            {
                CriticalDamageCollection.Add(location, entries);
            }
        }

        private void InsertSpecialDamage(Location location, SpecialDamageEntry entry)
        {
            InsertSpecialDamage(location, new List<SpecialDamageEntry> {entry});
        }

        private void InsertSpecialDamage(Location location, List<SpecialDamageEntry> entries)
        {
            if (SpecialDamageCollection.ContainsKey(location))
            {
                SpecialDamageCollection[location].AddRange(entries);
            }
            else
            {
                SpecialDamageCollection.Add(location, entries);
            }
        }

        public int GetTotalDamageOfType(SpecialDamageType type)
        {
            int result = 0;
            
            foreach (var (_, value) in SpecialDamageCollection)
            {
                result += value.Where(l => l.Type == type).Sum(d => int.Parse(d.Data));
            }

            return result;
        }
    }
}
