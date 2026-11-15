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

        /// <summary>Tamaño de un grupo de refuerzo para una oleada de <paramref name="waveCount"/> enemigos.</summary>
        public static int GetReinforcementCount(WaveConfig config, int waveCount)
        {
            int byFraction = Mathf.RoundToInt(waveCount * config.ReinforcementFraction);
            return Mathf.Max(config.ReinforcementMinCount, byFraction);
        }

        /// <summary>
        /// ¿Toca mandar refuerzos? Sí cuando la noche está en curso, no se ha llegado al máximo de
        /// grupos y todavía queda suficiente noche por delante.
        /// </summary>
        public static bool ShouldSendReinforcements(WaveConfig config, int groupsSent, float nightSecondsRemaining)
        {
            return groupsSent < config.MaxReinforcementGroups && nightSecondsRemaining >= config.MinNightRemaining;
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
