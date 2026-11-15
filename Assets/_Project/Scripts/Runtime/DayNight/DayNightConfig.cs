using UnityEngine;

namespace DeadAcres.DayNight
{
    /// <summary>
    /// Duraciones del ciclo en segundos reales. Valores por defecto según el GDD (sección 2).
    /// </summary>
    [CreateAssetMenu(menuName = "Dead Acres/Config/Day Night Config", fileName = "DayNightConfig")]
    public sealed class DayNightConfig : ScriptableObject
    {
        [SerializeField, Min(1f)] private float dawnDuration = 15f;
        [SerializeField, Min(1f)] private float firstDayDuration = 360f;
        [SerializeField, Min(1f)] private float dayDuration = 240f;
        [SerializeField, Min(1f)] private float duskDuration = 30f;

        [Tooltip("La noche no puede terminar antes de este tiempo aunque no queden enemigos.")]
        [SerializeField, Min(1f)] private float minNightDuration = 120f;

        [Tooltip("Tope de seguridad: al llegar aquí amanece y los enemigos restantes se queman.")]
        [SerializeField, Min(1f)] private float maxNightDuration = 300f;

        public float DawnDuration => dawnDuration;
        public float DuskDuration => duskDuration;
        public float MinNightDuration => minNightDuration;
        public float MaxNightDuration => maxNightDuration;

        /// <param name="dayNumber">Día 1-based. El día 1 es más largo para montar la granja.</param>
        public float GetDayDuration(int dayNumber) => dayNumber <= 1 ? firstDayDuration : dayDuration;

        internal void Configure(float dawn, float firstDay, float day, float dusk, float minNight, float maxNight)
        {
            dawnDuration = dawn;
            firstDayDuration = firstDay;
            dayDuration = day;
            duskDuration = dusk;
            minNightDuration = minNight;
            maxNightDuration = maxNight;
            OnValidate();
        }

        private void OnValidate()
        {
            if (maxNightDuration < minNightDuration)
            {
                maxNightDuration = minNightDuration;
            }
        }
    }
}
