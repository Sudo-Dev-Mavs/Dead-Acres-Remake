using DeadAcres.DayNight;

namespace DeadAcres.Farming
{
    /// <summary>Cuánto avanza el crecimiento de los cultivos según la fase del día.</summary>
    public static class GrowthRules
    {
        /// <summary>
        /// Amanecer, día y atardecer cuentan como día (velocidad normal). Solo la fase de noche
        /// aplica <paramref name="nightMultiplier"/>.
        /// </summary>
        public static float GetTimeScale(GamePhase phase, float nightMultiplier)
        {
            if (phase != GamePhase.Night)
            {
                return 1f;
            }

            return nightMultiplier < 0f ? 0f : nightMultiplier > 1f ? 1f : nightMultiplier;
        }
    }
}
