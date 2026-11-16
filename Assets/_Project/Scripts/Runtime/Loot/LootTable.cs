using System;
using System.Collections.Generic;
using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Loot
{
    /// <summary>
    /// Qué puede salir al registrar algo (un auto abandonado): oro, cosas seguras y unas
    /// cuantas tiradas al azar entre entradas con peso.
    /// </summary>
    [CreateAssetMenu(menuName = "Dead Acres/Loot Table")]
    public sealed class LootTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public ItemDefinition item;
            [Min(1)] public int min;
            [Min(1)] public int max;

            [Tooltip("Probabilidad relativa frente a las demás entradas.")]
            [Min(0f)] public float weight;

            public Entry(ItemDefinition item, int min, int max, float weight = 1f)
            {
                this.item = item;
                this.min = min;
                this.max = max;
                this.weight = weight;
            }
        }

        [SerializeField, Min(0)] private int goldMin;
        [SerializeField, Min(0)] private int goldMax;

        [Tooltip("Siempre salen.")]
        [SerializeField] private List<Entry> guaranteed = new List<Entry>();

        [Tooltip("Tiradas al azar entre las entradas de abajo. Una misma entrada no sale dos veces.")]
        [SerializeField, Min(0)] private int rolls = 2;
        [SerializeField] private List<Entry> random = new List<Entry>();

        [Tooltip("Armas como mucho por tirada, contando las seguras.")]
        [SerializeField, Min(0)] private int maxWeapons = 1;

        /// <summary>Tira el botín. Con la misma semilla sale siempre lo mismo.</summary>
        /// <returns>Oro encontrado.</returns>
        public int Roll(System.Random rng, List<ItemStack> results) => Roll(rng, results, maxWeapons);

        /// <param name="weaponLimit">Armas como mucho en esta tirada (el reparto entre autos lo decide LootDirector).</param>
        public int Roll(System.Random rng, List<ItemStack> results, int weaponLimit)
        {
            results.Clear();
            for (int i = 0; i < guaranteed.Count; i++)
            {
                AddEntry(guaranteed[i], rng, results);
            }

            var pool = new List<Entry>(random.Count);
            for (int i = 0; i < random.Count; i++)
            {
                if (random[i].item != null && random[i].weight > 0f) pool.Add(random[i]);
            }

            int weapons = 0;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Item.Kind == ItemKind.Weapon) weapons++;
            }

            for (int roll = 0; roll < rolls && pool.Count > 0; roll++)
            {
                // Con el cupo de armas lleno, las que queden dejan de poder salir.
                if (weapons >= weaponLimit)
                {
                    pool.RemoveAll(entry => entry.item.Kind == ItemKind.Weapon);
                    if (pool.Count == 0) break;
                }

                int picked = PickWeighted(pool, rng);
                if (pool[picked].item.Kind == ItemKind.Weapon) weapons++;
                AddEntry(pool[picked], rng, results);
                pool.RemoveAt(picked);
            }

            int low = Mathf.Min(goldMin, goldMax);
            int high = Mathf.Max(goldMin, goldMax);
            return rng.Next(low, high + 1);
        }

        private static int PickWeighted(List<Entry> pool, System.Random rng)
        {
            float total = 0f;
            for (int i = 0; i < pool.Count; i++) total += pool[i].weight;

            double target = rng.NextDouble() * total;
            for (int i = 0; i < pool.Count; i++)
            {
                target -= pool[i].weight;
                if (target < 0d) return i;
            }

            return pool.Count - 1;
        }

        private static void AddEntry(Entry entry, System.Random rng, List<ItemStack> results)
        {
            if (entry.item == null) return;

            int low = Mathf.Max(1, Mathf.Min(entry.min, entry.max));
            int high = Mathf.Max(low, Mathf.Max(entry.min, entry.max));
            results.Add(new ItemStack(entry.item, rng.Next(low, high + 1)));
        }

        internal void Configure(int minGold, int maxGold, int rollCount, List<Entry> guaranteedEntries,
            List<Entry> randomEntries, int weaponLimit = 1)
        {
            maxWeapons = Mathf.Max(0, weaponLimit);
            goldMin = Mathf.Max(0, minGold);
            goldMax = Mathf.Max(goldMin, maxGold);
            rolls = Mathf.Max(0, rollCount);
            guaranteed = guaranteedEntries ?? new List<Entry>();
            random = randomEntries ?? new List<Entry>();
        }
    }
}
