using DeadAcres.DayNight;
using DeadAcres.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    /// <summary>Reloj y oleada juntos, con las duraciones reales del juego (mínimo 120 s, máximo 300 s).</summary>
    public sealed class NightFlowTests
    {
        private DayNightConfig config;
        private WaveConfig waves;
        private WaveRunner runner;
        private DayNightClock clock;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<DayNightConfig>();
            config.Configure(dawn: 15f, firstDay: 360f, day: 240f, dusk: 30f, minNight: 120f, maxNight: 300f);
            waves = ScriptableObject.CreateInstance<WaveConfig>();
            waves.Configure(firstNight: 30, perNight: 5, delay: 3f, window: 60f);
            waves.ConfigureReinforcements(fraction: 0.25f, minCount: 3, maxGroups: 2, window: 6f,
                minRemaining: 15f, fastForward: 3f, radiusFactor: 0.6f);

            runner = new WaveRunner();
            clock = new DayNightClock(config, runner);
            clock.Begin();
            clock.Tick(15f);
            clock.Tick(360f);
            clock.Tick(30f);
            Assert.AreEqual(GamePhase.Night, clock.Phase);
            runner.Begin(30, waves.InitialDelay, waves.SpawnWindow);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(waves);
        }

        /// <summary>Avanza reloj y oleada a la vez, como hace la escena, en pasos de 0,1 s.</summary>
        private float Advance(float seconds, bool killEverything)
        {
            float realTime = 0f;
            while (realTime < seconds && clock.Phase == GamePhase.Night)
            {
                runner.Tick(0.1f);
                if (killEverything)
                {
                    while (runner.Alive > 0) runner.NotifyEnemyRemoved();
                }

                clock.Tick(0.1f);
                realTime += 0.1f;
            }

            return realTime;
        }

        [Test]
        public void ThirtyZombiesKilledAt150s_DawnComesAtOnce_NotAt300s()
        {
            Advance(149.9f, killEverything: false);
            Assert.AreEqual(GamePhase.Night, clock.Phase, "Con zombis vivos la noche sigue pasado el mínimo.");

            float extra = Advance(10f, killEverything: true);

            Assert.AreEqual(GamePhase.Dawn, clock.Phase);
            Assert.IsTrue(extra <= 0.2f, $"Amaneció {extra} s después de matar al último.");
        }

        [Test]
        public void KilledAt80s_ReinforcementsCome_AndTheRestOfTheNightRunsThreeTimesFaster()
        {
            Advance(80f, killEverything: true);
            Assert.AreEqual(GamePhase.Night, clock.Phase, "Antes del mínimo la noche no termina.");

            float remaining = clock.PhaseDuration - clock.PhaseElapsed;
            Assert.IsTrue(WaveMath.ShouldSendReinforcements(waves, runner.ReinforcementGroups, remaining));
            Assert.IsTrue(runner.AddReinforcements(WaveMath.GetReinforcementCount(waves, 30), waves.ReinforcementWindow,
                waves.NightFastForward));

            // Quedan ~40 s de reloj: al triple de velocidad son ~13 s reales.
            float real = Advance(60f, killEverything: true);

            Assert.AreEqual(GamePhase.Dawn, clock.Phase);
            Assert.IsTrue(real > 12f && real < 15f, $"El resto de la noche duró {real} s reales.");
        }

        [Test]
        public void ReinforcementSurvivor_DoesNotHoldTheNight_ItEndsOnItsOwn()
        {
            Advance(80f, killEverything: true);
            Assert.IsTrue(runner.AddReinforcements(WaveMath.GetReinforcementCount(waves, 30), waves.ReinforcementWindow,
                waves.NightFastForward));

            // Nadie mata a los refuerzos: quedan 220 s de reloj, que al triple son ~73 s reales.
            float real = Advance(200f, killEverything: false);

            Assert.AreEqual(GamePhase.Dawn, clock.Phase, "La noche no espera a que caiga el último.");
            Assert.IsTrue(real > 72f && real < 75f, $"El resto de la noche duró {real} s reales.");
            Assert.IsTrue(runner.Alive > 0, "Amaneció con zombis vivos: son los que se queman.");
        }

        [Test]
        public void ZombiesNeverKilled_NightStopsAt300s()
        {
            float real = Advance(400f, killEverything: false);

            Assert.AreEqual(GamePhase.Dawn, clock.Phase);
            Assert.IsTrue(real > 299f && real < 301f, $"La noche duró {real} s.");
        }
    }
}
