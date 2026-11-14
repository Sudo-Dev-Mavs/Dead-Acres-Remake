using System.Collections.Generic;
using DeadAcres.Farming;
using DeadAcres.GameMode;
using DeadAcres.Items;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class FarmFieldTests
    {
        private static readonly Vector2Int Cell = new Vector2Int(2, 3);

        private ItemDefinition seed;
        private ItemDefinition fruit;
        private CropDefinition regrowing;
        private CropDefinition singleHarvest;
        private GameModeRules rules;
        private FarmField field;
        private List<Vector2Int> changes;

        [SetUp]
        public void SetUp()
        {
            seed = ScriptableObject.CreateInstance<ItemDefinition>();
            seed.Configure("seed_test", "Semillas", ItemCategory.Seed, 99, Color.green);
            fruit = ScriptableObject.CreateInstance<ItemDefinition>();
            fruit.Configure("produce_test", "Fruto", ItemCategory.Produce, 10, Color.red);

            // 40 s hasta la primera cosecha, 20 s de rebrote, 2-3 frutos.
            regrowing = ScriptableObject.CreateInstance<CropDefinition>();
            regrowing.Configure("regrowing", "Rebrota", seed, fruit, 40f, 20f, 2, 3, Color.red, 0.5f);
            singleHarvest = ScriptableObject.CreateInstance<CropDefinition>();
            singleHarvest.Configure("single", "Única", seed, fruit, 40f, 0f, 1, 1, Color.red, 0.5f);

            rules = ScriptableObject.CreateInstance<GameModeRules>();
            rules.Configure("Test", plants: 2, objects: 2, nights: 5);

            field = new FarmField(new RectInt(0, 0, 8, 8), new PlacementLimiter(rules), new System.Random(1234));
            changes = new List<Vector2Int>();
            field.PlotChanged += changes.Add;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(seed);
            Object.DestroyImmediate(fruit);
            Object.DestroyImmediate(regrowing);
            Object.DestroyImmediate(singleHarvest);
            Object.DestroyImmediate(rules);
        }

        [Test]
        public void Till_WorksOnceAndOnlyInsideBounds()
        {
            Assert.AreEqual(FarmActionResult.Ok, field.Till(Cell));
            Assert.AreEqual(FarmActionResult.AlreadyTilled, field.Till(Cell));
            Assert.AreEqual(FarmActionResult.OutOfBounds, field.Till(new Vector2Int(8, 0)));
            Assert.AreEqual(FarmActionResult.OutOfBounds, field.Till(new Vector2Int(-1, 0)));

            Assert.AreEqual(1, field.PlotCount);
            Assert.AreEqual(1, changes.Count);
            Assert.IsTrue(field.TryGetPlot(Cell, out PlotState plot));
            Assert.AreEqual(PlotStage.Tilled, plot.Stage);
            Assert.IsNull(plot.Crop);
        }

        [Test]
        public void Plant_NeedsTilledEmptyPlot()
        {
            Assert.AreEqual(FarmActionResult.NotTilled, field.Plant(Cell, regrowing));

            field.Till(Cell);
            Assert.AreEqual(FarmActionResult.InvalidCrop, field.Plant(Cell, null));
            Assert.AreEqual(FarmActionResult.Ok, field.Plant(Cell, regrowing));
            Assert.AreEqual(FarmActionResult.Occupied, field.Plant(Cell, singleHarvest));

            field.TryGetPlot(Cell, out PlotState plot);
            Assert.AreEqual(PlotStage.Growing, plot.Stage);
            Assert.AreSame(regrowing, plot.Crop);
            Assert.AreEqual(0, plot.GrowthStep);
        }

        [Test]
        public void Crop_GrowsInSteps_AndRipensAtGrowSeconds()
        {
            field.Till(Cell);
            field.Plant(Cell, regrowing);
            changes.Clear();

            field.Tick(5f);
            Assert.IsEmpty(changes, "Dentro del mismo paso de crecimiento no se emiten eventos.");

            field.Tick(6f); // 11 s de 40 → paso 1
            field.TryGetPlot(Cell, out PlotState plot);
            Assert.AreEqual(1, plot.GrowthStep);
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(11f / 40f, plot.Progress, 0.0001f);

            field.Tick(28f); // 39 s → paso 3, aún creciendo
            field.TryGetPlot(Cell, out plot);
            Assert.AreEqual(PlotStage.Growing, plot.Stage);
            Assert.AreEqual(FarmField.GrowthSteps - 1, plot.GrowthStep);
            Assert.AreEqual(FarmActionResult.NotRipe, field.CanHarvest(Cell));

            field.Tick(1f); // 40 s → madura
            field.TryGetPlot(Cell, out plot);
            Assert.AreEqual(PlotStage.Ripe, plot.Stage);
            Assert.AreEqual(1f, plot.Progress, 0.0001f);
            Assert.AreEqual(FarmActionResult.Ok, field.CanHarvest(Cell));
        }

        [Test]
        public void Harvest_RegrowingCrop_KeepsPlantAndRipensAgain()
        {
            field.Till(Cell);
            field.Plant(Cell, regrowing);
            field.Tick(40f);

            Assert.AreEqual(FarmActionResult.Ok, field.Harvest(Cell, out ItemDefinition produce, out int amount));
            Assert.AreSame(fruit, produce);
            Assert.IsTrue(amount >= 2 && amount <= 3, $"Rendimiento fuera de rango: {amount}");

            field.TryGetPlot(Cell, out PlotState plot);
            Assert.AreEqual(PlotStage.Growing, plot.Stage);
            Assert.AreSame(regrowing, plot.Crop);
            Assert.AreEqual(FarmField.GrowthSteps - 1, plot.GrowthStep, "Al rebrotar la planta conserva su tamaño.");
            Assert.AreEqual(FarmActionResult.NotRipe, field.Harvest(Cell, out _, out _));

            field.Tick(19f);
            Assert.AreEqual(FarmActionResult.NotRipe, field.CanHarvest(Cell));
            field.Tick(1f);
            Assert.AreEqual(FarmActionResult.Ok, field.CanHarvest(Cell), "Rebrota en RegrowSeconds, no en GrowSeconds.");
        }

        [Test]
        public void Harvest_SingleHarvestCrop_LeavesTilledSoil_AndFreesThePlantSlot()
        {
            var limiter = new PlacementLimiter(rules);
            field = new FarmField(new RectInt(0, 0, 8, 8), limiter);
            field.Till(Cell);
            field.Plant(Cell, singleHarvest);
            Assert.AreEqual(1, limiter.GetCount(PlaceableCategory.Plant));

            field.Tick(40f);
            Assert.AreEqual(FarmActionResult.Ok, field.Harvest(Cell, out _, out int amount));
            Assert.AreEqual(1, amount);

            field.TryGetPlot(Cell, out PlotState plot);
            Assert.AreEqual(PlotStage.Tilled, plot.Stage);
            Assert.IsNull(plot.Crop);
            Assert.AreEqual(0, limiter.GetCount(PlaceableCategory.Plant));
            Assert.AreEqual(FarmActionResult.NothingPlanted, field.Harvest(Cell, out _, out _));
        }

        [Test]
        public void Plant_RespectsThePlantLimit_AndClearCropFreesIt()
        {
            var a = new Vector2Int(0, 0);
            var b = new Vector2Int(1, 0);
            var c = new Vector2Int(2, 0);
            field.Till(a);
            field.Till(b);
            field.Till(c);

            Assert.AreEqual(FarmActionResult.Ok, field.Plant(a, regrowing));
            Assert.AreEqual(FarmActionResult.Ok, field.Plant(b, regrowing));
            Assert.AreEqual(FarmActionResult.PlantLimitReached, field.Plant(c, regrowing), "El límite del modo es 2.");

            Assert.IsTrue(field.ClearCrop(a));
            Assert.IsFalse(field.ClearCrop(a), "Ya no hay planta que arrancar.");
            Assert.AreEqual(FarmActionResult.Ok, field.Plant(c, regrowing));
        }

        [Test]
        public void ClearedCrop_StopsGrowing()
        {
            field.Till(Cell);
            field.Plant(Cell, regrowing);
            field.ClearCrop(Cell);
            changes.Clear();

            field.Tick(100f);

            Assert.IsEmpty(changes);
            field.TryGetPlot(Cell, out PlotState plot);
            Assert.AreEqual(PlotStage.Tilled, plot.Stage);
        }

        [Test]
        public void Tick_ListenerMayHarvestWhileEventsAreRaised()
        {
            var other = new Vector2Int(5, 5);
            field.Till(Cell);
            field.Till(other);
            field.Plant(Cell, regrowing);
            field.Plant(other, regrowing);

            int harvested = 0;
            field.PlotChanged += cell =>
            {
                if (field.CanHarvest(cell) == FarmActionResult.Ok)
                {
                    field.Harvest(cell, out _, out _);
                    harvested++;
                }
            };

            field.Tick(40f);

            Assert.AreEqual(2, harvested, "Las dos maduran en el mismo tick y ambas se cosechan.");
            Assert.AreEqual(FarmActionResult.NotRipe, field.CanHarvest(Cell));
            Assert.AreEqual(FarmActionResult.NotRipe, field.CanHarvest(other));
        }

        [Test]
        public void GetCells_ListsEveryTilledCell()
        {
            field.Till(Cell);
            field.Till(new Vector2Int(0, 0));
            var cells = new List<Vector2Int>();

            field.GetCells(cells);

            Assert.AreEqual(2, cells.Count);
            Assert.IsTrue(cells.Contains(Cell));
        }

        [Test]
        public void PlantFromSlot_SpendsOneSeed_OnlyWhenThePlantingSucceeds()
        {
            var inventory = new Inventory(2);
            inventory.Add(seed, 3);
            inventory.Add(fruit, 1);

            Assert.AreEqual(FarmActionResult.NotTilled,
                FarmCommands.PlantFromSlot(field, Cell, regrowing, inventory, 0));
            Assert.AreEqual(3, inventory.CountOf(seed), "Un intento fallido no gasta semilla.");

            field.Till(Cell);
            Assert.AreEqual(FarmActionResult.MissingSeed,
                FarmCommands.PlantFromSlot(field, Cell, regrowing, inventory, 1), "La casilla 1 tiene fruto, no semilla.");
            Assert.AreEqual(FarmActionResult.MissingSeed,
                FarmCommands.PlantFromSlot(field, Cell, regrowing, inventory, 7), "Casilla inexistente.");

            Assert.AreEqual(FarmActionResult.Ok, FarmCommands.PlantFromSlot(field, Cell, regrowing, inventory, 0));
            Assert.AreEqual(2, inventory.CountOf(seed));
            Assert.AreEqual(FarmActionResult.Occupied,
                FarmCommands.PlantFromSlot(field, Cell, regrowing, inventory, 0));
            Assert.AreEqual(2, inventory.CountOf(seed));
        }

        [Test]
        public void HarvestInto_StoresTheProduce()
        {
            var inventory = new Inventory(2);
            field.Till(Cell);
            field.Plant(Cell, regrowing);
            field.Tick(40f);

            FarmActionResult result = FarmCommands.HarvestInto(field, Cell, inventory, out ItemDefinition produce, out int amount);

            Assert.AreEqual(FarmActionResult.Ok, result);
            Assert.AreSame(fruit, produce);
            Assert.AreEqual(amount, inventory.CountOf(fruit));
        }

        [Test]
        public void HarvestInto_WithoutRoom_LeavesTheCropRipe()
        {
            var inventory = new Inventory(1);
            inventory.Add(fruit, 8); // caben 2, pero el cultivo puede dar 3
            field.Till(Cell);
            field.Plant(Cell, regrowing);
            field.Tick(40f);

            FarmActionResult result = FarmCommands.HarvestInto(field, Cell, inventory, out _, out int amount);

            Assert.AreEqual(FarmActionResult.InventoryFull, result);
            Assert.AreEqual(0, amount);
            Assert.AreEqual(8, inventory.CountOf(fruit));
            Assert.AreEqual(FarmActionResult.Ok, field.CanHarvest(Cell), "No se pierde la cosecha.");
        }
    }
}
