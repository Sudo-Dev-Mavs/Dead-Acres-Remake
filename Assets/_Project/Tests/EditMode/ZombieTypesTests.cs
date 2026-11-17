using System.Collections.Generic;
using DeadAcres.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class ZombieTypesTests
    {
        private static List<ZombieType> Roster() => new List<ZombieType>
        {
            new ZombieType("Zombi", null, 1, 10f),
            new ZombieType("Corredor", null, 2, 4f),
            new ZombieType("Swat", null, 3, 3f),
            new ZombieType("Demoledor", null, 5, 3f),
            new ZombieType("Mutante", null, 1, 1f, true),
            new ZombieType("Riot Mutant", null, 1, 1f, true),
            new ZombieType("Demoledor Mutante", null, 1, 1f, true),
        };

        [Test]
        public void FirstNight_OnlyNormalZombies()
        {
            List<ZombieType> roster = Roster();
            var rng = new System.Random(1);
            for (int i = 0; i < 300; i++)
            {
                Assert.AreEqual(0, ZombieRoster.PickIndex(roster, 1, false, rng));
            }
        }

        [Test]
        public void LaterNights_MixTheTypesUnlockedSoFar_ByWeight_AndNeverAMiniBoss()
        {
            List<ZombieType> roster = Roster();
            var rng = new System.Random(7);
            var counts = new int[roster.Count];
            const int samples = 6000;
            for (int i = 0; i < samples; i++)
            {
                counts[ZombieRoster.PickIndex(roster, 3, false, rng)]++;
            }

            // Noche 3: normal (10), corredor (4) y swat (3). El demoledor aún no; los jefes, nunca aquí.
            Assert.AreEqual(0, counts[3], "El demoledor no sale hasta la noche 5.");
            Assert.AreEqual(0, counts[4] + counts[5] + counts[6], "Los mini jefes no se mezclan en la oleada.");
            Assert.IsTrue(counts[0] > counts[1] && counts[1] > counts[2], "El peso manda en la frecuencia.");
            Assert.AreEqual(10f / 17f, counts[0] / (float)samples, 0.03f);
        }

        [Test]
        public void MiniBoss_IsOneOfTheThree_AtRandom()
        {
            List<ZombieType> roster = Roster();
            var rng = new System.Random(3);
            var seen = new HashSet<int>();
            for (int i = 0; i < 300; i++)
            {
                int index = ZombieRoster.PickIndex(roster, 10, true, rng);
                Assert.IsTrue(roster[index].miniBoss);
                seen.Add(index);
            }

            Assert.AreEqual(3, seen.Count, "Cualquiera de los tres puede tocar.");
        }

        [Test]
        public void PickIndex_WithNothingToChoose_ReturnsMinusOne()
        {
            var rng = new System.Random(1);
            Assert.AreEqual(-1, ZombieRoster.PickIndex(null, 5, false, rng));
            Assert.AreEqual(-1, ZombieRoster.PickIndex(new List<ZombieType>(), 5, false, rng));
            Assert.AreEqual(-1, ZombieRoster.PickIndex(new List<ZombieType> { new ZombieType("Zombi", null, 1, 10f) }, 5, true, rng));
        }

        [TestCase(1, false)]
        [TestCase(9, false)]
        [TestCase(10, true)]
        [TestCase(11, false)]
        [TestCase(20, true)]
        [TestCase(30, true)]
        public void MiniBossNight_IsEveryTenth(int night, bool expected)
        {
            Assert.AreEqual(expected, ZombieRoster.IsMiniBossNight(night, 10));
        }

        [Test]
        public void MiniBossNight_ZeroInterval_MeansNever()
        {
            Assert.IsFalse(ZombieRoster.IsMiniBossNight(10, 0));
            Assert.IsFalse(ZombieRoster.IsMiniBossNight(0, 10));
        }

        [Test]
        public void Shield_CoversShotsComingFromTheFront_NotFromTheSidesOrBehind()
        {
            // El zombi mira hacia +Z. Una bala que le llega de frente viaja hacia -Z.
            Vector3 forward = Vector3.forward;
            Assert.IsTrue(ZombieShield.Covers(forward, Vector3.back, 70f), "De frente.");
            Assert.IsTrue(ZombieShield.Covers(forward, new Vector3(0.8f, -0.3f, -1f), 70f), "En diagonal por delante, desde arriba.");
            Assert.IsFalse(ZombieShield.Covers(forward, Vector3.left, 70f), "De costado.");
            Assert.IsFalse(ZombieShield.Covers(forward, Vector3.forward, 70f), "Por la espalda.");
            Assert.IsFalse(ZombieShield.Covers(forward, Vector3.down, 70f), "Desde arriba, sin dirección horizontal.");
        }
    }
}
