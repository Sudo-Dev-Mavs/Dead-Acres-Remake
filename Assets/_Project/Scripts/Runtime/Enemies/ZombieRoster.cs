using System;
using System.Collections.Generic;

namespace DeadAcres.Enemies
{
    /// <summary>Un tipo de zombi que puede salir en las oleadas.</summary>
    [Serializable]
    public sealed class ZombieType
    {
        public string displayName;
        public ZombieBrain prefab;

        /// <summary>Primera noche en la que puede salir.</summary>
        public int fromNight = 1;

        /// <summary>Probabilidad relativa frente a los demás tipos disponibles esa noche.</summary>
        public float weight = 1f;

        /// <summary>Mini jefe: no sale en la oleada normal, sino uno solo cada cierto número de noches.</summary>
        public bool miniBoss;

        public ZombieType()
        {
        }

        public ZombieType(string displayName, ZombieBrain prefab, int fromNight, float weight, bool miniBoss = false)
        {
            this.displayName = displayName;
            this.prefab = prefab;
            this.fromNight = fromNight;
            this.weight = weight;
            this.miniBoss = miniBoss;
        }
    }

    /// <summary>Qué tipo de zombi toca. Funciones puras.</summary>
    public static class ZombieRoster
    {
        /// <summary>
        /// Elige al azar, por peso, entre los tipos que ya pueden salir esa noche: los normales
        /// o los mini jefes, según se pida.
        /// </summary>
        /// <returns>Índice en la lista, o -1 si no hay ninguno.</returns>
        public static int PickIndex(IReadOnlyList<ZombieType> types, int nightNumber, bool miniBoss, Random rng)
        {
            if (types == null)
            {
                return -1;
            }

            double total = 0d;
            for (int i = 0; i < types.Count; i++)
            {
                if (IsCandidate(types[i], nightNumber, miniBoss)) total += types[i].weight;
            }

            if (total <= 0d)
            {
                return -1;
            }

            double target = rng.NextDouble() * total;
            int last = -1;
            for (int i = 0; i < types.Count; i++)
            {
                if (!IsCandidate(types[i], nightNumber, miniBoss))
                {
                    continue;
                }

                last = i;
                target -= types[i].weight;
                if (target < 0d)
                {
                    return i;
                }
            }

            return last;
        }

        /// <summary>¿Toca mini jefe esa noche? Cada <paramref name="everyNights"/> noches (10, 20, 30...).</summary>
        public static bool IsMiniBossNight(int nightNumber, int everyNights)
        {
            return everyNights > 0 && nightNumber > 0 && nightNumber % everyNights == 0;
        }

        private static bool IsCandidate(ZombieType type, int nightNumber, bool miniBoss)
        {
            return type != null && type.miniBoss == miniBoss && type.weight > 0f && type.fromNight <= nightNumber;
        }
    }
}
