using UnityEngine;

namespace DeadAcres.DayNight
{
    /// <summary>
    /// Traduce fase + progreso a una hora de reloj para el HUD ("Día 1  07:28", como el original).
    /// Amanecer 05–06 h, día 06–19 h, atardecer 19–20 h, noche 20–05 h.
    /// </summary>
    public static class DayNightTime
    {
        public const float DawnStartHour = 5f;
        public const float DayStartHour = 6f;
        public const float DuskStartHour = 19f;
        public const float NightStartHour = 20f;

        /// <param name="progress">0..1 dentro de la fase. Si la noche se alarga, se queda en las 05:00.</param>
        /// <returns>Hora en [0, 24).</returns>
        public static float GetHour(GamePhase phase, float progress)
        {
            float t = Mathf.Clamp01(progress);
            float hour;
            switch (phase)
            {
                case GamePhase.Dawn:
                    hour = Mathf.Lerp(DawnStartHour, DayStartHour, t);
                    break;
                case GamePhase.Day:
                    hour = Mathf.Lerp(DayStartHour, DuskStartHour, t);
                    break;
                case GamePhase.Dusk:
                    hour = Mathf.Lerp(DuskStartHour, NightStartHour, t);
                    break;
                default:
                    hour = Mathf.Lerp(NightStartHour, DawnStartHour + 24f, t);
                    break;
            }

            return hour >= 24f ? hour - 24f : hour;
        }

        public static void ToHoursMinutes(float hour, out int hours, out int minutes)
        {
            int totalMinutes = Mathf.FloorToInt(Mathf.Max(0f, hour) * 60f);
            hours = totalMinutes / 60 % 24;
            minutes = totalMinutes % 60;
        }
    }
}
