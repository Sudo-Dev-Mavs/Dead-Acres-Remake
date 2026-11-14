using UnityEngine;

namespace DeadAcres.DayNight
{
    /// <summary>
    /// Adaptador de escena para <see cref="DayNightClock"/>: le pasa el tiempo, publica
    /// sus eventos en los canales y gira el sol. Toda la lógica vive en el reloj.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class DayNightCycle : MonoBehaviour
    {
        [SerializeField] private DayNightConfig config;

        [Tooltip("Componente que implementa IWaveStatus (el WaveDirector). Vacío = la noche dura el mínimo.")]
        [SerializeField] private MonoBehaviour waveStatusSource;

        [Header("Canales de salida")]
        [SerializeField] private PhaseEventChannel phaseChanged;
        [SerializeField] private NightResultEventChannel nightCompleted;

        [Header("Visual (opcional)")]
        [SerializeField] private Transform sun;

        [Tooltip("Inclinación del sol en grados (eje Y fijo).")]
        [SerializeField] private float sunYaw = -30f;

        private DayNightClock clock;

        public DayNightClock Clock => clock;

        private void Awake()
        {
            var waveStatus = waveStatusSource as IWaveStatus;
            if (waveStatusSource != null && waveStatus == null)
            {
                Debug.LogError($"{waveStatusSource.name} no implementa IWaveStatus.", this);
            }

            clock = new DayNightClock(config, waveStatus);
            clock.PhaseChanged += OnPhaseChanged;
            clock.NightCompleted += OnNightCompleted;
        }

        private void Start() => clock.Begin();

        private void OnDestroy()
        {
            if (clock == null) return;
            clock.PhaseChanged -= OnPhaseChanged;
            clock.NightCompleted -= OnNightCompleted;
        }

        private void Update()
        {
            clock.Tick(Time.deltaTime);

            if (sun != null)
            {
                sun.rotation = Quaternion.Euler(GetSunPitch(), sunYaw, 0f);
            }
        }

        /// <summary>Para el menú de pausa, la tienda y el inventario (solo un jugador).</summary>
        public void SetPaused(bool paused) => clock.IsPaused = paused;

        /// <summary>Botón "Listo".</summary>
        public bool TrySkipDay() => clock.TrySkipDay();

        internal void Configure(DayNightConfig newConfig, PhaseEventChannel phaseChannel,
            NightResultEventChannel nightChannel, Transform sunTransform)
        {
            config = newConfig;
            phaseChanged = phaseChannel;
            nightCompleted = nightChannel;
            sun = sunTransform;
        }

        private void OnPhaseChanged(PhaseChange change)
        {
            if (phaseChanged != null) phaseChanged.Raise(change);
        }

        private void OnNightCompleted(NightResult result)
        {
            if (nightCompleted != null) nightCompleted.Raise(result);
        }

        /// <summary>
        /// Arco del sol: amanecer 0°→10°, día 10°→170°, atardecer 170°→180°, noche bajo el horizonte.
        /// La luz nocturna (luna) va en otra luz; aquí solo se mueve el sol.
        /// </summary>
        private float GetSunPitch()
        {
            float t = clock.PhaseProgress;
            switch (clock.Phase)
            {
                case GamePhase.Dawn: return Mathf.Lerp(0f, 10f, t);
                case GamePhase.Day: return Mathf.Lerp(10f, 170f, t);
                case GamePhase.Dusk: return Mathf.Lerp(170f, 180f, t);
                default: return Mathf.Lerp(180f, 360f, t);
            }
        }
    }
}
