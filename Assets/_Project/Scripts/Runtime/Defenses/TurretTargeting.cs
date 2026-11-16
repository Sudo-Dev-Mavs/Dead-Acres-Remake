using System.Collections.Generic;
using UnityEngine;

namespace DeadAcres.Defenses
{
    /// <summary>A quién alcanza cada tipo de torreta. Funciones puras sobre posiciones.</summary>
    public static class TurretTargeting
    {
        /// <summary>
        /// Enemigos dentro de un cono (escopeta, lanzallamas): los más cercanos primero, hasta
        /// <paramref name="maxTargets"/>. Los ángulos se miden en horizontal.
        /// </summary>
        public static void SelectCone(Vector3 origin, Vector3 aimDirection, float halfAngleDegrees, float range,
            IReadOnlyList<Vector3> positions, int maxTargets, List<int> results)
        {
            results.Clear();
            aimDirection.y = 0f;
            if (aimDirection.sqrMagnitude < 0.0001f || maxTargets <= 0)
            {
                return;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 to = positions[i] - origin;
                to.y = 0f;
                float distance = to.magnitude;
                if (distance > range)
                {
                    continue;
                }

                // Lo que está encima de la torreta cuenta siempre.
                if (distance > 0.01f && Vector3.Angle(aimDirection, to) > halfAngleDegrees)
                {
                    continue;
                }

                results.Add(i);
            }

            results.Sort((a, b) => (positions[a] - origin).sqrMagnitude.CompareTo((positions[b] - origin).sqrMagnitude));
            if (results.Count > maxTargets)
            {
                results.RemoveRange(maxTargets, results.Count - maxTargets);
            }
        }

        /// <summary>
        /// Rayo en cadena: desde el primer objetivo salta al enemigo más cercano que no haya
        /// tocado ya y esté a <paramref name="jumpRange"/> o menos, hasta <paramref name="maxJumps"/> saltos.
        /// </summary>
        /// <param name="results">Índices en orden de impacto; el primero es el objetivo inicial.</param>
        public static void SelectChain(IReadOnlyList<Vector3> positions, int firstIndex, int maxJumps, float jumpRange,
            List<int> results)
        {
            results.Clear();
            if (firstIndex < 0 || firstIndex >= positions.Count)
            {
                return;
            }

            results.Add(firstIndex);
            int current = firstIndex;
            for (int jump = 0; jump < maxJumps; jump++)
            {
                int next = -1;
                float bestSqr = jumpRange * jumpRange;
                for (int i = 0; i < positions.Count; i++)
                {
                    if (results.Contains(i))
                    {
                        continue;
                    }

                    float sqr = (positions[i] - positions[current]).sqrMagnitude;
                    if (sqr <= bestSqr)
                    {
                        bestSqr = sqr;
                        next = i;
                    }
                }

                if (next < 0)
                {
                    break;
                }

                results.Add(next);
                current = next;
            }
        }

        /// <summary>Daño del impacto número <paramref name="hitIndex"/> de la cadena (0 = el primero).</summary>
        public static float GetChainDamage(float damage, float falloffPerJump, int hitIndex)
        {
            return damage * Mathf.Pow(Mathf.Clamp01(falloffPerJump), Mathf.Max(0, hitIndex));
        }
    }
}
