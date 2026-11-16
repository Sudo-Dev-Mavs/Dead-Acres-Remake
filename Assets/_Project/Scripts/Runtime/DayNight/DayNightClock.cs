using System;

namespace DeadAcres.DayNight
{
    /// <summary>
    /// Máquina de estados del ciclo Amanecer → Día → Atardecer → Noche, en C# puro
    /// (sin MonoBehaviour) para poder testearla y, en co-op, ejecutarla solo en el host.
    /// </summary>
    public sealed class DayNightClock
    {
        private readonly DayNightConfig config;
        private readonly IWaveStatus waveStatus;

        public DayNightClock(DayNightConfig config, IWaveStatus waveStatus)
        {
            this.config = config ? config : throw new ArgumentNullException(nameof(config));
            this.waveStatus = waveStatus ?? NullWaveStatus.Instance;
        }

        public event Action<PhaseChange> PhaseChanged;
        public event Action<NightResult> NightCompleted;

        public GamePhase Phase { get; private set; }

        /// <summary>Día actual, 1-based. La noche N pertenece al día N.</summary>
        public int DayNumber { get; private set; }

        public float PhaseElapsed { get; private set; }
        public float PhaseDuration { get; private set; }
        public bool IsRunning { get; private set; }

        /// <summary>En un jugador se pausa con tienda, inventario o menú abiertos. En co-op nunca.</summary>
        public bool IsPaused { get; set; }

        /// <summary>0..1 dentro de la fase actual. En la noche se mide contra el mínimo.</summary>
        public float PhaseProgress => PhaseDuration > 0f ? Math.Min(PhaseElapsed / PhaseDuration, 1f) : 1f;

        public void Begin(int startDay = 1)
        {
            DayNumber = Math.Max(1, startDay);
            IsRunning = true;
            EnterPhase(GamePhase.Dawn);
        }

        public void Stop() => IsRunning = false;

        public void Tick(float deltaTime)
        {
            if (!IsRunning || IsPaused || deltaTime <= 0f)
            {
                return;
            }

            PhaseElapsed += GetStep(deltaTime);

            switch (Phase)
            {
                case GamePhase.Dawn:
                    if (PhaseElapsed >= PhaseDuration) EnterPhase(GamePhase.Day);
                    break;

                case GamePhase.Day:
                    if (PhaseElapsed >= PhaseDuration) EnterPhase(GamePhase.Dusk);
                    break;

                case GamePhase.Dusk:
                    if (PhaseElapsed >= PhaseDuration) EnterPhase(GamePhase.Night);
                    break;

                case GamePhase.Night:
                    TickNight();
                    break;
            }
        }

        /// <summary>Botón "Listo": salta del día al atardecer.</summary>
        /// <returns>False si no es de día.</returns>
        public bool TrySkipDay()
        {
            if (!IsRunning || Phase != GamePhase.Day)
            {
                return false;
            }

            EnterPhase(GamePhase.Dusk);
            return true;
        }

        /// <summary>
        /// De noche, el sistema de oleadas puede acelerar el tiempo, pero solo hasta la duración
        /// mínima: a partir de ahí (enemigos aún vivos) el reloj vuelve a ir a velocidad normal,
        /// para que el tope máximo siga siendo tiempo real.
        /// </summary>
        // De noche el tiempo corre a la velocidad que marque la oleada (más rápido cuando ya se
        // despejó una vez). Nunca se detiene a esperar a que caiga el último zombi.
        private float GetStep(float deltaTime)
        {
            return Phase == GamePhase.Night ? deltaTime * Math.Max(1f, waveStatus.NightTimeScale) : deltaTime;
        }

        private void TickNight()
        {
            bool timedOut = PhaseElapsed >= config.MaxNightDuration;
            bool cleared = PhaseElapsed >= config.MinNightDuration && waveStatus.IsWaveCleared;

            if (!timedOut && !cleared)
            {
                return;
            }

            int finishedNight = DayNumber;
            DayNumber++;
            NightCompleted?.Invoke(new NightResult(finishedNight, timedOut && !waveStatus.IsWaveCleared));

            // Un oyente de NightCompleted (p. ej. victoria en la noche 5) puede haber parado el reloj.
            if (IsRunning)
            {
                EnterPhase(GamePhase.Dawn);
            }
        }

        private void EnterPhase(GamePhase phase)
        {
            Phase = phase;
            PhaseElapsed = 0f;
            PhaseDuration = GetDuration(phase);
            PhaseChanged?.Invoke(new PhaseChange(phase, DayNumber, PhaseDuration));
        }

        private float GetDuration(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Dawn: return config.DawnDuration;
                case GamePhase.Day: return config.GetDayDuration(DayNumber);
                case GamePhase.Dusk: return config.DuskDuration;
                case GamePhase.Night: return config.MaxNightDuration;
                default: return 0f;
            }
        }
    }
}
