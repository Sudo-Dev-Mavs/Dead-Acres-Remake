using DeadAcres.Settings;
using NUnit.Framework;

namespace DeadAcres.Tests
{
    public sealed class StepCycleTests
    {
        private static readonly float[] Steps = { 0.25f, 0.5f, 1f, 2f };

        [TestCase(0.25f, 0.5f)]
        [TestCase(1f, 2f)]
        [TestCase(2f, 0.25f)]
        public void Next_WalksTheListAndWraps(float current, float expected)
        {
            Assert.AreEqual(expected, StepCycle.Next(Steps, current), 0.0001f);
        }

        [TestCase(0.6f, 1f)]
        [TestCase(1.4f, 2f)]
        [TestCase(99f, 0.25f)]
        [TestCase(-3f, 0.5f)]
        public void Next_FromAValueNotInTheList_StartsAtTheNearest(float current, float expected)
        {
            Assert.AreEqual(expected, StepCycle.Next(Steps, current), 0.0001f);
        }

        [Test]
        public void Next_WithoutSteps_LeavesTheValueAlone()
        {
            Assert.AreEqual(7f, StepCycle.Next(new float[0], 7f), 0.0001f);
            Assert.AreEqual(7f, StepCycle.Next(null, 7f), 0.0001f);
        }
    }
}
