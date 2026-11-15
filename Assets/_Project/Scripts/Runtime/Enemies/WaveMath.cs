using DeadAcres.GameMode;
using UnityEngine;

namespace DeadAcres.Enemies
{
    /// <summary>Cuántos enemigos trae cada noche y cuánto aguantan. Funciones puras.</summary>
    public static class WaveMath
    {
        /// <summary>
        /// Campaña: crecimiento lineal. Endless: además crece de forma compuesta con el
        /// porcentaje del modo (p. ej. +12 % por noche).
        /// </summary>
        public static int GetEnemyCount(WaveConfig config, GameModeRules rules, int nightNumber)
        {
            int night = Mathf.Max(1, nightNumber);
            float count = config.FirstNightCount + config.ExtraPerNight * (night - 1);

            if (rules != null && rules.IsEndless)
            {
                count *= Mathf.Pow(1f + rules.EnemyCountGrowthPerNight, night - 1);
            }

            return Mathf.Max(1, Mathf.RoundToInt(count));
        }

        /// <summary>Multiplicador de vida extra por noche. Solo Endless; en Campaña siempre 1.</summary>
        public static float GetHealthMultiplier(GameModeRules rules, int nightNumber)
        {
            if (rules == null || !rules.IsEndless)
            {
                return 1f;
            }

            return Mathf.Pow(1f + rules.EnemyHealthGrowthPerNight, Mathf.Max(1, nightNumber) - 1);
        }
    }
}
