using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadAcres.World
{
    /// <summary>Un árbol del bosque del borde: dónde está y cómo de grande es.</summary>
    public readonly struct ForestTree
    {
        public readonly Vector3 Position;
        public readonly float TrunkHeight;
        public readonly float TrunkWidth;
        public readonly float CanopySize;
        public readonly float Yaw;

        /// <summary>Tono de verde de la copa (índice en la lista de materiales).</summary>
        public readonly int Shade;

        public ForestTree(Vector3 position, float trunkHeight, float trunkWidth, float canopySize, float yaw, int shade)
        {
            Position = position;
            TrunkHeight = trunkHeight;
            TrunkWidth = trunkWidth;
            CanopySize = canopySize;
            Yaw = yaw;
            Shade = shade;
        }

        public float TotalHeight => TrunkHeight + CanopySize;
    }

    /// <summary>
    /// Reparte los árboles del bosque que rodea el mapa: una franja entre el borde del terreno y
    /// la zona jugable, con árboles pequeños, medianos y grandes mezclados. C# puro: con la
    /// misma semilla sale siempre el mismo bosque.
    /// </summary>
    public static class ForestLayout
    {
        /// <param name="mapHalfSize">Mitad del lado del terreno (150 en un mapa de 300 × 300).</param>
        /// <param name="bandWidth">Anchura del bosque, desde el borde hacia dentro.</param>
        /// <param name="shades">Cuántos tonos de verde hay para repartir.</param>
        public static void Generate(int seed, float mapHalfSize, float bandWidth, int treeCount, int shades,
            List<ForestTree> results)
        {
            results.Clear();
            float inner = mapHalfSize - bandWidth;
            if (treeCount <= 0 || mapHalfSize <= 0f || bandWidth <= 0f || inner < 0f)
            {
                return;
            }

            var rng = new System.Random(seed);
            shades = Math.Max(1, shades);
            int guard = treeCount * 20;
            while (results.Count < treeCount && guard-- > 0)
            {
                // Punto al azar en todo el terreno; solo vale si cae en la franja del borde.
                float x = Lerp(-mapHalfSize, mapHalfSize, rng.NextDouble());
                float z = Lerp(-mapHalfSize, mapHalfSize, rng.NextDouble());
                if (Math.Abs(x) < inner && Math.Abs(z) < inner)
                {
                    continue;
                }

                // 40 % pequeños, 40 % medianos, 20 % grandes.
                double roll = rng.NextDouble();
                float trunkHeight;
                float canopy;
                if (roll < 0.4)
                {
                    trunkHeight = Lerp(1.2f, 2.2f, rng.NextDouble());
                    canopy = Lerp(1.6f, 2.6f, rng.NextDouble());
                }
                else if (roll < 0.8)
                {
                    trunkHeight = Lerp(2.5f, 4.5f, rng.NextDouble());
                    canopy = Lerp(3f, 4.6f, rng.NextDouble());
                }
                else
                {
                    trunkHeight = Lerp(5f, 8f, rng.NextDouble());
                    canopy = Lerp(5f, 7.5f, rng.NextDouble());
                }

                float trunkWidth = Math.Max(0.25f, canopy * 0.14f);
                float yaw = Lerp(0f, 90f, rng.NextDouble());
                results.Add(new ForestTree(new Vector3(x, 0f, z), trunkHeight, trunkWidth, canopy, yaw, rng.Next(shades)));
            }
        }

        private static float Lerp(float a, float b, double t) => a + (b - a) * (float)t;
    }
}
