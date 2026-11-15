using System;
using DeadAcres.GameMode;

namespace DeadAcres.Match
{
    public enum MatchOutcome : byte
    {
        Playing = 0,
        Victory = 1,
        Defeat = 2,
    }

    public enum DefeatReason : byte
    {
        None = 0,
        RvDestroyed = 1,
        PlayerDied = 2,
    }

    /// <summary>
    /// Reglas de fin de partida en C# puro. Se pierde si cae la caravana o el jugador; en Campaña
    /// se gana al sobrevivir las noches del modo. Una vez decidido el resultado, no cambia.
    /// </summary>
    public sealed class MatchState
    {
        private readonly GameModeRules rules;

        public MatchState(GameModeRules rules)
        {
            this.rules = rules;
        }

        public event Action<MatchOutcome> Ended;

        public MatchOutcome Outcome { get; private set; } = MatchOutcome.Playing;
        public DefeatReason DefeatReason { get; private set; } = DefeatReason.None;
        public int NightsSurvived { get; private set; }
        public bool IsOver => Outcome != MatchOutcome.Playing;

        public void NotifyNightCompleted(int nightNumber)
        {
            if (IsOver)
            {
                return;
            }

            NightsSurvived = Math.Max(NightsSurvived, nightNumber);
            if (rules != null && !rules.IsEndless && nightNumber >= rules.NightsToWin)
            {
                End(MatchOutcome.Victory, DefeatReason.None);
            }
        }

        public void NotifyDefeat(DefeatReason reason)
        {
            if (!IsOver)
            {
                End(MatchOutcome.Defeat, reason);
            }
        }

        private void End(MatchOutcome outcome, DefeatReason reason)
        {
            Outcome = outcome;
            DefeatReason = reason;
            Ended?.Invoke(outcome);
        }
    }
}
