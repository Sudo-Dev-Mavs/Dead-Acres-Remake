using System.Collections.Generic;
using DeadAcres.Enemies;
using DeadAcres.Items;
using DeadAcres.Loot;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class CryptAndLootTests
    {
        private WaveConfig config;
        private readonly List<Object> created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<WaveConfig>();
            config.ConfigureStrong(fromNight: 2, perNight: 1, maxCount: 3);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
            foreach (Object item in created) Object.DestroyImmediate(item);
            created.Clear();
        }

        [TestCase(1, 0)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(9, 3)]
        public void StrongCount_StartsOnItsNight_GrowsAndIsCapped(int night, int expected)
        {
            Assert.AreEqual(expected, WaveMath.GetStrongCount(config, night));
        }

        [Test]
        public void StrongCount_ZeroStartNight_MeansNever()
        {
            config.ConfigureStrong(fromNight: 0, perNight: 5, maxCount: 10);
            Assert.AreEqual(0, WaveMath.GetStrongCount(config, 7));
        }

        [TestCase(17, 1)]
        [TestCase(20, 3)]
        [TestCase(8, 8)]
        [TestCase(5, 9)]
        [TestCase(12, 0)]
        public void IsStrongSpawn_MarksExactlyTheStrongOnes(int total, int strong)
        {
            int marked = 0;
            for (int i = 0; i < total; i++)
            {
                if (WaveMath.IsStrongSpawn(i, total, strong)) marked++;
            }

            Assert.AreEqual(Mathf.Min(strong, total), marked);
            Assert.IsFalse(WaveMath.IsStrongSpawn(total, total, strong), "Fuera de la oleada (refuerzos) nunca.");
            Assert.IsFalse(WaveMath.IsStrongSpawn(-1, total, strong));
        }

        [Test]
        public void IsStrongSpawn_SpreadsThemAcrossTheWave()
        {
            // 3 fuertes entre 30 apariciones: uno en cada tercio, no los tres seguidos.
            var indices = new List<int>();
            for (int i = 0; i < 30; i++)
            {
                if (WaveMath.IsStrongSpawn(i, 30, 3)) indices.Add(i);
            }

            Assert.AreEqual(3, indices.Count);
            Assert.AreEqual(9, indices[0]);
            Assert.AreEqual(19, indices[1]);
            Assert.AreEqual(29, indices[2]);
        }

        [Test]
        public void LootTable_GivesGuaranteedItems_GoldInRange_AndDistinctRolls()
        {
            ItemDefinition rifle = NewItem("Rifle");
            ItemDefinition seeds = NewItem("Semillas");
            ItemDefinition tape = NewItem("Cinta");
            ItemDefinition lamp = NewItem("Luz");

            var table = ScriptableObject.CreateInstance<LootTable>();
            created.Add(table);
            table.Configure(10, 30, 2,
                new List<LootTable.Entry> { new LootTable.Entry(rifle, 1, 1) },
                new List<LootTable.Entry>
                {
                    new LootTable.Entry(seeds, 3, 6, 3f),
                    new LootTable.Entry(tape, 1, 2, 2f),
                    new LootTable.Entry(lamp, 1, 1, 1f),
                });

            var results = new List<ItemStack>();
            for (int seed = 0; seed < 200; seed++)
            {
                int gold = table.Roll(new System.Random(seed), results);

                Assert.IsTrue(gold >= 10 && gold <= 30, $"Oro fuera de rango: {gold}");
                Assert.AreEqual(3, results.Count, "El seguro más dos tiradas.");
                Assert.AreSame(rifle, results[0].Item);
                Assert.AreEqual(1, results[0].Count);
                Assert.AreNotEqual(results[1].Item, results[2].Item, "Una entrada no sale dos veces.");

                for (int i = 1; i < results.Count; i++)
                {
                    if (results[i].Item == seeds)
                    {
                        Assert.IsTrue(results[i].Count >= 3 && results[i].Count <= 6);
                    }
                }
            }
        }

        [Test]
        public void LootTable_SameSeed_SameLoot()
        {
            ItemDefinition a = NewItem("A");
            ItemDefinition b = NewItem("B");
            var table = ScriptableObject.CreateInstance<LootTable>();
            created.Add(table);
            table.Configure(0, 100, 1, null,
                new List<LootTable.Entry> { new LootTable.Entry(a, 1, 9), new LootTable.Entry(b, 1, 9) });

            var first = new List<ItemStack>();
            var second = new List<ItemStack>();
            int goldFirst = table.Roll(new System.Random(42), first);
            int goldSecond = table.Roll(new System.Random(42), second);

            Assert.AreEqual(goldFirst, goldSecond);
            Assert.AreEqual(first.Count, second.Count);
            Assert.AreSame(first[0].Item, second[0].Item);
            Assert.AreEqual(first[0].Count, second[0].Count);
        }

        [Test]
        public void LootTable_MoreRollsThanEntries_GivesEachOnce()
        {
            ItemDefinition a = NewItem("A");
            var table = ScriptableObject.CreateInstance<LootTable>();
            created.Add(table);
            table.Configure(0, 0, 5, null, new List<LootTable.Entry> { new LootTable.Entry(a, 2, 2) });

            var results = new List<ItemStack>();
            Assert.AreEqual(0, table.Roll(new System.Random(1), results));
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(2, results[0].Count);
        }

        private ItemDefinition NewItem(string name)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Configure(name, ItemKind.Consumable, 99);
            created.Add(item);
            return item;
        }
    }
}
