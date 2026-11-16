using DeadAcres.Building;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class NoBuildZoneAndPowerRangeTests
    {
        private static PlacementGrid NewGrid() => new PlacementGrid(300, 300, new Vector3(-150f, 0f, -150f));

        [Test]
        public void CellsCovering_IncludesEveryCellTheBoxTouches_PlusTheMargin()
        {
            PlacementGrid grid = NewGrid();

            // La caravana del greybox: de (-8.3, -5.5) a (-5.7, 1.5).
            RectInt footprint = grid.CellsCovering(new Vector3(-8.3f, 0f, -5.5f), new Vector3(-5.7f, 2.8f, 1.5f));
            Assert.AreEqual(new RectInt(141, 144, 4, 8), footprint);

            RectInt zone = grid.CellsCovering(new Vector3(-8.3f, 0f, -5.5f), new Vector3(-5.7f, 2.8f, 1.5f), 3);
            Assert.AreEqual(new RectInt(138, 141, 10, 14), zone);
        }

        [Test]
        public void CellsCovering_AnEdgeOnACellLine_DoesNotTouchTheNextCell()
        {
            PlacementGrid grid = NewGrid();
            RectInt cells = grid.CellsCovering(new Vector3(2f, 0f, 4f), new Vector3(4f, 1f, 5f));
            Assert.AreEqual(new RectInt(152, 154, 2, 1), cells);
        }

        [Test]
        public void Reserve_BlocksTheZone_AndOnlyTheZone()
        {
            PlacementGrid grid = NewGrid();
            var zone = new RectInt(138, 141, 10, 14);
            int id = grid.Reserve(zone);

            Assert.AreNotEqual(PlacementGrid.NoOccupant, id);
            Assert.IsFalse(grid.IsFree(new RectInt(138, 141, 1, 1)), "Esquina de la zona.");
            Assert.IsFalse(grid.IsFree(new RectInt(147, 154, 1, 1)), "Esquina opuesta.");
            Assert.IsFalse(grid.IsFree(new RectInt(137, 141, 2, 1)), "Un objeto de 2 celdas con una dentro no cabe.");
            Assert.IsTrue(grid.IsFree(new RectInt(137, 141, 1, 1)), "La celda de al lado queda libre.");
            Assert.IsTrue(grid.IsFree(new RectInt(148, 155, 1, 1)));
            Assert.IsTrue(grid.IsFree(new RectInt(0, 0, 1, 1)), "El resto del suelo es construible.");
            Assert.IsTrue(grid.IsFree(new RectInt(299, 299, 1, 1)));

            Assert.IsTrue(grid.Release(id));
            Assert.IsTrue(grid.IsFree(zone));
        }

        [Test]
        public void Reserve_ClipsToTheGrid()
        {
            var grid = new PlacementGrid(10, 10, Vector3.zero);
            int id = grid.Reserve(new RectInt(-3, -3, 5, 5));

            Assert.AreNotEqual(PlacementGrid.NoOccupant, id);
            Assert.IsFalse(grid.IsFree(new RectInt(1, 1, 1, 1)));
            Assert.IsTrue(grid.IsFree(new RectInt(2, 2, 1, 1)));
            Assert.AreEqual(PlacementGrid.NoOccupant, grid.Reserve(new RectInt(20, 20, 3, 3)), "Fuera del todo: nada que reservar.");
        }

        [Test]
        public void PowerRange_WithExtents_IsMeasuredFromTheEdge()
        {
            var power = new PowerMap();

            // Caravana de 2.6 × 7 centrada en (-7, -2), 6 m de alcance desde su borde.
            power.Add(new Vector3(-7f, 1.4f, -2f), 6f, new Vector2(1.3f, 3.5f));

            Assert.IsTrue(power.IsPowered(new Vector3(-7f, 0f, -2f)), "Dentro de la caravana.");
            Assert.IsTrue(power.IsPowered(new Vector3(-5.7f + 5.9f, 0f, -2f)), "A 5.9 m del costado.");
            Assert.IsFalse(power.IsPowered(new Vector3(-5.7f + 6.1f, 0f, -2f)), "A 6.1 m del costado.");
            Assert.IsTrue(power.IsPowered(new Vector3(-7f, 0f, 1.5f + 5.9f)), "A 5.9 m del frente.");
            Assert.IsFalse(power.IsPowered(new Vector3(-7f, 0f, 1.5f + 6.1f)), "A 6.1 m del frente.");

            // En diagonal desde la esquina cuenta la distancia real, no la de cada eje.
            Assert.IsFalse(power.IsPowered(new Vector3(-5.7f + 5f, 0f, 1.5f + 5f)));
            Assert.IsTrue(power.IsPowered(new Vector3(-5.7f + 4f, 0f, 1.5f + 4f)));
        }

        [Test]
        public void PowerRange_WithoutExtents_IsStillMeasuredFromTheCenter()
        {
            var power = new PowerMap();
            power.Add(Vector3.zero, 9f);

            Assert.IsTrue(power.IsPowered(new Vector3(8.9f, 0f, 0f)));
            Assert.IsFalse(power.IsPowered(new Vector3(9.1f, 0f, 0f)));
        }
    }
}
