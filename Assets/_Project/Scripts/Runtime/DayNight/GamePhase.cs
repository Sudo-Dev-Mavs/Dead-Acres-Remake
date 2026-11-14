namespace DeadAcres.DayNight
{
    public enum GamePhase : byte
    {
        Dawn = 0,
        Day = 1,
        Dusk = 2,
        Night = 3,
    }

    /// <summary>Payload del cambio de fase. Struct para no generar basura al emitirlo.</summary>
    public readonly struct PhaseChange
    {
        public readonly GamePhase Phase;
        public readonly int DayNumber;
        public readonly float Duration;

        public PhaseChange(GamePhase phase, int dayNumber, float duration)
        {
            Phase = phase;
            DayNumber = dayNumber;
            Duration = duration;
        }
    }

    /// <summary>Resultado de una noche terminada.</summary>
    public readonly struct NightResult
    {
        public readonly int NightNumber;

        /// <summary>True si la noche acabó por el tope de tiempo con enemigos aún vivos.</summary>
        public readonly bool EndedByTimeout;

        public NightResult(int nightNumber, bool endedByTimeout)
        {
            NightNumber = nightNumber;
            EndedByTimeout = endedByTimeout;
        }
    }
}
