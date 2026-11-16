using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>
    /// Red eléctrica de la partida en C# puro. Las fuentes (caravana, generadores) dan energía
    /// dentro de su radio, medido en horizontal desde su borde, y hasta su capacidad. Cada
    /// aparato consume lo suyo de una fuente que lo alcance; si ninguna tiene hueco, se queda
    /// sin energía. Tienen prioridad los aparatos que se conectaron antes.
    /// </summary>
    public sealed class PowerMap
    {
        private sealed class Source
        {
            public int Id;
            public Vector3 Position;
            public float Radius;
            public Vector2 HalfExtents;
            public float Capacity;
            public float Used;
        }

        private sealed class Consumer
        {
            public int Id;
            public Vector3 Position;
            public float Demand;
            public bool Powered;
        }

        private readonly List<Source> sources = new List<Source>(8);
        private readonly List<Consumer> consumers = new List<Consumer>(32);
        private int nextId = 1;

        /// <summary>Cambió el reparto: los aparatos deben volver a comprobar si tienen energía.</summary>
        public event Action Changed;

        public int SourceCount => sources.Count;

        /// <summary>Energía que se está consumiendo ahora mismo, entre todas las fuentes.</summary>
        public float TotalUsed { get; private set; }

        /// <summary>Capacidad sumada de todas las fuentes. Infinito si alguna no tiene límite.</summary>
        public float TotalCapacity { get; private set; }

        /// <param name="halfExtents">Mitad del tamaño de la fuente en X y Z. Cero: un punto.</param>
        /// <param name="capacity">Consumo máximo que aguanta. Por defecto, sin límite.</param>
        /// <returns>Id para quitar la fuente después.</returns>
        public int Add(Vector3 position, float radius, Vector2 halfExtents = default,
            float capacity = float.PositiveInfinity)
        {
            int id = nextId++;
            sources.Add(new Source
            {
                Id = id,
                Position = position,
                Radius = Mathf.Max(0f, radius),
                HalfExtents = new Vector2(Mathf.Max(0f, halfExtents.x), Mathf.Max(0f, halfExtents.y)),
                Capacity = Mathf.Max(0f, capacity),
            });
            Reallocate();
            return id;
        }

        public bool Remove(int id)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i].Id == id)
                {
                    sources.RemoveAt(i);
                    Reallocate();
                    return true;
                }
            }

            return false;
        }

        /// <returns>Id del aparato, para consultar si tiene energía y para desconectarlo.</returns>
        public int AddConsumer(Vector3 position, float demand)
        {
            int id = nextId++;
            consumers.Add(new Consumer { Id = id, Position = position, Demand = Mathf.Max(0f, demand) });
            Reallocate();
            return id;
        }

        public bool RemoveConsumer(int id)
        {
            for (int i = 0; i < consumers.Count; i++)
            {
                if (consumers[i].Id == id)
                {
                    consumers.RemoveAt(i);
                    Reallocate();
                    return true;
                }
            }

            return false;
        }

        public bool IsConsumerPowered(int id)
        {
            for (int i = 0; i < consumers.Count; i++)
            {
                if (consumers[i].Id == id)
                {
                    return consumers[i].Powered;
                }
            }

            return false;
        }

        /// <summary>¿Llega alguna fuente hasta aquí? No mira si le queda capacidad.</summary>
        public bool IsPowered(Vector3 point)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                if (Reaches(sources[i], point))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Reaches(Source source, Vector3 point)
        {
            // Distancia al rectángulo de la fuente: dentro de él vale 0.
            float dx = Mathf.Max(0f, Mathf.Abs(point.x - source.Position.x) - source.HalfExtents.x);
            float dz = Mathf.Max(0f, Mathf.Abs(point.z - source.Position.z) - source.HalfExtents.y);
            return dx * dx + dz * dz <= source.Radius * source.Radius;
        }

        // Reparto desde cero, por orden de conexión: cada aparato toma su consumo de la fuente
        // a su alcance a la que más le sobre.
        private void Reallocate()
        {
            TotalUsed = 0f;
            TotalCapacity = 0f;
            for (int i = 0; i < sources.Count; i++)
            {
                sources[i].Used = 0f;
                TotalCapacity += sources[i].Capacity;
            }

            for (int c = 0; c < consumers.Count; c++)
            {
                Consumer consumer = consumers[c];
                Source best = null;
                float bestFree = -1f;
                for (int s = 0; s < sources.Count; s++)
                {
                    Source source = sources[s];
                    float free = source.Capacity - source.Used;
                    if (free >= consumer.Demand && free > bestFree && Reaches(source, consumer.Position))
                    {
                        best = source;
                        bestFree = free;
                    }
                }

                consumer.Powered = best != null;
                if (best != null)
                {
                    best.Used += consumer.Demand;
                    TotalUsed += consumer.Demand;
                }
            }

            Changed?.Invoke();
        }
    }
}
