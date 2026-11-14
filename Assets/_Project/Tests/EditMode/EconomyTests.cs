using System.Collections.Generic;
using DeadAcres.Economy;
using DeadAcres.Items;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class EconomyTests
    {
        private ItemDefinition seed;
        private ItemDefinition lateSeed;
        private ItemDefinition fruit;
        private ItemDefinition hoe;
        private ShopCatalog catalog;
        private Wallet wallet;
        private Inventory inventory;

        [SetUp]
        public void SetUp()
        {
            seed = CreateItem("Semillas", ItemKind.Seed, 99, buy: 5, sell: 0);
            lateSeed = CreateItem("Semillas raras", ItemKind.Seed, 99, buy: 12, sell: 0);
            fruit = CreateItem("Fruto", ItemKind.Produce, 10, buy: 0, sell: 3);
            hoe = CreateItem("Azada", ItemKind.Tool, 1, buy: 0, sell: 0);

            catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            catalog.Configure(new List<KeyValuePair<ItemDefinition, int>>
            {
                new KeyValuePair<ItemDefinition, int>(seed, 1),
                new KeyValuePair<ItemDefinition, int>(lateSeed, 3),
                new KeyValuePair<ItemDefinition, int>(hoe, 1), // en catálogo pero sin precio de compra
            });

            wallet = new Wallet(20);
            inventory = new Inventory(0); // 9 ranuras
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(seed);
            Object.DestroyImmediate(lateSeed);
            Object.DestroyImmediate(fruit);
            Object.DestroyImmediate(hoe);
            Object.DestroyImmediate(catalog);
        }

        private static ItemDefinition CreateItem(string name, ItemKind kind, int stack, int buy, int sell)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Configure(name, kind, stack);
            item.ConfigurePrices(buy, sell);
            return item;
        }

        [Test]
        public void Wallet_SpendsAllOrNothing_AndNeverGoesNegative()
        {
            var changes = new List<int>();
            wallet.Changed += changes.Add;

            Assert.IsFalse(wallet.TrySpend(21));
            Assert.AreEqual(20, wallet.Gold);
            Assert.IsTrue(wallet.TrySpend(20));
            Assert.AreEqual(0, wallet.Gold);
            Assert.IsFalse(wallet.TrySpend(-5), "Un importe negativo no es un gasto.");

            wallet.Add(-10);
            wallet.Add(7);

            Assert.AreEqual(7, wallet.Gold);
            Assert.AreEqual(2, changes.Count, "Solo avisan el gasto y el ingreso reales.");
            Assert.AreEqual(0, new Wallet(-3).Gold);
        }

        [Test]
        public void Catalog_GrowsWithTheDays()
        {
            var day1 = new List<ItemDefinition>();
            var day3 = new List<ItemDefinition>();

            catalog.GetAvailable(1, day1);
            catalog.GetAvailable(3, day3);

            Assert.AreEqual(2, day1.Count);
            Assert.IsFalse(day1.Contains(lateSeed));
            Assert.AreEqual(3, day3.Count);
            Assert.IsTrue(catalog.TryGetAvailableFromDay(lateSeed, out int fromDay));
            Assert.AreEqual(3, fromDay);
            Assert.IsFalse(catalog.TryGetAvailableFromDay(fruit, out _));
        }

        [Test]
        public void Buy_MovesGoldAndItems()
        {
            Assert.AreEqual(ShopResult.Ok, ShopCommands.Buy(catalog, seed, 3, 1, wallet, inventory));

            Assert.AreEqual(5, wallet.Gold);
            Assert.AreEqual(3, inventory.CountOf(seed));
        }

        [Test]
        public void Buy_Rejected_ChangesNothing()
        {
            Assert.AreEqual(ShopResult.NotEnoughGold, ShopCommands.Buy(catalog, seed, 5, 1, wallet, inventory));
            Assert.AreEqual(ShopResult.NotAvailableYet, ShopCommands.Buy(catalog, lateSeed, 1, 2, wallet, inventory));
            Assert.AreEqual(ShopResult.NotForSale, ShopCommands.Buy(catalog, fruit, 1, 1, wallet, inventory), "No está en el catálogo.");
            Assert.AreEqual(ShopResult.NotForSale, ShopCommands.Buy(catalog, hoe, 1, 1, wallet, inventory), "Sin precio de compra.");
            Assert.AreEqual(ShopResult.NotForSale, ShopCommands.Buy(catalog, seed, 0, 1, wallet, inventory));

            inventory.Add(fruit, 90); // llena las 9 ranuras
            Assert.AreEqual(ShopResult.InventoryFull, ShopCommands.Buy(catalog, seed, 1, 1, wallet, inventory));

            Assert.AreEqual(20, wallet.Gold);
            Assert.AreEqual(0, inventory.CountOf(seed));
        }

        [Test]
        public void Buy_LateItem_WorksFromItsDay()
        {
            Assert.AreEqual(ShopResult.Ok, ShopCommands.Buy(catalog, lateSeed, 1, 3, wallet, inventory));
            Assert.AreEqual(8, wallet.Gold);
        }

        [Test]
        public void Sell_PaysTheSellPrice_AndTakesTheItems()
        {
            inventory.Add(fruit, 14); // 10 + 4

            Assert.AreEqual(ShopResult.Ok, ShopCommands.Sell(fruit, 12, wallet, inventory));

            Assert.AreEqual(20 + 36, wallet.Gold);
            Assert.AreEqual(2, inventory.CountOf(fruit));
        }

        [Test]
        public void Sell_Rejected_ChangesNothing()
        {
            inventory.Add(fruit, 2);
            inventory.Add(seed, 4);

            Assert.AreEqual(ShopResult.NothingToSell, ShopCommands.Sell(fruit, 3, wallet, inventory));
            Assert.AreEqual(ShopResult.NotSellable, ShopCommands.Sell(seed, 1, wallet, inventory));
            Assert.AreEqual(ShopResult.NothingToSell, ShopCommands.Sell(null, 1, wallet, inventory));

            Assert.AreEqual(20, wallet.Gold);
            Assert.AreEqual(2, inventory.CountOf(fruit));
            Assert.AreEqual(4, inventory.CountOf(seed));
        }

        [Test]
        public void InventoryRemove_SpansStacks_FromTheBackpackFirst_AllOrNothing()
        {
            var bag = new Inventory(2); // 9 de hotbar + 2 de mochila
            bag.Add(fruit, 25); // ranuras 0, 1 y 2

            Assert.IsFalse(bag.Remove(fruit, 26));
            Assert.AreEqual(25, bag.CountOf(fruit));

            Assert.IsTrue(bag.Remove(fruit, 7));

            Assert.AreEqual(18, bag.CountOf(fruit));
            Assert.AreEqual(10, bag[0].Count, "Las primeras pilas (hotbar) se tocan al final.");
            Assert.AreEqual(8, bag[1].Count);
            Assert.IsTrue(bag[2].IsEmpty);
        }
    }
}
