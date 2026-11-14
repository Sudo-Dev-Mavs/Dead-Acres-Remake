using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>
    /// Rejilla de bloques de la zona jugable (64×64 en el slice). Solo guarda ocupación:
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
