using DeadAcres.Combat;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class HitZonesTests
    {
        // Zombi normal: 1,8 m de alto y 0,35 de radio. Mira hacia +Z; el tirador está delante.
        private const float Height = 1.8f;
        private const float LegsTop = 0.45f;
        private const float HeadBottom = 0.83f;
        private const float ArmOffset = 0.22f;

        private static HitZone FromFront(float x, float y)
        {
            // Tiro recto desde 10 m por delante, a esa altura y con ese desvío lateral.
            var origin = new Vector3(x, y, 10f);
            var hit = new Vector3(x, y, Mathf.Sqrt(Mathf.Max(0f, 0.35f * 0.35f - x * x)));
            return HitZones.Classify(hit, origin, Vector3.back, Height, LegsTop, HeadBottom, ArmOffset);
        }

        [TestCase(0f, 1.65f, HitZone.Head)]
        [TestCase(0.2f, 1.55f, HitZone.Head)]
        [TestCase(0f, 1.2f, HitZone.Torso)]
        [TestCase(0.15f, 1.0f, HitZone.Torso)]
        [TestCase(0.29f, 1.2f, HitZone.Arm)]
        [TestCase(-0.3f, 0.9f, HitZone.Arm)]
        [TestCase(0f, 0.4f, HitZone.Leg)]
        [TestCase(0.3f, 0.2f, HitZone.Leg)]
        public void FromTheFront_EachPartOfTheSilhouette_IsItsZone(float x, float y, HitZone expected)
        {
            Assert.AreEqual(expected, FromFront(x, y));
        }

        [Test]
        public void ZoneBorders_FallOnTheHigherOrOuterZone()
        {
            Assert.AreEqual(HitZone.Leg, FromFront(0f, Height * LegsTop - 0.01f));
            Assert.AreEqual(HitZone.Torso, FromFront(0f, Height * LegsTop + 0.01f));
            Assert.AreEqual(HitZone.Torso, FromFront(0f, Height * HeadBottom - 0.01f));
            Assert.AreEqual(HitZone.Head, FromFront(0f, Height * HeadBottom + 0.01f));
            Assert.AreEqual(HitZone.Torso, FromFront(ArmOffset - 0.01f, 1.2f));
            Assert.AreEqual(HitZone.Arm, FromFront(ArmOffset + 0.01f, 1.2f));
        }

        [Test]
        public void FromTheSide_AShotAtTheMiddleOfTheBody_IsStillTorso()
        {
            // El tirador está a la derecha del zombi: la bala toca la cápsula en x = 0,35, pero
            // pasa por el eje del cuerpo.
            var origin = new Vector3(10f, 1.2f, 0f);
            var hit = new Vector3(0.35f, 1.2f, 0f);
            Assert.AreEqual(HitZone.Torso, HitZones.Classify(hit, origin, Vector3.left, Height, LegsTop, HeadBottom, ArmOffset));
        }

        [Test]
        public void FromBehind_TheArmsAreStillAtTheSides()
        {
            var origin = new Vector3(-0.29f, 1.2f, -10f);
            var hit = new Vector3(-0.29f, 1.2f, -0.2f);
            Assert.AreEqual(HitZone.Arm, HitZones.Classify(hit, origin, Vector3.forward, Height, LegsTop, HeadBottom, ArmOffset));
        }

        [Test]
        public void FromAbove_StraightDown_UsesWhereItLands()
        {
            var origin = new Vector3(0.05f, 10f, 0f);
            var hit = new Vector3(0.05f, 1.75f, 0f);
            Assert.AreEqual(HitZone.Head, HitZones.Classify(hit, origin, Vector3.down, Height, LegsTop, HeadBottom, ArmOffset));
        }

        [Test]
        public void BiggerBody_ScalesItsZones()
        {
            // Zombi fuerte: 2,3 m. Lo que en el normal sería cabeza (1,65 m) aquí es torso.
            var origin = new Vector3(0f, 1.65f, 10f);
            var hit = new Vector3(0f, 1.65f, 0.45f);
            Assert.AreEqual(HitZone.Torso, HitZones.Classify(hit, origin, Vector3.back, 2.3f, LegsTop, HeadBottom, 0.28f));

            origin.y = hit.y = 2.1f;
            Assert.AreEqual(HitZone.Head, HitZones.Classify(hit, origin, Vector3.back, 2.3f, LegsTop, HeadBottom, 0.28f));
        }
    }
}
