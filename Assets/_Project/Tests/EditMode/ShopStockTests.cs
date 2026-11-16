using System.Collections.Generic;
using DeadAcres.Economy;
using DeadAcres.Items;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class ShopStockTests
    {
        private readonly List<ItemDefinition> pool = new List<ItemDefinition>();
        private readonly List<ItemDefinition> seeds = new List<ItemDefinition>();
        private ItemDefinition legendary;

        [SetUp]
        public void SetUp()
        {
            // 30 ítems: 4 semillas comunes, 20 comunes más, 5 raros y 1 legendario.
            for (int i = 0; i < 4; i++) seeds.Add(NewItem($"seed_{i}", ItemKind.Seed, ItemRarity.Common, 5 + i));
            for (int i = 0; i < 20; i++) NewItem($"common_{i:00}", ItemKind.Placeable, ItemRarity.Common, 10 + i);
            for (int i = 0; i < 5; i++) NewItem($"rare_{i}", ItemKind.Placeable, ItemRarity.Rare, 100 + i);
            legendary = NewItem("legendary", ItemKind.Weapon, ItemRarity.Legendary, 900);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (ItemDefinition item in pool) Object.DestroyImmediate(item);
            pool.Clear();
            seeds.Clear();
        }

        [Test]
        public void Pick_GivesTwelveDistinctItems()
        {
            var stock = new List<ItemDefinition>();
            ShopStock.Pick(pool, 1, 1234, ShopStock.DefaultSize, 2, stock);

            Assert.AreEqual(12, stock.Count);
            Assert.AreEqual(12, new HashSet<ItemDefinition>(stock).Count, "Ninguno repetido.");
        }

        [Test]
        public void Pick_SameSeedAndDay_SameStock_ButAnotherDayChangesIt()
        {
            var first = new List<ItemDefinition>();
            var again = new List<ItemDefinition>();
            var nextDay = new List<ItemDefinition>();
            ShopStock.Pick(pool, 3, 77, 12, 2, first);
            ShopStock.Pick(pool, 3, 77, 12, 2, again);
            ShopStock.Pick(pool, 4, 77, 12, 2, nextDay);

            Assert.AreEqual(first.Count, again.Count);
            for (int i = 0; i < first.Count; i++) Assert.AreSame(first[i], again[i]);

            bool different = false;
            for (int i = 0; i < first.Count; i++) different |= first[i] != nextDay[i];
            Assert.IsTrue(different, "Al día siguiente el surtido cambia.");
        }

        [Test]
        public void Pick_AlwaysBringsTheGuaranteedSeeds()
        {
            var stock = new List<ItemDefinition>();
            for (int seed = 0; seed < 300; seed++)
            {
                ShopStock.Pick(pool, 1, seed, 12, 2, stock);
                int seedCount = 0;
                foreach (ItemDefinition item in stock)
                {
                    if (item.Kind == ItemKind.Seed) seedCount++;
                }

                Assert.IsTrue(seedCount >= 2, $"Semilla {seed}: solo {seedCount} tipo(s) de semilla.");
            }
        }

        [Test]
        public void Pick_RarerItemsShowUpLessOften_AndMoreOnLaterDays()
        {
            int day1 = CountAppearances(legendary, 1);
            int day10 = CountAppearances(legendary, 10);
            int commonDay1 = CountAppearances(pool[10], 1);

            Assert.IsTrue(day1 < commonDay1 / 3, $"Legendario {day1} veces frente a un común {commonDay1}.");
            Assert.IsTrue(day10 > day1 * 2, $"Día 10: {day10} veces; día 1: {day1}.");
        }

        [Test]
        public void Pick_SmallPool_GivesEverythingOnce()
        {
            var few = new List<ItemDefinition> { pool[0], pool[5], pool[5], null, legendary };
            var stock = new List<ItemDefinition>();
            ShopStock.Pick(few, 1, 9, 12, 2, stock);

            Assert.AreEqual(3, stock.Count);
            Assert.AreSame(legendary, stock[2], "Ordenado de común a raro.");
        }

        [TestCase(ItemRarity.Common, 1, 100f)]
        [TestCase(ItemRarity.Common, 9, 100f)]
        [TestCase(ItemRarity.Legendary, 1, 4f)]
        [TestCase(ItemRarity.Legendary, 6, 20f)]
        public void Weight_GrowsWithTheDaysOnlyForRareItems(ItemRarity rarity, int day, float expected)
        {
            Assert.AreEqual(expected, ShopStock.GetWeight(rarity, day), 0.001f);
        }

        private int CountAppearances(ItemDefinition item, int day)
        {
            var stock = new List<ItemDefinition>();
            int count = 0;
            for (int seed = 0; seed < 2000; seed++)
            {
                ShopStock.Pick(pool, day, seed, 12, 2, stock);
                if (stock.Contains(item)) count++;
            }

            return count;
        }

        private ItemDefinition NewItem(string id, ItemKind kind, ItemRarity rarity, int price)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Configure(id, kind, 99);
            item.ConfigureExtras(id, Color.white);
            item.ConfigureRarity(rarity);
            item.ConfigurePrices(price, 0);
            pool.Add(item);
            return item;
        }
    }
}
