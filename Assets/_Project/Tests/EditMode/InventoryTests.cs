using DeadAcres.Items;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class InventoryTests
    {
        private ItemDefinition seeds;
        private ItemDefinition shotgun;

        [SetUp]
        public void SetUp()
        {
            seeds = ScriptableObject.CreateInstance<ItemDefinition>();
            seeds.Configure("Semillas de trigo", ItemKind.Seed, stack: 20);
            shotgun = ScriptableObject.CreateInstance<ItemDefinition>();
            shotgun.Configure("Escopeta", ItemKind.Weapon, stack: 1);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(seeds);
            Object.DestroyImmediate(shotgun);
        }

        [Test]
        public void Add_FillsHotbarBeforeBackpack()
        {
            var inventory = new Inventory(backpackSize: 4);
            for (int i = 0; i < Inventory.HotbarSize; i++)
            {
                Assert.AreEqual(0, inventory.Add(shotgun, 1));
            }

            inventory.Add(shotgun, 1);

            for (int i = 0; i < Inventory.HotbarSize; i++)
            {
                Assert.AreSame(shotgun, inventory[i].Item, $"Ranura de hotbar {i}");
            }

            Assert.AreSame(shotgun, inventory[Inventory.HotbarSize].Item, "La décima va a la mochila.");
        }

        [Test]
        public void Add_StacksIntoExistingStackFirst()
        {
            var inventory = new Inventory();
            inventory.Add(shotgun, 1);
            inventory.Add(seeds, 15);

            inventory.Add(seeds, 10);

            Assert.AreEqual(20, inventory[1].Count, "Completa la pila existente.");
            Assert.AreEqual(5, inventory[2].Count, "El resto va a la siguiente ranura libre.");
            Assert.AreEqual(25, inventory.CountOf(seeds));
        }

        [Test]
        public void Add_ReturnsRemainderWhenFull()
        {
            var inventory = new Inventory(backpackSize: 0);

            int remainder = inventory.Add(seeds, 20 * Inventory.HotbarSize + 7);

            Assert.AreEqual(7, remainder);
        }

        [Test]
        public void RemoveAt_EmptiesSlotAtZero()
        {
            var inventory = new Inventory();
            inventory.Add(seeds, 2);

            Assert.IsTrue(inventory.RemoveAt(0, 2));
            Assert.IsTrue(inventory[0].IsEmpty);
            Assert.IsFalse(inventory.RemoveAt(0, 1));
        }

        [Test]
        public void Cycle_WrapsAround()
        {
            var inventory = new Inventory();

            inventory.Cycle(-1);
            Assert.AreEqual(8, inventory.SelectedIndex);

            inventory.Cycle(1);
            Assert.AreEqual(0, inventory.SelectedIndex);
        }

        [Test]
        public void Move_MergesSameItemAndSwapsDifferentItems()
        {
            var inventory = new Inventory();
            inventory.Add(seeds, 20);
            inventory.Add(seeds, 5);   // ranura 1
            inventory.Add(shotgun, 1); // ranura 2
            inventory.RemoveAt(0, 10); // ranura 0 = 10

            inventory.Move(1, 0);
            Assert.AreEqual(15, inventory[0].Count);
            Assert.IsTrue(inventory[1].IsEmpty);

            inventory.Move(2, 0);
            Assert.AreSame(shotgun, inventory[0].Item);
            Assert.AreSame(seeds, inventory[2].Item);
        }
    }
}
