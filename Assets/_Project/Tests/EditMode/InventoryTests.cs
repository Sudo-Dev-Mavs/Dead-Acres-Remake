using System.Collections.Generic;
using DeadAcres.Items;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class InventoryTests
    {
        private ItemDefinition seeds;
        private ItemDefinition hoe;

        [SetUp]
        public void SetUp()
        {
            seeds = ScriptableObject.CreateInstance<ItemDefinition>();
            seeds.Configure("seed_test", "Semillas", ItemCategory.Seed, 10, Color.green);
            hoe = ScriptableObject.CreateInstance<ItemDefinition>();
            hoe.Configure("tool_test", "Azada", ItemCategory.Tool, 1, Color.gray, ToolKind.Hoe);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(seeds);
            Object.DestroyImmediate(hoe);
        }

        [Test]
        public void ItemStack_WithoutItemOrCount_IsEmpty()
        {
            Assert.IsTrue(new ItemStack(seeds, 0).IsEmpty);
            Assert.IsTrue(new ItemStack(null, 5).IsEmpty);
            Assert.IsTrue(ItemStack.Empty.IsEmpty);
            Assert.IsFalse(new ItemStack(seeds, 1).IsEmpty);
        }

        [Test]
        public void Add_FillsExistingStacksBeforeUsingEmptySlots()
        {
            var inventory = new Inventory(3);
            inventory.Add(seeds, 7);
            inventory.Add(hoe, 1);

            int leftover = inventory.Add(seeds, 5);

            Assert.AreEqual(0, leftover);
            Assert.AreEqual(10, inventory.GetSlot(0).Count, "La primera pila se llena hasta el máximo.");
            Assert.AreEqual(2, inventory.GetSlot(2).Count, "El resto va a la siguiente casilla vacía.");
            Assert.AreEqual(12, inventory.CountOf(seeds));
        }

        [Test]
        public void Add_ReturnsWhatDidNotFit()
        {
            var inventory = new Inventory(2);

            int leftover = inventory.Add(seeds, 25);

            Assert.AreEqual(5, leftover);
            Assert.AreEqual(20, inventory.CountOf(seeds));
            Assert.AreEqual(0, inventory.GetFreeSpaceFor(seeds));
        }

        [Test]
        public void TryAdd_IsAllOrNothing()
        {
            var inventory = new Inventory(1);
            inventory.Add(seeds, 8);

            Assert.IsFalse(inventory.TryAdd(seeds, 3));
            Assert.AreEqual(8, inventory.CountOf(seeds), "Si no cabe todo, no entra nada.");
            Assert.IsTrue(inventory.TryAdd(seeds, 2));
            Assert.IsFalse(inventory.CanAdd(hoe, 1), "Sin casillas libres no cabe otro tipo de ítem.");
        }

        [Test]
        public void NonStackableItems_UseOneSlotEach()
        {
            var inventory = new Inventory(3);

            int leftover = inventory.Add(hoe, 4);

            Assert.AreEqual(1, leftover);
            Assert.AreEqual(1, inventory.GetSlot(0).Count);
            Assert.AreEqual(1, inventory.GetSlot(2).Count);
        }

        [Test]
        public void TryRemoveAt_TakesFromThatSlotOnly_AndEmptiesIt()
        {
            var inventory = new Inventory(2);
            inventory.Add(seeds, 12);

            Assert.IsFalse(inventory.TryRemoveAt(1, 3), "La casilla 1 solo tiene 2.");
            Assert.IsTrue(inventory.TryRemoveAt(1, 2));

            Assert.IsTrue(inventory.GetSlot(1).IsEmpty);
            Assert.IsNull(inventory.GetSlot(1).Item);
            Assert.AreEqual(10, inventory.GetSlot(0).Count);
        }

        [Test]
        public void TryRemove_SpansSlots_AndIsAllOrNothing()
        {
            var inventory = new Inventory(3);
            inventory.Add(seeds, 15);

            Assert.IsFalse(inventory.TryRemove(seeds, 16));
            Assert.AreEqual(15, inventory.CountOf(seeds));

            Assert.IsTrue(inventory.TryRemove(seeds, 7));
            Assert.AreEqual(8, inventory.CountOf(seeds));
            Assert.AreEqual(8, inventory.GetSlot(0).Count, "Se gasta primero la última pila.");
            Assert.IsTrue(inventory.GetSlot(1).IsEmpty);
        }

        [Test]
        public void Move_SwapsDifferentItems_AndMovesIntoEmptySlots()
        {
            var inventory = new Inventory(3);
            inventory.Add(seeds, 4);
            inventory.Add(hoe, 1);

            Assert.IsTrue(inventory.Move(0, 1));
            Assert.AreSame(hoe, inventory.GetSlot(0).Item);
            Assert.AreSame(seeds, inventory.GetSlot(1).Item);

            Assert.IsTrue(inventory.Move(1, 2));
            Assert.IsTrue(inventory.GetSlot(1).IsEmpty);
            Assert.AreEqual(4, inventory.GetSlot(2).Count);

            Assert.IsFalse(inventory.Move(1, 0), "No se puede mover una casilla vacía.");
        }

        [Test]
        public void Move_MergesSameItem_LeavingWhatDoesNotFit()
        {
            var inventory = new Inventory(2);
            inventory.Add(seeds, 16); // 10 + 6
            inventory.TryRemoveAt(0, 3); // 7 + 6

            Assert.IsTrue(inventory.Move(1, 0));

            Assert.AreEqual(10, inventory.GetSlot(0).Count);
            Assert.AreEqual(3, inventory.GetSlot(1).Count);
            Assert.IsFalse(inventory.Move(1, 0), "La pila destino ya está llena.");
        }

        [Test]
        public void SlotChanged_ReportsEveryTouchedSlot()
        {
            var inventory = new Inventory(3);
            var changed = new List<int>();
            inventory.SlotChanged += changed.Add;

            inventory.Add(seeds, 12);

            Assert.AreEqual(2, changed.Count);
            Assert.AreEqual(0, changed[0]);
            Assert.AreEqual(1, changed[1]);
        }
    }
}
