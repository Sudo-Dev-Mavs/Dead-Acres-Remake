using System.Collections.Generic;
using DeadAcres.Items;
using DeadAcres.Loot;
using DeadAcres.Saving;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class MenuSlotsAndLootBudgetTests
    {
        [Test]
        public void Summary_EmptySlot_SaysSo()
        {
            Assert.AreEqual("Ranura 3  ·  vacía", SaveSummary.Describe(2, null));
        }

        [Test]
        public void Summary_ShowsNameDayModeAndDifficulty()
        {
            var data = new SaveData { name = "  Granja de Marco ", dayNumber = 4, mode = "Campaña", difficulty = "Media" };
            Assert.AreEqual("Ranura 1  ·  Granja de Marco  ·  Día 4  ·  Campaña, Media", SaveSummary.Describe(0, data));

            data.name = "   ";
            Assert.AreEqual("Ranura 5  ·  Partida 5  ·  Día 4  ·  Campaña, Media", SaveSummary.Describe(4, data));
        }

        [Test]
        public void CleanName_TrimsLimitsAndNeverReturnsEmpty()
        {
            Assert.AreEqual("Mi granja", SaveSummary.CleanName("  Mi granja  ", 0));
            Assert.AreEqual("Partida 2", SaveSummary.CleanName("", 1));
            Assert.AreEqual("Partida 4", SaveSummary.CleanName(null, 3));

            string longName = new string('a', 60);
            Assert.AreEqual(SaveSummary.MaxNameLength, SaveSummary.CleanName(longName, 0).Length);
        }

        [Test]
        public void SlotNumbers_OnlyFiveAreValid()
        {
            Assert.AreEqual(5, SaveFile.SlotCount);
            Assert.IsTrue(SaveFile.IsValidSlot(0));
            Assert.IsTrue(SaveFile.IsValidSlot(4));
            Assert.IsFalse(SaveFile.IsValidSlot(5));
            Assert.IsFalse(SaveFile.IsValidSlot(-1), "Sin ranura: la partida no se guarda.");
        }

        [Test]
        public void LuckyCar_IsStableForASeed_AndAlwaysOneOfTheCars()
        {
            var seen = new HashSet<int>();
            for (int seed = 1; seed <= 400; seed++)
            {
                int lucky = LootDirector.PickLucky(seed, 5);
                Assert.IsTrue(lucky >= 0 && lucky < 5);
                Assert.AreEqual(lucky, LootDirector.PickLucky(seed, 5), "La misma semilla da el mismo auto al cargar.");
                seen.Add(lucky);
            }

            Assert.AreEqual(5, seen.Count, "Cualquier auto puede ser el afortunado.");
            Assert.AreEqual(-1, LootDirector.PickLucky(7, 0));
        }

        [Test]
        public void CarWithoutAllowance_NeverGivesAWeapon_AndTheLuckyOneNotAlways()
        {
            var created = new List<Object>();
            ItemDefinition NewItem(string id, ItemKind kind)
            {
                var item = ScriptableObject.CreateInstance<ItemDefinition>();
                item.Configure(id, kind, 99);
                created.Add(item);
                return item;
            }

            // Mismos pesos que la tabla de autos del juego: 15 de cosas normales y 2,8 de armas.
            var table = ScriptableObject.CreateInstance<LootTable>();
            created.Add(table);
            table.Configure(10, 40, 3, null, new List<LootTable.Entry>
            {
                new LootTable.Entry(NewItem("fresa", ItemKind.Seed), 3, 6, 3f),
                new LootTable.Entry(NewItem("papa", ItemKind.Seed), 2, 4, 3f),
                new LootTable.Entry(NewItem("tomate", ItemKind.Seed), 2, 3, 2f),
                new LootTable.Entry(NewItem("cinta", ItemKind.Consumable), 1, 2, 2f),
                new LootTable.Entry(NewItem("antorcha", ItemKind.Placeable), 1, 2, 2f),
                new LootTable.Entry(NewItem("muro", ItemKind.Placeable), 3, 6, 2f),
                new LootTable.Entry(NewItem("luz", ItemKind.Placeable), 1, 1, 1f),
                new LootTable.Entry(NewItem("monotiro", ItemKind.Weapon), 1, 1, 1.2f),
                new LootTable.Entry(NewItem("pistola", ItemKind.Weapon), 1, 1, 0.6f),
                new LootTable.Entry(NewItem("uzi", ItemKind.Weapon), 1, 1, 0.5f),
                new LootTable.Entry(NewItem("corredera", ItemKind.Weapon), 1, 1, 0.5f),
            }, weaponLimit: 1);

            var results = new List<ItemStack>();
            int luckyWithWeapon = 0;
            const int samples = 2000;
            for (int seed = 0; seed < samples; seed++)
            {
                table.Roll(new System.Random(seed), results, 0);
                foreach (ItemStack stack in results)
                {
                    Assert.AreNotEqual(ItemKind.Weapon, stack.Item.Kind, "Un auto sin cupo no da armas.");
                }

                Assert.AreEqual(3, results.Count);

                table.Roll(new System.Random(seed), results, 1);
                int weapons = 0;
                foreach (ItemStack stack in results)
                {
                    if (stack.Item.Kind == ItemKind.Weapon) weapons++;
                }

                Assert.IsTrue(weapons <= 1);
                if (weapons == 1) luckyWithWeapon++;
            }

            float chance = luckyWithWeapon / (float)samples;
            Assert.IsTrue(chance > 0.3f && chance < 0.6f, $"El auto afortunado trae arma el {chance:P0} de las veces: ni seguro ni imposible.");
            foreach (Object item in created) Object.DestroyImmediate(item);
        }
    }
}
