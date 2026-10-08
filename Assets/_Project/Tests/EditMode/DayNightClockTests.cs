using System.Collections.Generic;
using DeadAcres.DayNight;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class DayNightClockTests
    {
        private sealed class FakeWave : IWaveStatus
        {
            public bool IsWaveCleared { get; set; }
        }

        private DayNightConfig config;
        private FakeWave wave;
        private DayNightClock clock;
        private List<PhaseChange> phases;
        private List<NightResult> nights;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<DayNightConfig>();
            config.Configure(dawn: 10f, firstDay: 60f, day: 40f, dusk: 5f, minNight: 30f, maxNight: 50f);
            wave = new FakeWave();
            clock = new DayNightClock(config, wave);
            phases = new List<PhaseChange>();
            nights = new List<NightResult>();
            clock.PhaseChanged += phases.Add;
            clock.NightCompleted += nights.Add;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(config);

        [Test]
        public void Begin_StartsAtDawnOfDayOne()
        {
            clock.Begin();

            Assert.AreEqual(GamePhase.Dawn, clock.Phase);
            Assert.AreEqual(1, clock.DayNumber);
            Assert.AreEqual(1, phases.Count);
        }

        [Test]
        public void FirstDay_UsesLongerDuration_ThenNormalDay()
        {
            clock.Begin();
            clock.Tick(10f); // → Día 1
            Assert.AreEqual(GamePhase.Day, clock.Phase);
            Assert.AreEqual(60f, clock.PhaseDuration);

            clock.Tick(60f); // → Atardecer
            clock.Tick(5f);  // → Noche 1
            wave.IsWaveCleared = true;
            clock.Tick(30f); // → Amanecer día 2
            clock.Tick(10f); // → Día 2

            Assert.AreEqual(2, clock.DayNumber);
            Assert.AreEqual(GamePhase.Day, clock.Phase);
            Assert.AreEqual(40f, clock.PhaseDuration);
        }

        [Test]
        public void Night_DoesNotEndBeforeMinimum_EvenIfCleared()
        {
            AdvanceToNight();
            wave.IsWaveCleared = true;

            clock.Tick(29f);

            Assert.AreEqual(GamePhase.Night, clock.Phase);
            Assert.IsEmpty(nights);
        }

        [Test]
        public void Night_EndsAfterMinimum_WhenWaveCleared()
        {
            AdvanceToNight();
            clock.Tick(35f);
            Assert.AreEqual(GamePhase.Night, clock.Phase, "Con enemigos vivos la noche sigue.");

            wave.IsWaveCleared = true;
            clock.Tick(0.1f);

            Assert.AreEqual(GamePhase.Dawn, clock.Phase);
            Assert.AreEqual(1, nights.Count);
            Assert.AreEqual(1, nights[0].NightNumber);
            Assert.IsFalse(nights[0].EndedByTimeout);
        }

        [Test]
        public void Night_EndsAtMaximum_WithEnemiesAlive()
        {
            AdvanceToNight();

            clock.Tick(50f);

            Assert.AreEqual(GamePhase.Dawn, clock.Phase);
            Assert.IsTrue(nights[0].EndedByTimeout);
        }

        [Test]
        public void SkipDay_OnlyWorksDuringDay()
        {
            clock.Begin();
            Assert.IsFalse(clock.TrySkipDay(), "No se salta el amanecer.");

            clock.Tick(10f);
            Assert.IsTrue(clock.TrySkipDay());
            Assert.AreEqual(GamePhase.Dusk, clock.Phase);
        }

        [Test]
        public void Paused_DoesNotAdvance()
        {
            clock.Begin();
            clock.IsPaused = true;

            clock.Tick(100f);

            Assert.AreEqual(GamePhase.Dawn, clock.Phase);
            Assert.AreEqual(0f, clock.PhaseElapsed);
        }

        [Test]
        public void StoppingDuringNightCompleted_DoesNotStartNextDawn()
        {
            clock.NightCompleted += _ => clock.Stop();
            AdvanceToNight();
            wave.IsWaveCleared = true;
            int phaseEventsBefore = phases.Count;

            clock.Tick(30f);

            Assert.AreEqual(phaseEventsBefore, phases.Count);
            Assert.IsFalse(clock.IsRunning);
        }

        private void AdvanceToNight()
        {
            clock.Begin();
            clock.Tick(10f);
            clock.Tick(60f);
            clock.Tick(5f);
            Assert.AreEqual(GamePhase.Night, clock.Phase);
        }
    }
}
