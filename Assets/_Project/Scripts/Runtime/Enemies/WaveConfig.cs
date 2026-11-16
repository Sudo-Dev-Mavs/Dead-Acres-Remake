using UnityEngine;

namespace DeadAcres.Enemies
{
    /// <summary>
    /// Tamaño y ritmo de las oleadas nocturnas. Valores provisionales: el original no publica los suyos.
    /// </summary>
    [CreateAssetMenu(menuName = "Dead Acres/Config/Wave Config", fileName = "WaveConfig")]
    public sealed class WaveConfig : ScriptableObject
    {
        [Tooltip("Enemigos de la primera noche.")]
        [SerializeField, Min(1)] private int firstNightCount = 12;

        [Tooltip("Enemigos que se añaden cada noche en Campaña.")]
        [SerializeField, Min(0)] private int extraPerNight = 5;

        [Tooltip("Segundos desde que anochece hasta el primer enemigo.")]
        [SerializeField, Min(0f)] private float initialDelay = 3f;

        [Tooltip("Segundos en los que se reparte la aparición de toda la oleada.")]
        [SerializeField, Min(1f)] private float spawnWindow = 60f;

        [Header("Refuerzos (si se despeja la oleada antes de tiempo)")]
        [Tooltip("Tamaño de cada grupo de refuerzo, como fracción de la oleada de esa noche.")]
        [SerializeField, Range(0f, 1f)] private float reinforcementFraction = 0.25f;

        [SerializeField, Min(1)] private int reinforcementMinCount = 3;

        [Tooltip("Grupos de refuerzo como máximo por noche. 0 = sin refuerzos ni aceleración.")]
        [SerializeField, Min(0)] private int maxReinforcementGroups = 2;

        [Tooltip("Segundos en los que aparece un grupo de refuerzo.")]
        [SerializeField, Min(0f)] private float reinforcementWindow = 6f;

        [Tooltip("Solo se mandan refuerzos si a la noche le queda al menos este tiempo (segundos de reloj).")]
        [SerializeField, Min(0f)] private float minNightRemaining = 15f;

        [Tooltip("Velocidad del resto de la noche desde que llegan los primeros refuerzos (3 = el triple).")]
        [SerializeField, Min(1f)] private float nightFastForward = 3f;

        [Tooltip("Los refuerzos aparecen más cerca: fracción del radio de aparición normal.")]
        [SerializeField, Range(0.2f, 1f)] private float reinforcementRadiusFactor = 0.6f;

        [Header("Zombis fuertes (salen de la cripta)")]
        [Tooltip("Primera noche en la que la cripta suelta zombis fuertes. 0 = nunca.")]
        [SerializeField, Min(0)] private int strongFromNight = 2;

        [Tooltip("Cuántos más cada noche a partir de esa.")]
        [SerializeField, Min(0)] private int strongPerNight = 1;

        [SerializeField, Min(0)] private int strongMaxCount = 8;

        [Header("Crecimiento por noche (todos los modos)")]
        [Tooltip("Vida extra de los zombis por cada noche que pasa (0,2 = +20 % de la base por noche).")]
        [SerializeField, Min(0f)] private float healthGrowthPerNight = 0.2f;

        [Tooltip("Daño extra de los zombis por cada noche que pasa.")]
        [SerializeField, Min(0f)] private float damageGrowthPerNight = 0.15f;

        public float HealthGrowthPerNight => healthGrowthPerNight;
        public float DamageGrowthPerNight => damageGrowthPerNight;
        public int StrongFromNight => strongFromNight;
        public int StrongPerNight => strongPerNight;
        public int StrongMaxCount => strongMaxCount;
        public int FirstNightCount => firstNightCount;
        public float ReinforcementFraction => reinforcementFraction;
        public int ReinforcementMinCount => reinforcementMinCount;
        public int MaxReinforcementGroups => maxReinforcementGroups;
        public float ReinforcementWindow => reinforcementWindow;
        public float MinNightRemaining => minNightRemaining;
        public float NightFastForward => nightFastForward;
        public float ReinforcementRadiusFactor => reinforcementRadiusFactor;
        public int ExtraPerNight => extraPerNight;
        public float InitialDelay => initialDelay;
        public float SpawnWindow => spawnWindow;

        internal void Configure(int firstNight, int perNight, float delay, float window)
        {
            firstNightCount = Mathf.Max(1, firstNight);
            extraPerNight = Mathf.Max(0, perNight);
            initialDelay = Mathf.Max(0f, delay);
            spawnWindow = Mathf.Max(1f, window);
        }

        internal void ConfigureGrowth(float healthPerNight, float damagePerNight)
        {
            healthGrowthPerNight = Mathf.Max(0f, healthPerNight);
            damageGrowthPerNight = Mathf.Max(0f, damagePerNight);
        }

        internal void ConfigureStrong(int fromNight, int perNight, int maxCount)
        {
            strongFromNight = Mathf.Max(0, fromNight);
            strongPerNight = Mathf.Max(0, perNight);
            strongMaxCount = Mathf.Max(0, maxCount);
        }

        internal void ConfigureReinforcements(float fraction, int minCount, int maxGroups, float window,
            float minRemaining, float fastForward, float radiusFactor)
        {
            reinforcementFraction = Mathf.Clamp01(fraction);
            reinforcementMinCount = Mathf.Max(1, minCount);
            maxReinforcementGroups = Mathf.Max(0, maxGroups);
            reinforcementWindow = Mathf.Max(0f, window);
            minNightRemaining = Mathf.Max(0f, minRemaining);
            nightFastForward = Mathf.Max(1f, fastForward);
            reinforcementRadiusFactor = Mathf.Clamp(radiusFactor, 0.2f, 1f);
        }
    }
}
