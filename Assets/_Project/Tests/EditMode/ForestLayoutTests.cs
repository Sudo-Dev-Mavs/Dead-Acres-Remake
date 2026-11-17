using System.Collections.Generic;
using DeadAcres.World;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class ForestLayoutTests
    {
        private static List<ForestTree> Generate(int seed = 20151, int count = 3500)
        {
            var trees = new List<ForestTree>();
            ForestLayout.Generate(seed, 150f, 50f, count, 3, trees);
            return trees;
        }

        [Test]
        public void EveryTree_IsInTheBorderBand_NeverInThePlayableArea()
        {
            List<ForestTree> trees = Generate();
            Assert.AreEqual(3500, trees.Count);

            foreach (ForestTree tree in trees)
            {
                float x = Mathf.Abs(tree.Position.x);
                float z = Mathf.Abs(tree.Position.z);
                Assert.IsTrue(x <= 150f && z <= 150f, "Dentro del terreno.");
                Assert.IsTrue(x >= 100f || z >= 100f, "Fuera de la zona jugable de 200 × 200.");
                Assert.AreEqual(0f, tree.Position.y, 0.0001f);
            }
        }

        [Test]
        public void TheForest_SurroundsTheMap_OnAllFourSides()
        {
            int north = 0, south = 0, east = 0, west = 0;
            foreach (ForestTree tree in Generate())
            {
                if (tree.Position.z >= 100f) north++;
                if (tree.Position.z <= -100f) south++;
                if (tree.Position.x >= 100f) east++;
                if (tree.Position.x <= -100f) west++;
            }

            // Cada lado se lleva más o menos un tercio (las esquinas cuentan para dos lados).
            foreach (int side in new[] { north, south, east, west })
            {
                Assert.IsTrue(side > 800, $"Un lado quedó con solo {side} árboles.");
            }
        }

        [Test]
        public void Trees_ComeInSmallMediumAndLarge_WithEveryShade()
        {
            int small = 0, medium = 0, large = 0;
            var shades = new HashSet<int>();
            foreach (ForestTree tree in Generate())
            {
                if (tree.TotalHeight < 5f) small++;
                else if (tree.TotalHeight < 9.5f) medium++;
                else large++;

                shades.Add(tree.Shade);
                Assert.IsTrue(tree.TrunkHeight > 0f && tree.CanopySize > 0f && tree.TrunkWidth > 0f);
            }

            Assert.IsTrue(small > 1000 && medium > 1000, $"Pequeños {small}, medianos {medium}.");
            Assert.IsTrue(large > 400 && large < small, $"Grandes {large}: los menos, pero se ven.");
            Assert.AreEqual(3, shades.Count);
        }

        [Test]
        public void SameSeed_SameForest_AndAnotherSeed_AnotherForest()
        {
            List<ForestTree> first = Generate(7, 200);
            List<ForestTree> again = Generate(7, 200);
            List<ForestTree> other = Generate(8, 200);

            Assert.AreEqual(first.Count, again.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].Position, again[i].Position);
                Assert.AreEqual(first[i].CanopySize, again[i].CanopySize, 0.0001f);
            }

            Assert.AreNotEqual(first[0].Position, other[0].Position);
        }

        [Test]
        public void NonsenseInput_GivesNoTrees()
        {
            var trees = new List<ForestTree>();
            ForestLayout.Generate(1, 150f, 50f, 0, 3, trees);
            Assert.AreEqual(0, trees.Count);

            ForestLayout.Generate(1, 150f, 200f, 100, 3, trees);
            Assert.AreEqual(0, trees.Count, "Un bosque más ancho que el mapa no tiene sentido.");
        }
    }
}
