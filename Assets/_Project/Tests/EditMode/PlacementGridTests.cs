using DeadAcres.Building;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class PlacementGridTests
    {
        [Test]
        public void WorldToCell_UsesOriginAndFloors()
        {
            var grid = new PlacementGrid(64, 64, new Vector3(-32f, 0f, -32f));

            Assert.AreEqual(new Vector2Int(32, 32), grid.WorldToCell(new Vector3(0.2f, 5f, 0.9f)));
            Assert.AreEqual(new Vector2Int(31, 31), grid.WorldToCell(new Vector3(-0.1f, 0f, -0.1f)));
        }

        [Test]
        public void RotateFootprint_SwapsOnOddQuarterTurns()
        {
            var size = new Vector2Int(3, 1);

            Assert.AreEqual(size, PlacementGrid.RotateFootprint(size, 0));
            Assert.AreEqual(new Vector2Int(1, 3), PlacementGrid.RotateFootprint(size, 1));
            Assert.AreEqual(size, PlacementGrid.RotateFootprint(size, 2));
        }

        [Test]
        public void Occupy_BlocksOverlap_AndReleaseFreesCells()
        {
            var grid = new PlacementGrid(8, 8, Vector3.zero);
            var wall = new RectInt(2, 2, 3, 1);

            int id = grid.Occupy(wall);
            Assert.AreNotEqual(PlacementGrid.NoOccupant, id);
            Assert.AreEqual(id, grid.GetOccupant(new Vector2Int(4, 2)));
            Assert.AreEqual(PlacementGrid.NoOccupant, grid.Occupy(new RectInt(4, 2, 1, 1)), "Solapa con el muro.");

            Assert.IsTrue(grid.Release(id));
            Assert.IsTrue(grid.IsFree(wall));
            Assert.AreEqual(0, grid.OccupantCount);
        }

        [Test]
        public void IsFree_RejectsOutOfBounds()
        {
            var grid = new PlacementGrid(8, 8, Vector3.zero);

            Assert.IsFalse(grid.IsFree(new RectInt(7, 7, 2, 1)));
            Assert.IsFalse(grid.IsFree(new RectInt(-1, 0, 1, 1)));
        }

        [Test]
        public void RectAround_CentersOddFootprints()
        {
            RectInt rect = PlacementGrid.RectAround(new Vector2Int(10, 10), new Vector2Int(3, 1));

            Assert.AreEqual(new Vector2Int(9, 10), rect.min);
            Assert.AreEqual(new Vector2Int(3, 1), rect.size);
        }

        [Test]
        public void RectCenterToWorld_ReturnsCellCenter()
        {
            var grid = new PlacementGrid(8, 8, new Vector3(10f, 0f, 20f));

            Vector3 center = grid.RectCenterToWorld(new RectInt(2, 3, 1, 1));

            Assert.AreEqual(new Vector3(12.5f, 0f, 23.5f), center);
        }
    }
}
