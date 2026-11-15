using DeadAcres.DayNight;
using DeadAcres.Farming;
using NUnit.Framework;

namespace DeadAcres.Tests
{
    public sealed class GrowthRulesTests
    {
        [TestCase(GamePhase.Dawn, 1f)]
        [TestCase(GamePhase.Day, 1f)]
        [TestCase(GamePhase.Dusk, 1f)]
        [TestCase(GamePhase.Night, 0.1f)]
        public void OnlyTheNightPhaseSlowsGrowth(GamePhase phase, float expected)
        {
            Assert.AreEqual(expected, GrowthRules.GetTimeScale(phase, 0.1f), 0.0001f);
        }

        [Test]
        public void NightMultiplier_IsClampedBetweenZeroAndOne()
        {
            Assert.AreEqual(0f, GrowthRules.GetTimeScale(GamePhase.Night, -2f), 0.0001f);
            Assert.AreEqual(1f, GrowthRules.GetTimeScale(GamePhase.Night, 5f), 0.0001f);
            Assert.AreEqual(0f, GrowthRules.GetTimeScale(GamePhase.Night, 0f), 0.0001f, "Con 0 los cultivos no crecen de noche.");
        }
    }
}
