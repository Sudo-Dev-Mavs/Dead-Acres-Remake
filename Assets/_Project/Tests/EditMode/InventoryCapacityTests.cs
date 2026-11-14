using DeadAcres.Items;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class InventoryCapacityTests
    {
        private ItemDefinition stackable;
        private ItemDefinition tool;

        [SetUp]
        public void SetUp()
        {
            stackable = ScriptableObject.CreateInstance<ItemDefinition>();
            stackable.Configure("Fruto", ItemKind.Produce, 10);
            tool = ScriptableObject.CreateInstance<ItemDefinition>();
            tool.Configure("Azada", ItemKind.Tool, 1);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(stackable);
            Object.DestroyImmediate(tool);
        }

        [Test]
        public void GetFreeSpaceFor_CountsEmptySlotsAndPartialStacks()
        {
            var inventory = new Inventory(0); // 9 ranuras
            Assert.AreEqual(90, inventory.GetFreeSpaceFor(stackable));

            inventory.Add(stackable, 13); // 10 + 3
            inventory.Add(tool, 1);

            Assert.AreEqual(7 + 6 * 10, inventory.GetFreeSpaceFor(stackable));
            Assert.AreEqual(6, inventory.GetFreeSpaceFor(tool), "Una herramienta no se apila: una por ranura vacía.");
            Assert.AreEqual(0, inventory.GetFreeSpaceFor(null));
        }

        [Test]
        public void CanAdd_MatchesWhatAddActuallyFits()
        {
            var inventory = new Inventory(0);
            inventory.Add(stackable, 85);

            Assert.IsTrue(inventory.CanAdd(stackable, 5));
            Assert.IsFalse(inventory.CanAdd(stackable, 6));
            Assert.IsFalse(inventory.CanAdd(stackable, 0));
            Assert.IsFalse(inventory.CanAdd(tool, 1), "No queda ninguna ranura vacía.");

            Assert.AreEqual(1, inventory.Add(stackable, 6), "Add confirma que solo cabían 5.");
        }
    }
}
