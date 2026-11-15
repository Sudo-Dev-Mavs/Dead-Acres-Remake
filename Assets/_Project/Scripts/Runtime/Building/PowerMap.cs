using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>
    /// Fuentes de energía de la partida (caravana, generadores) en C# puro. Un aparato tiene
    /// energía si está dentro del radio de alguna fuente, medido en horizontal.
    /// </summary>
    public sealed class PowerMap
    {
        private struct Source
        {
            public int Id;
            public Vector3 Position;
            public float Radius;
        }

        private readonly List<Source> sources = new List<Source>(8);
        private int nextId = 1;

        /// <summary>Se añadió o se quitó una fuente: los aparatos deben volver a comprobar.</summary>
        public event Action Changed;

        public int SourceCount => sources.Count;

        /// <returns>Id para quitar la fuente después.</returns>
        public int Add(Vector3 position, float radius)
        {
            int id = nextId++;
            sources.Add(new Source { Id = id, Position = position, Radius = Mathf.Max(0f, radius) });
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
                float dx = point.x - sources[i].Position.x;
                float dz = point.z - sources[i].Position.z;
                if (dx * dx + dz * dz <= sources[i].Radius * sources[i].Radius)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
