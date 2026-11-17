using System;
using System.Collections.Generic;
using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Economy
{
    /// <summary>
    /// Lo que vende Jack y desde qué día. Como en el original, el catálogo crece con los días.
    /// Los precios viven en cada <see cref="ItemDefinition"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Dead Acres/Economy/Shop Catalog", fileName = "ShopCatalog")]
    public sealed class ShopCatalog : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public ItemDefinition item;

            [Tooltip("Primer día (1 = el primero) en que Jack lo trae.")]
            [Min(1)] public int availableFromDay;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        [NonSerialized] private List<ItemDefinition> runtimeItems;

        /// <returns>False si Jack no vende este ítem ningún día.</returns>
        public bool TryGetAvailableFromDay(ItemDefinition item, out int day)
        {
            if (item != null && runtimeItems != null && runtimeItems.Contains(item))
            {
                day = 1;
                return true;
            }

            if (item != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].item == item)
                    {
                        day = Mathf.Max(1, entries[i].availableFromDay);
                        return true;
                    }
                }
            }

            day = 0;
            return false;
        }

        /// <summary>Añade a <paramref name="results"/> lo que Jack vende ese día, en el orden del catálogo.</summary>
        /// <summary>Añade un ítem a la venta solo en memoria (los de los mods), desde el primer día.</summary>
        public void RegisterRuntime(ItemDefinition item)
        {
            if (item == null)
            {
                return;
            }

            if (runtimeItems == null) runtimeItems = new List<ItemDefinition>();
            if (!runtimeItems.Contains(item)) runtimeItems.Add(item);
        }

        public void ClearRuntime() => runtimeItems = null;

        public void GetAvailable(int dayNumber, List<ItemDefinition> results)
        {
            if (runtimeItems != null) results.AddRange(runtimeItems);

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].item != null && Mathf.Max(1, entries[i].availableFromDay) <= dayNumber)
                {
                    results.Add(entries[i].item);
                }
            }
        }

        internal void Configure(IEnumerable<KeyValuePair<ItemDefinition, int>> itemsAndDays)
        {
            entries = new List<Entry>();
            foreach (KeyValuePair<ItemDefinition, int> pair in itemsAndDays)
            {
                entries.Add(new Entry { item = pair.Key, availableFromDay = pair.Value });
            }
        }
    }
}
