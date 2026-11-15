using System.Collections.Generic;
using DeadAcres.Enemies;
using DeadAcres.GameMode;
using DeadAcres.Match;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class WaveAndMatchTests
    {
        private WaveConfig config;
        private GameModeRules campaign;
        private GameModeRules endless;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<WaveConfig>();
            config.Configure(firstNight: 5, perNight: 3, delay: 6f, window: 100f);
            campaign = ScriptableObject.CreateInstance<GameModeRules>();
            campaign.Configure("Campaña", plants: 100, objects: 100, nights: 5);
            endless = ScriptableObject.CreateInstance<GameModeRules>();
            endless.Configure("Endless", plants: 500, objects: 500, nights: 0, countGrowth: 0.1f, healthGrowth: 0.05f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(campaign);
            Object.DestroyImmediate(endless);
        }

        [TestCase(1, 5)]
        [TestCase(2, 8)]
        [TestCase(5, 17)]
        [TestCase(0, 5)]
        public void EnemyCount_Campaign_GrowsLinearly(int night, int expected)
        {
            Assert.AreEqual(expected, WaveMath.GetEnemyCount(config, campaign, night));
            Assert.AreEqual(expected, WaveMath.GetEnemyCount(config, null, night), "Sin modo se comporta como Campaña.");
        }

        [Test]
        public void EnemyCount_Endless_AlsoCompounds()
        {
            Assert.AreEqual(5, WaveMath.GetEnemyCount(config, endless, 1));
            Assert.AreEqual(9, WaveMath.GetEnemyCount(config, endless, 2), "8 × 1,1 = 8,8 → 9");
            Assert.AreEqual(13, WaveMath.GetEnemyCount(config, endless, 3), "11 × 1,21 = 13,31 → 13");
        }

        [Test]
        public void HealthMultiplier_OnlyGrowsInEndless()
        {
            Assert.AreEqual(1f, WaveMath.GetHealthMultiplier(campaign, 4), 0.0001f);
            Assert.AreEqual(1f, WaveMath.GetHealthMultiplier(endless, 1), 0.0001f);
            Assert.AreEqual(1.1025f, WaveMath.GetHealthMultiplier(endless, 3), 0.0001f);
        }

        [Test]
        public void Runner_WithoutWave_CountsAsCleared()
        {
            var runner = new WaveRunner();

            Assert.IsTrue(runner.IsWaveCleared);
            Assert.AreEqual(0, runner.Tick(10f));
        }

        [Test]
        public void Runner_SpreadsSpawnsAcrossTheWindow()
        {
            var runner = new WaveRunner();
            runner.Begin(5, 6f, 100f); // uno cada 25 s tras 6 s de espera

            Assert.IsFalse(runner.IsWaveCleared);
            Assert.AreEqual(0, runner.Tick(5.9f), "Aún en la espera inicial.");
            Assert.AreEqual(1, runner.Tick(0.2f), "El primero sale al acabar la espera.");
            Assert.AreEqual(0, runner.Tick(24f));
            Assert.AreEqual(1, runner.Tick(1f));
            Assert.AreEqual(2, runner.Tick(50f), "Un tick largo suelta todos los atrasados.");
            Assert.AreEqual(1, runner.Tick(25f));
            Assert.AreEqual(0, runner.Tick(500f), "Nunca más de los previstos.");

            Assert.AreEqual(5, runner.Spawned);
            Assert.AreEqual(5, runner.Alive);
        }

        [Test]
        public void Runner_IsClearedOnlyWhenAllSpawnedAndDead()
        {
            var runner = new WaveRunner();
            runner.Begin(2, 0f, 10f);

            Assert.AreEqual(1, runner.Tick(0.1f));
            runner.NotifyEnemyRemoved();
            Assert.IsFalse(runner.IsWaveCleared, "Falta uno por aparecer.");

            Assert.AreEqual(1, runner.Tick(10f));
            Assert.IsFalse(runner.IsWaveCleared);
            runner.NotifyEnemyRemoved();

            Assert.IsTrue(runner.IsWaveCleared);
            Assert.IsTrue(runner.IsActive, "La oleada sigue abierta hasta el amanecer: pueden llegar refuerzos.");
            runner.NotifyEnemyRemoved();
            Assert.AreEqual(0, runner.Alive, "Nunca baja de cero.");
        }

        [Test]
        public void Runner_Reinforcements_OnlyAfterTheFieldIsEmpty()
        {
            var runner = new WaveRunner();
            Assert.IsFalse(runner.AddReinforcements(3, 6f, 3f), "Sin oleada no hay refuerzos.");

            runner.Begin(2, 0f, 0f);
            runner.Tick(0.1f);
            Assert.IsFalse(runner.AddReinforcements(3, 6f, 3f), "Quedan zombis vivos.");
            Assert.AreEqual(1f, runner.NightTimeScale, 0.0001f);

            runner.NotifyEnemyRemoved();
            runner.NotifyEnemyRemoved();
            Assert.IsTrue(runner.AddReinforcements(3, 6f, 3f));

            Assert.IsFalse(runner.IsWaveCleared, "Con refuerzos en camino la noche no puede acabar.");
            Assert.AreEqual(3f, runner.NightTimeScale, 0.0001f);
            Assert.AreEqual(1, runner.ReinforcementGroups);
            Assert.AreEqual(5, runner.Total);
        }

        [Test]
        public void Runner_Reinforcements_SpawnOverTheirOwnWindow_AndKeepTheNightFast()
        {
            var runner = new WaveRunner();
            runner.Begin(1, 0f, 0f);
            runner.Tick(0.1f);
            runner.NotifyEnemyRemoved();
            runner.AddReinforcements(3, 6f, 3f); // uno ya, otro a los 3 s, otro a los 6 s

            Assert.AreEqual(1, runner.Tick(0.1f));
            Assert.AreEqual(0, runner.Tick(2f));
            Assert.AreEqual(1, runner.Tick(1f));
            Assert.AreEqual(1, runner.Tick(3f));
            Assert.AreEqual(0, runner.Tick(100f));
            Assert.AreEqual(4, runner.Spawned);

            runner.NotifyEnemyRemoved();
            runner.NotifyEnemyRemoved();
            runner.NotifyEnemyRemoved();

            Assert.IsTrue(runner.IsWaveCleared);
            Assert.AreEqual(3f, runner.NightTimeScale, 0.0001f, "El resto de la noche sigue acelerado.");

            runner.Abort();
            Assert.AreEqual(1f, runner.NightTimeScale, 0.0001f);
            runner.Begin(1, 0f, 0f);
            Assert.AreEqual(0, runner.ReinforcementGroups, "Cada noche empieza de cero.");
        }

        [Test]
        public void ReinforcementRules_SizeAndTiming()
        {
            config.ConfigureReinforcements(fraction: 0.25f, minCount: 3, maxGroups: 2, window: 6f,
                minRemaining: 15f, fastForward: 3f, radiusFactor: 0.6f);

            Assert.AreEqual(3, WaveMath.GetReinforcementCount(config, 8), "25 % de 8 = 2 → se aplica el mínimo.");
            Assert.AreEqual(6, WaveMath.GetReinforcementCount(config, 22), "25 % de 22 = 5,5 → 6");

            Assert.IsTrue(WaveMath.ShouldSendReinforcements(config, 0, 40f));
            Assert.IsTrue(WaveMath.ShouldSendReinforcements(config, 1, 15f));
            Assert.IsFalse(WaveMath.ShouldSendReinforcements(config, 2, 40f), "Ya se mandaron los dos grupos.");
            Assert.IsFalse(WaveMath.ShouldSendReinforcements(config, 0, 14f), "Queda muy poca noche.");
        }

        [Test]
        public void Runner_Abort_ClearsTheWave_AndCanBeginAgain()
        {
            var runner = new WaveRunner();
            runner.Begin(3, 0f, 0f);
            Assert.AreEqual(3, runner.Tick(0.1f), "Con ventana 0 salen todos a la vez.");

            runner.Abort();

            Assert.IsTrue(runner.IsWaveCleared);
            Assert.AreEqual(0, runner.Alive);
            Assert.AreEqual(0, runner.Tick(10f));

            runner.Begin(1, 0f, 10f);
            Assert.IsFalse(runner.IsWaveCleared);
            Assert.AreEqual(1, runner.Tick(0.1f));
        }

        [Test]
        public void Match_Campaign_IsWonOnTheLastNight()
        {
            var match = new MatchState(campaign);
            var outcomes = new List<MatchOutcome>();
            match.Ended += outcomes.Add;

            match.NotifyNightCompleted(4);
            Assert.IsFalse(match.IsOver);

            match.NotifyNightCompleted(5);

            Assert.AreEqual(MatchOutcome.Victory, match.Outcome);
            Assert.AreEqual(5, match.NightsSurvived);
            Assert.AreEqual(1, outcomes.Count);
        }

        [Test]
        public void Match_Endless_NeverWins()
        {
            var match = new MatchState(endless);

            match.NotifyNightCompleted(50);

            Assert.IsFalse(match.IsOver);
            Assert.AreEqual(50, match.NightsSurvived);
        }

        [Test]
        public void Match_FirstResultIsFinal()
        {
            var match = new MatchState(campaign);
            int ended = 0;
            match.Ended += _ => ended++;

            match.NotifyNightCompleted(2);
            match.NotifyDefeat(DefeatReason.RvDestroyed);
            match.NotifyDefeat(DefeatReason.PlayerDied);
            match.NotifyNightCompleted(5);

            Assert.AreEqual(MatchOutcome.Defeat, match.Outcome);
            Assert.AreEqual(DefeatReason.RvDestroyed, match.DefeatReason);
            Assert.AreEqual(2, match.NightsSurvived);
            Assert.AreEqual(1, ended);
        }
    }
}
