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

            public float NightTimeScale { get; set; } = 1f;
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

        [Test]
        public void Night_FastForward_ShortensOnlyTheWaitUntilTheMinimum()
        {
            AdvanceToNight(); // mínimo 30 s, máximo 50 s
            clock.Tick(6f);
            wave.NightTimeScale = 3f;

            clock.Tick(4f); // cuenta como 12 s → 18 s de reloj
            Assert.AreEqual(18f, clock.PhaseElapsed, 0.0001f);

            clock.Tick(10f); // serían 30 s más: se queda en el mínimo, no lo salta
            Assert.AreEqual(30f, clock.PhaseElapsed, 0.0001f);
            Assert.AreEqual(GamePhase.Night, clock.Phase, "Con enemigos vivos la noche no termina.");

            clock.Tick(5f); // pasado el mínimo vuelve a ir a velocidad normal
            Assert.AreEqual(35f, clock.PhaseElapsed, 0.0001f);

            wave.IsWaveCleared = true;
            clock.Tick(0.1f);
            Assert.AreEqual(GamePhase.Dawn, clock.Phase);
        }

        [Test]
        public void Night_FastForward_EndsTheNightSoonerWhenCleared()
        {
            AdvanceToNight();
            wave.IsWaveCleared = true;
            wave.NightTimeScale = 3f;

            clock.Tick(9.9f);
            Assert.AreEqual(GamePhase.Night, clock.Phase);

            clock.Tick(0.2f); // 10,1 s reales × 3 ≥ 30 s de mínimo

            Assert.AreEqual(GamePhase.Dawn, clock.Phase);
            Assert.IsFalse(nights[0].EndedByTimeout);
        }

        [Test]
        public void FastForward_DoesNotAffectTheDay()
        {
            clock.Begin();
            clock.Tick(10f); // → Día 1 (60 s)
            wave.NightTimeScale = 3f;

            clock.Tick(20f);

            Assert.AreEqual(GamePhase.Day, clock.Phase);
            Assert.AreEqual(20f, clock.PhaseElapsed, 0.0001f);
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
