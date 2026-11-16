using DeadAcres.Audio;
using NUnit.Framework;

namespace DeadAcres.Tests
{
    public sealed class VolumeStepsTests
    {
        [TestCase(1f, 0.75f)]
        [TestCase(0.75f, 0.5f)]
        [TestCase(0.5f, 0.25f)]
        [TestCase(0.25f, 0f)]
        [TestCase(0f, 1f)]
        public void Next_WalksDownAndWrapsAround(float current, float expected)
        {
            Assert.AreEqual(expected, VolumeSteps.Next(current), 0.0001f);
        }

        [TestCase(0.8f, 0.5f)]
        [TestCase(0.6f, 0.25f)]
        [TestCase(5f, 0.75f)]
        [TestCase(-1f, 1f)]
        public void Next_FromAnOddValue_StartsAtTheNearestStep(float current, float expected)
        {
            Assert.AreEqual(expected, VolumeSteps.Next(current), 0.0001f);
        }

        [Test]
        public void Format_ShowsPercentOrSilence()
        {
            Assert.AreEqual("Volumen: 100 %", VolumeSteps.Format(1f));
            Assert.AreEqual("Volumen: 25 %", VolumeSteps.Format(0.25f));
            Assert.AreEqual("Volumen: silencio", VolumeSteps.Format(0f));
            Assert.AreEqual("Volumen: 100 %", VolumeSteps.Format(3f));
        }
    }
}
