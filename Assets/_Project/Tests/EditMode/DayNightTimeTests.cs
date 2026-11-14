using DeadAcres.DayNight;
using NUnit.Framework;

namespace DeadAcres.Tests
{
    public sealed class DayNightTimeTests
    {
        [TestCase(GamePhase.Dawn, 0f, 5f)]
        [TestCase(GamePhase.Dawn, 1f, 6f)]
        [TestCase(GamePhase.Day, 0f, 6f)]
        [TestCase(GamePhase.Day, 0.5f, 12.5f)]
        [TestCase(GamePhase.Dusk, 0.5f, 19.5f)]
        [TestCase(GamePhase.Night, 0f, 20f)]
        [TestCase(GamePhase.Night, 0.5f, 0.5f)]
        [TestCase(GamePhase.Night, 1f, 5f)]
        [TestCase(GamePhase.Night, 3f, 5f)]
        public void GetHour_MapsEachPhaseToItsClockRange(GamePhase phase, float progress, float expectedHour)
        {
            Assert.AreEqual(expectedHour, DayNightTime.GetHour(phase, progress), 0.0001f);
        }

        [TestCase(7.47f, 7, 28)]
        [TestCase(0f, 0, 0)]
        [TestCase(23.999f, 23, 59)]
        [TestCase(12.5f, 12, 30)]
        public void ToHoursMinutes_SplitsTheHour(float hour, int expectedHours, int expectedMinutes)
        {
            DayNightTime.ToHoursMinutes(hour, out int hours, out int minutes);

            Assert.AreEqual(expectedHours, hours);
            Assert.AreEqual(expectedMinutes, minutes);
        }
    }
}
