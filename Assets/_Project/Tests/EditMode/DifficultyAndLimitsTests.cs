using DeadAcres.Combat;
using DeadAcres.Core.Session;
using DeadAcres.Difficulty;
using DeadAcres.GameMode;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class DifficultyAndLimitsTests
    {
        [TestCase(0.7f, "-30 %")]
        [TestCase(1f, "normal")]
        [TestCase(1.5f, "+50 %")]
        [TestCase(2.2f, "+120 %")]
        [TestCase(1.6f, "+60 %")]
        public void FormatDelta_MatchesConfirmationScreen(float multiplier, string expected)
        {
            Assert.AreEqual(expected, DifficultyProfile.FormatDelta(multiplier));
        }

        [Test]
        public void Hell_ScalesEnemyHealthAndDamageTakenByAllPlayerSideTargets()
        {
            var hell = ScriptableObject.CreateInstance<DifficultyProfile>();
            hell.Configure("Hell", string.Empty, 2.2f, 1.6f);
            var session = ScriptableObject.CreateInstance<GameSession>();
            session.Select(hell, null);

            var go = new GameObject("test");
            var zombie = go.AddComponent<Health>();
            zombie.Configure(Faction.Enemy, 100f, session);
            var wall = go.AddComponent<Health>();
            wall.Configure(Faction.PlayerSide, 100f, session);

            Assert.AreEqual(220f, zombie.Max, 0.001f, "Vida de enemigo ×2,2.");
            Assert.AreEqual(10f, zombie.ApplyDamage(10f), 0.001f, "El daño a enemigos no se escala.");
            Assert.AreEqual(16f, wall.ApplyDamage(10f), 0.001f, "El daño a defensas se escala ×1,6.");
            Assert.AreEqual(100f, wall.Max, 0.001f, "La vida del bando del jugador no se escala.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(session);
            Object.DestroyImmediate(hell);
        }

        [Test]
        public void Health_DiesOnce_AndHealDoesNotRevive()
        {
            var go = new GameObject("rv");
            var rv = go.AddComponent<Health>();
            rv.Configure(Faction.PlayerSide, 50f, null);
            int deaths = 0;
            rv.Died += _ => deaths++;

            rv.ApplyDamage(80f);
            rv.ApplyDamage(10f);
            rv.Heal(25f);

            Assert.AreEqual(1, deaths);
            Assert.IsTrue(rv.IsDead);
            Assert.AreEqual(0f, rv.Current);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void PlacementLimiter_RespectsLimitPerCategory()
        {
            var rules = ScriptableObject.CreateInstance<GameModeRules>();
            rules.Configure("Test", plants: 2, objects: 1, nights: 5);
            var limiter = new PlacementLimiter(rules);

            Assert.IsTrue(limiter.TryRegister(PlaceableCategory.Plant));
            Assert.IsTrue(limiter.TryRegister(PlaceableCategory.Plant));
            Assert.IsFalse(limiter.TryRegister(PlaceableCategory.Plant));
            Assert.IsTrue(limiter.TryRegister(PlaceableCategory.Object), "Las categorías son independientes.");

            limiter.Unregister(PlaceableCategory.Plant);
            Assert.IsTrue(limiter.CanPlace(PlaceableCategory.Plant));

            limiter.Unregister(PlaceableCategory.Object);
            limiter.Unregister(PlaceableCategory.Object);
            Assert.AreEqual(0, limiter.GetCount(PlaceableCategory.Object), "Nunca baja de cero.");

            Object.DestroyImmediate(rules);
        }

        [Test]
        public void EndlessRules_AreEndlessWith500Limits()
        {
            var endless = ScriptableObject.CreateInstance<GameModeRules>();
            endless.Configure("Endless", plants: 500, objects: 500, nights: 0);

            Assert.IsTrue(endless.IsEndless);
            Assert.AreEqual(500, endless.GetLimit(PlaceableCategory.Plant));
            Assert.AreEqual(500, endless.GetLimit(PlaceableCategory.Object));

            Object.DestroyImmediate(endless);
        }
    }
}
