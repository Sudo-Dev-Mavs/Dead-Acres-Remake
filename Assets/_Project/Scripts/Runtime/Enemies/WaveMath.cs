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

        /// <summary>Zombis fuertes que suelta la cripta esa noche, además de la oleada normal.</summary>
        public static int GetStrongCount(WaveConfig config, int nightNumber)
        {
            if (config.StrongFromNight <= 0 || nightNumber < config.StrongFromNight)
            {
                return 0;
            }

            int count = (nightNumber - config.StrongFromNight + 1) * config.StrongPerNight;
            return Mathf.Clamp(count, 0, config.StrongMaxCount);
        }

        /// <summary>
        /// ¿La aparición número <paramref name="spawnIndex"/> (desde 0) de la noche es un zombi
        /// fuerte? Los reparte de forma uniforme entre las <paramref name="totalCount"/> apariciones.
        /// </summary>
        public static bool IsStrongSpawn(int spawnIndex, int totalCount, int strongCount)
        {
            if (strongCount <= 0 || totalCount <= 0 || spawnIndex < 0 || spawnIndex >= totalCount)
            {
                return false;
            }

            strongCount = Mathf.Min(strongCount, totalCount);
            return (spawnIndex + 1) * strongCount / totalCount > spawnIndex * strongCount / totalCount;
        }

        /// <summary>
        /// Crecimiento lineal por noche: 1 la primera, 1 + <paramref name="perNight"/> la segunda...
        /// Vale para la vida y para el daño de los zombis, en cualquier modo.
        /// </summary>
        public static float GetNightGrowth(float perNight, int nightNumber)
        {
            return 1f + Mathf.Max(0f, perNight) * (Mathf.Max(1, nightNumber) - 1);
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
