using System.Collections.Generic;
using DeadAcres.Building;
using DeadAcres.Items;
using DeadAcres.Loot;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class GroundAimAndCarLootTests
    {
        private static readonly Vector3 Eye = new Vector3(10f, 1.62f, 20f);

        [Test]
        public void Aim_LookingDown_GivesTheSpotOnTheGround()
        {
            // 45° hacia abajo mirando a +Z: toca el suelo a 1,62 m por delante.
            var ray = new Ray(Eye, new Vector3(0f, -1f, 1f).normalized);
            Assert.IsTrue(GroundAim.TryGetPoint(ray, 0f, 6f, out Vector3 point));
            Assert.AreEqual(10f, point.x, 0.001f);
            Assert.AreEqual(0f, point.y, 0.001f);
            Assert.AreEqual(21.62f, point.z, 0.001f);
        }

        [Test]
        public void Aim_LookingFartherThanReach_StopsAtTheEdgeOfReach_InThatDirection()
        {
            // Casi horizontal: el rayo tocaría el suelo a decenas de metros.
            var ray = new Ray(Eye, new Vector3(1f, -0.02f, 0f).normalized);
            Assert.IsTrue(GroundAim.TryGetPoint(ray, 0f, 6f, out Vector3 point));
            Assert.AreEqual(16f, point.x, 0.001f);
            Assert.AreEqual(20f, point.z, 0.001f);
        }

        [Test]
        public void Aim_LookingLevelOrUp_StillGivesATargetAhead_NotAtTheFeet()
        {
            Assert.IsTrue(GroundAim.TryGetPoint(new Ray(Eye, Vector3.forward), 0f, 6f, out Vector3 level));
            Assert.AreEqual(26f, level.z, 0.001f);

            var up = new Ray(Eye, new Vector3(-1f, 0.5f, 0f).normalized);
            Assert.IsTrue(GroundAim.TryGetPoint(up, 0f, 6f, out Vector3 point));
            Assert.AreEqual(4f, point.x, 0.001f);
            Assert.AreEqual(0f, point.y, 0.001f);
        }

        [Test]
        public void Aim_StraightDown_IsTheFeet_AndStraightUp_IsNothing()
        {
            Assert.IsTrue(GroundAim.TryGetPoint(new Ray(Eye, Vector3.down), 0f, 6f, out Vector3 feet));
            Assert.AreEqual(10f, feet.x, 0.001f);
            Assert.AreEqual(20f, feet.z, 0.001f);

            Assert.IsFalse(GroundAim.TryGetPoint(new Ray(Eye, Vector3.up), 0f, 6f, out _));
        }

        [Test]
        public void CarLoot_NeverGivesMoreThanOneWeapon()
        {
            var created = new List<Object>();
            ItemDefinition NewItem(string id, ItemKind kind)
            {
                var item = ScriptableObject.CreateInstance<ItemDefinition>();
                item.Configure(id, kind, 99);
                created.Add(item);
                return item;
            }

            var table = ScriptableObject.CreateInstance<LootTable>();
            created.Add(table);
            table.Configure(0, 0, 3, null, new List<LootTable.Entry>
            {
                new LootTable.Entry(NewItem("escopeta", ItemKind.Weapon), 1, 1, 5f),
                new LootTable.Entry(NewItem("uzi", ItemKind.Weapon), 1, 1, 5f),
                new LootTable.Entry(NewItem("pistola", ItemKind.Weapon), 1, 1, 5f),
                new LootTable.Entry(NewItem("semillas", ItemKind.Seed), 1, 3, 1f),
                new LootTable.Entry(NewItem("cinta", ItemKind.Consumable), 1, 2, 1f),
            }, weaponLimit: 1);

            var results = new List<ItemStack>();
            int withWeapon = 0;
            for (int seed = 0; seed < 500; seed++)
            {
                table.Roll(new System.Random(seed), results);
                int weapons = 0;
                foreach (ItemStack stack in results)
                {
                    if (stack.Item.Kind == ItemKind.Weapon) weapons++;
                }

                Assert.IsTrue(weapons <= 1, $"Semilla {seed}: {weapons} armas.");
                Assert.AreEqual(3, results.Count, "Las tiradas que no pueden ser arma salen de lo demás.");
                if (weapons == 1) withWeapon++;
            }

            Assert.IsTrue(withWeapon > 400, "Con tanto peso, casi siempre sale un arma (pero solo una).");
            foreach (Object item in created) Object.DestroyImmediate(item);
        }
    }
}
