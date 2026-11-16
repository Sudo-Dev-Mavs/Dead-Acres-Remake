using System;
using System.Collections.Generic;
using DeadAcres.Items;

namespace DeadAcres.Economy
{
    /// <summary>
    /// Qué trae la tienda un día concreto: unos pocos ítems elegidos al azar del catálogo, con
    /// más probabilidad cuanto más comunes. Los raros se dejan ver más según avanzan los días.
    /// C# puro: con la misma semilla y el mismo día sale siempre el mismo surtido.
    /// </summary>
    public static class ShopStock
    {
        public const int DefaultSize = 12;

        // Peso el primer día y cuánto crece por cada día que pasa.
        private static readonly float[] BaseWeight = { 100f, 55f, 28f, 12f, 4f };
        private static readonly float[] GrowthPerDay = { 0f, 0.15f, 0.30f, 0.50f, 0.80f };

        /// <summary>Lo mínimo que trae la tienda un día en el que ya podría traerlo todo.</summary>
        public const int MinLateSize = 6;

        /// <summary>
        /// Cuántos objetos trae ese día: 4 el primero, 6 el segundo, 9 el tercero. Desde el cuarto,
        /// casi siempre el máximo, pero hay días flojos en los que trae entre 6 y uno menos.
        /// </summary>
        public static int GetSize(int dayNumber, int seed, int maxSize = DefaultSize)
        {
            int size;
            if (dayNumber <= 1) size = 4;
            else if (dayNumber == 2) size = 6;
            else if (dayNumber == 3) size = 9;
            else
            {
                var rng = new Random(unchecked(seed * 92821 + dayNumber * 31337 + 17));
                size = rng.NextDouble() < FullStockChance || maxSize <= MinLateSize
                    ? maxSize
                    : rng.Next(MinLateSize, maxSize);
            }

            return Math.Max(1, Math.Min(size, maxSize));
        }

        /// <summary>Probabilidad, desde el cuarto día, de que el surtido venga completo.</summary>
        public const double FullStockChance = 0.6;

        public static float GetWeight(ItemRarity rarity, int dayNumber)
        {
            int index = Math.Min(Math.Max((int)rarity, 0), BaseWeight.Length - 1);
            return BaseWeight[index] * (1f + GrowthPerDay[index] * Math.Max(0, dayNumber - 1));
        }

        /// <param name="pool">Todo lo que la tienda puede llegar a vender.</param>
        /// <param name="guaranteedSeeds">Semillas que salen seguro (al azar entre las que hay): sin semillas no hay día de granja.</param>
        public static void Pick(IReadOnlyList<ItemDefinition> pool, int dayNumber, int seed, int size, int guaranteedSeeds,
            List<ItemDefinition> results)
        {
            results.Clear();
            var remaining = new List<ItemDefinition>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && !remaining.Contains(pool[i])) remaining.Add(pool[i]);
            }

            var rng = new Random(unchecked(seed * 486187739 + dayNumber * 7919));

            var seeds = new List<ItemDefinition>();
            for (int i = 0; i < remaining.Count; i++)
            {
                if (remaining[i].Kind == ItemKind.Seed) seeds.Add(remaining[i]);
            }

            for (int i = 0; i < guaranteedSeeds && seeds.Count > 0 && results.Count < size; i++)
            {
                ItemDefinition picked = TakeWeighted(seeds, dayNumber, rng);
                remaining.Remove(picked);
                results.Add(picked);
            }

            while (results.Count < size && remaining.Count > 0)
            {
                results.Add(TakeWeighted(remaining, dayNumber, rng));
            }

            // En la ventana: de más común a más raro y, dentro de cada rareza, de barato a caro.
            results.Sort(Compare);
        }

        private static ItemDefinition TakeWeighted(List<ItemDefinition> candidates, int dayNumber, Random rng)
        {
            double total = 0d;
            for (int i = 0; i < candidates.Count; i++) total += GetWeight(candidates[i].Rarity, dayNumber);

            double target = rng.NextDouble() * total;
            int picked = candidates.Count - 1;
            for (int i = 0; i < candidates.Count; i++)
            {
                target -= GetWeight(candidates[i].Rarity, dayNumber);
                if (target < 0d)
                {
                    picked = i;
                    break;
                }
            }

            ItemDefinition item = candidates[picked];
            candidates.RemoveAt(picked);
            return item;
        }

        private static int Compare(ItemDefinition a, ItemDefinition b)
        {
            int byRarity = ((int)a.Rarity).CompareTo((int)b.Rarity);
            if (byRarity != 0) return byRarity;

            int byPrice = a.BuyPrice.CompareTo(b.BuyPrice);
            return byPrice != 0 ? byPrice : string.CompareOrdinal(a.Id, b.Id);
        }
    }
}
