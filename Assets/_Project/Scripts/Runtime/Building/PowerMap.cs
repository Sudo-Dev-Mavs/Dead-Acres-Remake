using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>
    /// Fuentes de energía de la partida (caravana, generadores) en C# puro. Un aparato tiene
    /// energía si está dentro del radio de alguna fuente, medido en horizontal. Si la fuente
    /// tiene tamaño (la caravana), el radio se cuenta desde su borde y no desde su centro.
    /// </summary>
    public sealed class PowerMap
    {
        private struct Source
        {
            public int Id;
            public Vector3 Position;
            public float Radius;
            public Vector2 HalfExtents;
        }

        private readonly List<Source> sources = new List<Source>(8);
        private int nextId = 1;

        /// <summary>Se añadió o se quitó una fuente: los aparatos deben volver a comprobar.</summary>
        public event Action Changed;

        public int SourceCount => sources.Count;

        /// <param name="halfExtents">Mitad del tamaño de la fuente en X y Z. Cero: un punto.</param>
        /// <returns>Id para quitar la fuente después.</returns>
        public int Add(Vector3 position, float radius, Vector2 halfExtents = default)
        {
            int id = nextId++;
            sources.Add(new Source
            {
                Id = id,
                Position = position,
                Radius = Mathf.Max(0f, radius),
                HalfExtents = new Vector2(Mathf.Max(0f, halfExtents.x), Mathf.Max(0f, halfExtents.y)),
            });
            Changed?.Invoke();
            return id;
        }

        public bool Remove(int id)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i].Id == id)
                {
                    sources.RemoveAt(i);
                    Changed?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public bool IsPowered(Vector3 point)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                // Distancia al rectángulo de la fuente: dentro de él vale 0.
                float dx = Mathf.Max(0f, Mathf.Abs(point.x - sources[i].Position.x) - sources[i].HalfExtents.x);
                float dz = Mathf.Max(0f, Mathf.Abs(point.z - sources[i].Position.z) - sources[i].HalfExtents.y);
                if (dx * dx + dz * dz <= sources[i].Radius * sources[i].Radius)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
