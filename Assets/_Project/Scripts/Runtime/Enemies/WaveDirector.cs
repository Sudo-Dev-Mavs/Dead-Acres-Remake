using System;
using System.Collections.Generic;
using DeadAcres.Animals;
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

        [Header("Cripta")]
        [Tooltip("Opcional. El zombi fuerte.")]
        [SerializeField] private ZombieBrain strongZombiePrefab;

        [Tooltip("De dónde salen los zombis fuertes.")]
        [SerializeField] private Transform crypt;

        [Tooltip("Para saber cuánta noche queda al decidir si se mandan refuerzos.")]
        [SerializeField] private DayNightCycle dayNight;

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
        private readonly List<Health> nearbyTargets = new List<Health>(16);
        private readonly List<Health> animals = new List<Health>(16);
        private int animalsFrame = -1;
        private float healthMultiplier = 1f;
        private int waveCount;
        private bool spawningReinforcements;
        private int strongCount;
        private int spawnedThisWave;

        /// <summary>Empieza una noche en la que la cripta suelta este número de zombis fuertes.</summary>
        public event Action<int> StrongIncoming;

        /// <summary>Murió un zombi (no cuenta a los que se queman al amanecer).</summary>
        public event Action ZombieKilled;

        /// <summary>Llega un grupo de refuerzo de este tamaño y la noche se acelera.</summary>
        public event Action<int> ReinforcementsIncoming;

        public bool IsWaveCleared => runner.IsWaveCleared;
        public float NightTimeScale => runner.NightTimeScale;
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

            TrySendReinforcements();
        }

        // Si el jugador limpia la oleada con noche de sobra, llegan unos cuantos más y lo que
        // queda de noche pasa más rápido: nada de minutos mirando el campo vacío.
        private void TrySendReinforcements()
        {
            if (!runner.IsActive || !runner.IsWaveCleared || config == null)
            {
                return;
            }

            DayNightClock clock = dayNight != null ? dayNight.Clock : null;
            if (clock == null || clock.Phase != GamePhase.Night)
            {
                return;
            }

            float remaining = clock.PhaseDuration - clock.PhaseElapsed;
            if (!WaveMath.ShouldSendReinforcements(config, runner.ReinforcementGroups, remaining))
            {
                return;
            }

            int count = WaveMath.GetReinforcementCount(config, waveCount);
            if (runner.AddReinforcements(count, config.ReinforcementWindow, config.NightFastForward))
            {
                spawningReinforcements = true;
                ReinforcementsIncoming?.Invoke(count);
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
            waveCount = WaveMath.GetEnemyCount(config, rules, change.DayNumber);
            strongCount = strongZombiePrefab != null && crypt != null
                ? WaveMath.GetStrongCount(config, change.DayNumber)
                : 0;
            spawnedThisWave = 0;
            spawningReinforcements = false;

            // Los fuertes van aparte de la oleada normal, pero también hay que matarlos para
            // que la noche cuente como despejada.
            runner.Begin(waveCount + strongCount, config.InitialDelay, config.SpawnWindow);
            if (strongCount > 0) StrongIncoming?.Invoke(strongCount);
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
            // Los refuerzos son siempre zombis normales.
            if (!spawningReinforcements)
            {
                int index = spawnedThisWave++;
                if (WaveMath.IsStrongSpawn(index, waveCount + strongCount, strongCount))
                {
                    SpawnFromCrypt();
                    return;
                }
            }

            Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;
            float radius = spawningReinforcements ? spawnRadius * config.ReinforcementRadiusFactor : spawnRadius;
            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            var position = new Vector3(center.x + Mathf.Cos(angle) * radius, center.y + 0.1f,
                center.z + Mathf.Sin(angle) * radius);
            Quaternion facing = Quaternion.LookRotation(new Vector3(center.x - position.x, 0f, center.z - position.z));

            Register(Instantiate(zombiePrefab, position, facing, transform));
        }

        private void SpawnFromCrypt()
        {
            // Un poco repartidos delante de la puerta para que no salgan uno encima de otro.
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.5f;
            Vector3 position = crypt.position + crypt.forward * 2.5f + new Vector3(offset.x, 0.1f, offset.y);
            Register(Instantiate(strongZombiePrefab, position, crypt.rotation, transform));
        }

        private void Register(ZombieBrain zombie)
        {
            zombie.Initialize(primaryTarget, GetNearbyTargets());

            var health = zombie.GetComponent<Health>();
            health.ResetHealth(healthMultiplier);
            health.Died += OnZombieDied;
            alive.Add(health);
        }

        // Jugadores y animales que sigan vivos: a quién puede desviarse un zombi.
        private List<Health> GetNearbyTargets()
        {
            RefreshAnimals();
            nearbyTargets.Clear();
            AddLiving(players);
            AddLiving(animals);
            return nearbyTargets;
        }

        // Los animales se compran, se recolocan y mueren: se buscan en la escena, como mucho una
        // vez por frame aunque salgan varios zombis a la vez.
        private void RefreshAnimals()
        {
            if (animalsFrame == Time.frameCount)
            {
                return;
            }

            animalsFrame = Time.frameCount;
            animals.Clear();
            Animal[] found = FindObjectsByType<Animal>(FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i].TryGetComponent(out Health animalHealth))
                {
                    animals.Add(animalHealth);
                }
            }
        }

        private void AddLiving(List<Health> source)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null && !source[i].IsDead)
                {
                    nearbyTargets.Add(source[i]);
                }
            }
        }

        private void OnZombieDied(Health zombie)
        {
            zombie.Died -= OnZombieDied;
            alive.Remove(zombie);
            runner.NotifyEnemyRemoved();
            ZombieKilled?.Invoke();
            Destroy(zombie.gameObject, 0.15f);
        }

        internal void ConfigureCrypt(ZombieBrain strongPrefab, Transform cryptDoor)
        {
            strongZombiePrefab = strongPrefab;
            crypt = cryptDoor;
        }

        internal void Configure(WaveConfig waveConfig, GameSession gameSession, PhaseEventChannel phaseChannel,
            NightResultEventChannel nightChannel, ZombieBrain prefab, Health rv, List<Health> playerTargets,
            Transform center, float radius, DayNightCycle cycle)
        {
            dayNight = cycle;
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
