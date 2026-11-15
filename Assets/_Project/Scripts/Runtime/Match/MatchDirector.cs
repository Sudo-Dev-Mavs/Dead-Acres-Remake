using System;
using DeadAcres.Combat;
using DeadAcres.Core.Events;
using DeadAcres.Core.Session;
using DeadAcres.DayNight;
using DeadAcres.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DeadAcres.Match
{
    /// <summary>
    /// Adaptador de escena de <see cref="MatchState"/>: escucha la vida de la caravana y del
    /// jugador y las noches superadas, y al acabar la partida detiene el reloj y el input.
    /// Con la partida terminada, Enter recarga la escena.
    /// </summary>
    public sealed class MatchDirector : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private DayNightCycle dayNight;
        [SerializeField] private NightResultEventChannel nightCompleted;
        [SerializeField] private Health rvHealth;
        [SerializeField] private Health playerHealth;
        [SerializeField] private PlayerInputReader playerInput;

        [Header("Canales de salida (opcionales)")]
        [SerializeField] private VoidEventChannel rvDestroyed;
        [SerializeField] private VoidEventChannel victory;

        private MatchState state;

        /// <summary>La partida terminó. El estado completo está en <see cref="State"/>.</summary>
        public event Action<MatchState> Ended;

        public MatchState State
        {
            get
            {
                if (state == null)
                {
                    state = new MatchState(session != null ? session.Mode : null);
                    state.Ended += OnEnded;
                }

                return state;
            }
        }

        private void OnEnable()
        {
            if (nightCompleted != null) nightCompleted.Subscribe(OnNightCompleted);
            if (rvHealth != null) rvHealth.Died += OnRvDied;
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            if (nightCompleted != null) nightCompleted.Unsubscribe(OnNightCompleted);
            if (rvHealth != null) rvHealth.Died -= OnRvDied;
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
        }

        // Con la partida acabada el input de juego está apagado: la tecla se lee del teclado.
        private void Update()
        {
            if (state == null || !state.IsOver)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.enterKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void OnNightCompleted(NightResult result) => State.NotifyNightCompleted(result.NightNumber);

        private void OnRvDied(Health _) => State.NotifyDefeat(DefeatReason.RvDestroyed);

        private void OnPlayerDied(Health _) => State.NotifyDefeat(DefeatReason.PlayerDied);

        private void OnEnded(MatchOutcome outcome)
        {
            if (dayNight != null && dayNight.Clock != null) dayNight.Clock.Stop();
            if (playerInput != null) playerInput.enabled = false;

            if (outcome == MatchOutcome.Victory)
            {
                if (victory != null) victory.Raise();
            }
            else if (state.DefeatReason == DefeatReason.RvDestroyed && rvDestroyed != null)
            {
                rvDestroyed.Raise();
            }

            Ended?.Invoke(state);
        }

        internal void Configure(GameSession gameSession, DayNightCycle cycle, NightResultEventChannel nightChannel,
            Health rv, Health player, PlayerInputReader input, VoidEventChannel rvDestroyedChannel,
            VoidEventChannel victoryChannel)
        {
            session = gameSession;
            dayNight = cycle;
            nightCompleted = nightChannel;
            rvHealth = rv;
            playerHealth = player;
            playerInput = input;
            rvDestroyed = rvDestroyedChannel;
            victory = victoryChannel;
        }
    }
}
