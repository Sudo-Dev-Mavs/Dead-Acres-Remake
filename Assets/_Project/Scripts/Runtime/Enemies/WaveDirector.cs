using System.Collections.Generic;
using DeadAcres.Combat;
using DeadAcres.Core.Session;
using DeadAcres.DayNight;
using UnityEngine;

namespace DeadAcres.Enemies
{
    /// <summary>
    /// Adaptador de escena de <see cref="WaveRunner"/>: al anochecer arranca la oleada, hace
    /// aparecer a los zombis alrededor del mapa y, al amanecer, quema a los que queden.
    /// El reloj lo consulta (como <see cref="IWaveStatus"/>) para saber si la noche puede acabar.
    /// </summary>
    public sealed class WaveDirector : MonoBehaviour, IWaveStatus
    {
        [SerializeField] private WaveConfig config;
        [SerializeField] private GameSession session;
        [SerializeField] private PhaseEventChannel phaseChanged;
        [SerializeField] private NightResultEventChannel nightCompleted;
        [SerializeField] private ZombieBrain zombiePrefab;

        [Header("Objetivos")]
        [Tooltip("La caravana.")]
        [SerializeField] private Health primaryTarget;
        [SerializeField] private List<Health> players = new List<Health>();

        [Header("Aparición")]
        [Tooltip("Centro del círculo en el que aparecen. Vacío = este objeto.")]
        [SerializeField] private Transform spawnCenter;
        [SerializeField, Min(5f)] private float spawnRadius = 45f;

        private readonly WaveRunner runner = new WaveRunner();
        private readonly List<Health> alive = new List<Health>(64);
        private float healthMultiplier = 1f;

        public bool IsWaveCleared => runner.IsWaveCleared;
        public int AliveCount => runner.Alive;

        private void OnEnable()
        {
            if (phaseChanged != null) phaseChanged.Subscribe(OnPhaseChanged);
            if (nightCompleted != null) nightCompleted.Subscribe(OnNightCompleted);
        }

        private void OnDisable()
        {
            if (phaseChanged != null) phaseChanged.Unsubscribe(OnPhaseChanged);
            if (nightCompleted != null) nightCompleted.Unsubscribe(OnNightCompleted);
        }

        private void Update()
        {
            int toSpawn = runner.Tick(Time.deltaTime);
            for (int i = 0; i < toSpawn; i++)
            {
                Spawn();
            }
        }

        private void OnPhaseChanged(PhaseChange change)
        {
            if (change.Phase != GamePhase.Night || config == null || zombiePrefab == null)
            {
                return;
            }

            var rules = session != null ? session.Mode : null;
            healthMultiplier = WaveMath.GetHealthMultiplier(rules, change.DayNumber);
            runner.Begin(WaveMath.GetEnemyCount(config, rules, change.DayNumber), config.InitialDelay, config.SpawnWindow);
        }

        // Amanece: los que queden se queman, como dice la configuración del ciclo.
        private void OnNightCompleted(NightResult _)
        {
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                Health zombie = alive[i];
                if (zombie != null)
                {
                    zombie.Died -= OnZombieDied;
                    Destroy(zombie.gameObject);
                }
            }

            alive.Clear();
            runner.Abort();
        }

        private void Spawn()
        {
            Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;
            float angle = Random.value * Mathf.PI * 2f;
            var position = new Vector3(center.x + Mathf.Cos(angle) * spawnRadius, center.y + 0.1f,
                center.z + Mathf.Sin(angle) * spawnRadius);
            Quaternion facing = Quaternion.LookRotation(new Vector3(center.x - position.x, 0f, center.z - position.z));

            ZombieBrain zombie = Instantiate(zombiePrefab, position, facing, transform);
            zombie.Initialize(primaryTarget, players);

            var health = zombie.GetComponent<Health>();
            health.ResetHealth(healthMultiplier);
            health.Died += OnZombieDied;
            alive.Add(health);
        }

        private void OnZombieDied(Health zombie)
        {
            zombie.Died -= OnZombieDied;
            alive.Remove(zombie);
            runner.NotifyEnemyRemoved();
            Destroy(zombie.gameObject, 0.15f);
        }

        internal void Configure(WaveConfig waveConfig, GameSession gameSession, PhaseEventChannel phaseChannel,
            NightResultEventChannel nightChannel, ZombieBrain prefab, Health rv, List<Health> playerTargets,
            Transform center, float radius)
        {
            config = waveConfig;
            session = gameSession;
            phaseChanged = phaseChannel;
            nightCompleted = nightChannel;
            zombiePrefab = prefab;
            primaryTarget = rv;
            players = playerTargets;
            spawnCenter = center;
            spawnRadius = Mathf.Max(5f, radius);
        }
    }
}
