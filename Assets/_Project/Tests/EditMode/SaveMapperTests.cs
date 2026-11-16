using System.Collections.Generic;
using DeadAcres.Building;
using DeadAcres.Economy;
using DeadAcres.Farming;
using DeadAcres.Items;
using DeadAcres.Saving;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class SaveMapperTests
    {
        private readonly List<Object> created = new List<Object>();
        private ItemDefinition pistol;
        private ItemDefinition seeds;
        private ItemDefinition fruit;
        private CropDefinition regrowing;
        private CropDefinition singleHarvest;
        private ItemCatalog items;
        private CropCatalog crops;

        [SetUp]
        public void SetUp()
        {
            pistol = NewItem("weapon_pistol", "Pistola", ItemKind.Weapon, 1);
            seeds = NewItem("seed_test", "Semillas", ItemKind.Seed, 99);
            fruit = NewItem("produce_test", "Fruto", ItemKind.Produce, 20);

            regrowing = ScriptableObject.CreateInstance<CropDefinition>();
            regrowing.Configure("regrowing", "Rebrota", seeds, fruit, 40f, 20f, 2, 3, Color.red, 0.5f);
            singleHarvest = ScriptableObject.CreateInstance<CropDefinition>();
            singleHarvest.Configure("single", "Única", seeds, fruit, 40f, 0f, 1, 1, Color.red, 0.5f);
            created.Add(regrowing);
            created.Add(singleHarvest);

            items = ScriptableObject.CreateInstance<ItemCatalog>();
            items.Configure(new[] { pistol, seeds, fruit });
            crops = ScriptableObject.CreateInstance<CropCatalog>();
            crops.Configure(new[] { regrowing, singleHarvest });
            created.Add(items);
            created.Add(crops);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in created) Object.DestroyImmediate(item);
            created.Clear();
        }

        [Test]
        public void Inventory_RoundTrip_KeepsSlotsCountsAndSelection()
        {
            var original = new Inventory(24);
            original.Add(pistol, 1);
            original.Add(seeds, 130);
            original.Move(1, 20);
            original.Add(fruit, 7);
            original.Select(2);

            var data = new SaveData();
            SaveMapper.CaptureInventory(original, data);

            var restored = new Inventory(24);
            restored.Add(fruit, 3);
            Assert.AreEqual(0, SaveMapper.RestoreInventory(data, restored, items));

            Assert.AreEqual(original.SlotCount, restored.SlotCount);
            for (int i = 0; i < original.SlotCount; i++)
            {
                Assert.AreSame(original[i].Item, restored[i].Item, $"Ranura {i}");
                Assert.AreEqual(original[i].Count, restored[i].Count, $"Ranura {i}");
            }

            Assert.AreEqual(2, restored.SelectedIndex);
            Assert.AreEqual(130, restored.CountOf(seeds));
            Assert.AreEqual(7, restored.CountOf(fruit), "Lo que hubiera antes de cargar se descarta.");
        }

        [Test]
        public void Inventory_UnknownItem_IsSkippedAndCounted()
        {
            var data = new SaveData();
            data.slots.Add(new SlotSave { index = 0, item = "weapon_pistol", count = 1 });
            data.slots.Add(new SlotSave { index = 1, item = "ya_no_existe", count = 5 });
            data.slots.Add(new SlotSave { index = 999, item = "seed_test", count = 5 });

            var inventory = new Inventory(24);
            Assert.AreEqual(2, SaveMapper.RestoreInventory(data, inventory, items));
            Assert.AreSame(pistol, inventory[0].Item);
            Assert.IsTrue(inventory[1].IsEmpty);
        }

        [Test]
        public void Plots_RoundTrip_KeepsGrowthRipenessAndRegrowth()
        {
            var grid = new PlacementGrid(8, 8, Vector3.zero);
            var field = new FarmField(new RectInt(0, 0, 8, 8), null, new System.Random(1), grid);

            var tilledOnly = new Vector2Int(0, 0);
            var halfGrown = new Vector2Int(1, 0);
            var ripe = new Vector2Int(2, 0);
            var regrowingPlot = new Vector2Int(3, 0);

            field.Till(tilledOnly);
            field.Till(ripe);
            field.Plant(ripe, singleHarvest);
            field.Till(regrowingPlot);
            field.Plant(regrowingPlot, regrowing);
            field.Tick(40f);
            field.Harvest(regrowingPlot, out _, out _);
            field.Tick(5f);
            field.Till(halfGrown);
            field.Plant(halfGrown, regrowing);
            field.Tick(10f);

            var data = new SaveData();
            SaveMapper.CapturePlots(field, data);
            Assert.AreEqual(4, data.plots.Count);

            var restoredGrid = new PlacementGrid(8, 8, Vector3.zero);
            var restored = new FarmField(new RectInt(0, 0, 8, 8), null, new System.Random(1), restoredGrid);
            Assert.AreEqual(0, SaveMapper.RestorePlots(data, restored, crops));

            Assert.IsTrue(restored.TryGetPlot(tilledOnly, out PlotState state));
            Assert.AreEqual(PlotStage.Tilled, state.Stage);
            Assert.IsNull(state.Crop);
            Assert.IsTrue(restoredGrid.IsFree(new RectInt(tilledOnly, Vector2Int.one)), "La tierra arada no ocupa celda.");

            Assert.IsTrue(restored.TryGetPlot(ripe, out state));
            Assert.AreEqual(PlotStage.Ripe, state.Stage);
            Assert.AreSame(singleHarvest, state.Crop);
            Assert.AreEqual(FarmActionResult.Ok, restored.CanHarvest(ripe));
            Assert.IsFalse(restoredGrid.IsFree(new RectInt(ripe, Vector2Int.one)), "La planta vuelve a ocupar su celda.");

            // Media planta: llevaba 10 de 40 s. Con 29 s más sigue creciendo; con 2 más, madura.
            Assert.IsTrue(restored.TryGetPlot(halfGrown, out state));
            Assert.AreEqual(PlotStage.Growing, state.Stage);
            Assert.AreEqual(0.25f, state.Progress, 0.001f);
            restored.Tick(29f);
            Assert.AreEqual(FarmActionResult.NotRipe, restored.CanHarvest(halfGrown));

            // El rebrote llevaba 5 + 10 de 20 s y sigue contando como rebrote (no como planta nueva).
            Assert.IsTrue(restored.TryGetPlot(regrowingPlot, out state));
            Assert.AreEqual(PlotStage.Ripe, state.Stage, "15 + 29 s de un rebrote de 20 s: ya maduró.");

            restored.Tick(2f);
            Assert.AreEqual(FarmActionResult.Ok, restored.CanHarvest(halfGrown));
        }

        [Test]
        public void Plots_RegrowingPlot_KeepsItsShorterTimer()
        {
            var field = new FarmField(new RectInt(0, 0, 8, 8));
            var cell = new Vector2Int(4, 4);
            field.Till(cell);
            field.Plant(cell, regrowing);
            field.Tick(40f);
            field.Harvest(cell, out _, out _);
            field.Tick(5f);

            var data = new SaveData();
            SaveMapper.CapturePlots(field, data);
            Assert.IsTrue(data.plots[0].regrowing);

            var restored = new FarmField(new RectInt(0, 0, 8, 8));
            SaveMapper.RestorePlots(data, restored, crops);
            restored.Tick(14f);
            Assert.AreEqual(FarmActionResult.NotRipe, restored.CanHarvest(cell));
            restored.Tick(2f);
            Assert.AreEqual(FarmActionResult.Ok, restored.CanHarvest(cell), "Rebrote de 20 s: 5 guardados + 16.");
        }

        [Test]
        public void Plots_UnknownCrop_KeepsTheTilledSoil()
        {
            var data = new SaveData();
            data.plots.Add(new PlotSave { x = 1, z = 1, crop = "ya_no_existe", elapsed = 3f });

            var field = new FarmField(new RectInt(0, 0, 8, 8));
            Assert.AreEqual(1, SaveMapper.RestorePlots(data, field, crops));
            Assert.IsTrue(field.TryGetPlot(new Vector2Int(1, 1), out PlotState state));
            Assert.AreEqual(PlotStage.Tilled, state.Stage);
        }

        [Test]
        public void Wallet_Restore_SetsTheExactAmount()
        {
            var wallet = new Wallet(20);
            int notified = -1;
            wallet.Changed += gold => notified = gold;

            wallet.Restore(134);
            Assert.AreEqual(134, wallet.Gold);
            Assert.AreEqual(134, notified);

            wallet.Restore(-5);
            Assert.AreEqual(0, wallet.Gold);
        }

        [Test]
        public void Grid_TryGetFootprint_ReturnsWhatWasOccupied()
        {
            var grid = new PlacementGrid(16, 16, Vector3.zero);
            int id = grid.Occupy(new RectInt(3, 4, 2, 2));

            Assert.IsTrue(grid.TryGetFootprint(id, out RectInt rect));
            Assert.AreEqual(new RectInt(3, 4, 2, 2), rect);

            grid.Release(id);
            Assert.IsFalse(grid.TryGetFootprint(id, out _));
        }

        [Test]
        public void ItemCatalog_FindsByStableId()
        {
            Assert.IsTrue(items.TryGet("seed_test", out ItemDefinition found));
            Assert.AreSame(seeds, found);
            Assert.IsFalse(items.TryGet("nada", out _));
            Assert.IsFalse(items.TryGet(null, out _));
        }

        private ItemDefinition NewItem(string id, string name, ItemKind kind, int maxStack)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Configure(name, kind, maxStack);
            item.ConfigureExtras(id, Color.white);
            created.Add(item);
            return item;
        }
    }
}
