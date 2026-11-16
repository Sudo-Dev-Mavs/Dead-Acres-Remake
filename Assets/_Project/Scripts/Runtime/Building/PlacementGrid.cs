using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>
    /// Rejilla de bloques de la zona jugable (todo el suelo en el greybox). Solo guarda ocupación:
    /// qué celdas están libres y qué objeto ocupa cada una. No sabe nada de GameObjects.
    /// </summary>
    public sealed class PlacementGrid
    {
        public const int NoOccupant = 0;

        private readonly int[] cells;
        private readonly Dictionary<int, RectInt> footprints = new Dictionary<int, RectInt>(256);
        private int nextId = 1;

        public PlacementGrid(int width, int depth, Vector3 worldOrigin, float cellSize = 1f)
        {
            if (width <= 0 || depth <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));

            Width = width;
            Depth = depth;
            WorldOrigin = worldOrigin;
            CellSize = cellSize;
            cells = new int[width * depth];
        }

        public int Width { get; }
        public int Depth { get; }
        public Vector3 WorldOrigin { get; }
        public float CellSize { get; }
        public int OccupantCount => footprints.Count;

        /// <summary>Huella con rotación en cuartos de vuelta: con 1 y 3 se intercambian X y Z.</summary>
        public static Vector2Int RotateFootprint(Vector2Int size, int quarterTurns)
        {
            return (quarterTurns & 1) == 1 ? new Vector2Int(size.y, size.x) : size;
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            Vector3 local = world - WorldOrigin;
            return new Vector2Int(Mathf.FloorToInt(local.x / CellSize), Mathf.FloorToInt(local.z / CellSize));
        }

        /// <summary>Centro en el mundo de un rectángulo de celdas (para posicionar el prefab).</summary>
        public Vector3 RectCenterToWorld(RectInt rect)
        {
            return WorldOrigin + new Vector3(rect.center.x * CellSize, 0f, rect.center.y * CellSize);
        }

        /// <summary>Rectángulo centrado en la celda apuntada (el objeto "sigue" al cursor por su centro).</summary>
        public static RectInt RectAround(Vector2Int cell, Vector2Int rotatedSize)
        {
            var min = new Vector2Int(cell.x - (rotatedSize.x - 1) / 2, cell.y - (rotatedSize.y - 1) / 2);
            return new RectInt(min, rotatedSize);
        }

        public bool IsInside(RectInt rect)
        {
            return rect.xMin >= 0 && rect.yMin >= 0 && rect.xMax <= Width && rect.yMax <= Depth;
        }

        public bool IsFree(RectInt rect)
        {
            if (!IsInside(rect)) return false;

            for (int z = rect.yMin; z < rect.yMax; z++)
            {
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    if (cells[Index(x, z)] != NoOccupant) return false;
                }
            }

            return true;
        }

        public int GetOccupant(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Depth
                ? cells[Index(cell.x, cell.y)]
                : NoOccupant;
        }

        /// <returns>Id del ocupante, o <see cref="NoOccupant"/> si no cabe.</returns>
        public int Occupy(RectInt rect)
        {
            if (!IsFree(rect)) return NoOccupant;

            int id = nextId++;
            Fill(rect, id);
            footprints.Add(id, rect);
            return id;
        }

        /// <summary>
        /// Celdas que toca la caja del mundo [min, max] (solo cuentan X y Z), con un margen de
        /// celdas alrededor. No se recorta a la rejilla.
        /// </summary>
        public RectInt CellsCovering(Vector3 worldMin, Vector3 worldMax, int marginCells = 0)
        {
            // Un borde que cae justo en la línea entre dos celdas no cuenta como tocar la siguiente.
            const float epsilon = 0.0001f;
            Vector2Int min = WorldToCell(worldMin);
            Vector2Int max = WorldToCell(new Vector3(worldMax.x - epsilon, worldMax.y, worldMax.z - epsilon));
            max = Vector2Int.Max(min, max);
            marginCells = Mathf.Max(0, marginCells);

            return new RectInt(min.x - marginCells, min.y - marginCells,
                max.x - min.x + 1 + 2 * marginCells, max.y - min.y + 1 + 2 * marginCells);
        }

        /// <summary>
        /// Bloquea un rectángulo (zona donde no se construye). Lo que sobresalga de la rejilla
        /// se ignora.
        /// </summary>
        /// <returns>Id para liberarlo, o <see cref="NoOccupant"/> si no queda nada dentro o ya hay algo.</returns>
        public int Reserve(RectInt rect)
        {
            int xMin = Mathf.Max(rect.xMin, 0);
            int zMin = Mathf.Max(rect.yMin, 0);
            int xMax = Mathf.Min(rect.xMax, Width);
            int zMax = Mathf.Min(rect.yMax, Depth);
            if (xMax <= xMin || zMax <= zMin) return NoOccupant;

            return Occupy(new RectInt(xMin, zMin, xMax - xMin, zMax - zMin));
        }

        public bool Release(int occupantId)
        {
            if (!footprints.TryGetValue(occupantId, out RectInt rect)) return false;

            Fill(rect, NoOccupant);
            footprints.Remove(occupantId);
            return true;
        }

        private void Fill(RectInt rect, int value)
        {
            for (int z = rect.yMin; z < rect.yMax; z++)
            {
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    cells[Index(x, z)] = value;
                }
            }
        }

        private int Index(int x, int z) => z * Width + x;
    }
}
