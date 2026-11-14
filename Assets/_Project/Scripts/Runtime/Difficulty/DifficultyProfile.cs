using UnityEngine;

namespace DeadAcres.Difficulty
{
    /// <summary>
    /// Multiplicadores de una dificultad (GDD sección 7).
    /// La vida de enemigos se escala al spawnear; el daño se escala al recibirlo
    /// en el bando del jugador (jugador, RV, defensas, cultivos y animales).
    /// </summary>
    [CreateAssetMenu(menuName = "Dead Acres/Config/Difficulty Profile", fileName = "Difficulty_")]
    public sealed class DifficultyProfile : ScriptableObject
    {
        [SerializeField] private string displayName = "Medium";
        [SerializeField, TextArea] private string flavorText;
        [SerializeField, Min(0.1f)] private float enemyHealthMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float damageTakenMultiplier = 1f;

        public string DisplayName => displayName;
        public string FlavorText => flavorText;
        public float EnemyHealthMultiplier => enemyHealthMultiplier;
        public float DamageTakenMultiplier => damageTakenMultiplier;

        public float ScaleEnemyHealth(float baseHealth) => baseHealth * enemyHealthMultiplier;

        public float ScaleDamageTaken(float rawDamage) => rawDamage * damageTakenMultiplier;

        /// <summary>
        /// Texto para la pantalla de confirmación. Solo se llama desde el menú,
        /// así que la asignación de string no afecta al gameplay.
        /// </summary>
        public string BuildSummary()
        {
            return $"Vida de los enemigos: {FormatDelta(enemyHealthMultiplier)}\n" +
                   $"Daño que recibes tú, el RV y tus defensas: {FormatDelta(damageTakenMultiplier)}";
        }

        /// <summary>×1,5 → "+50 %", ×0,7 → "-30 %", ×1 → "normal".</summary>
        public static string FormatDelta(float multiplier)
        {
            int percent = Mathf.RoundToInt((multiplier - 1f) * 100f);
            if (percent == 0) return "normal";
            return percent > 0 ? $"+{percent} %" : $"{percent} %";
        }

        internal void Configure(string name, string flavor, float enemyHealth, float damageTaken)
        {
            displayName = name;
            flavorText = flavor;
            enemyHealthMultiplier = enemyHealth;
            damageTakenMultiplier = damageTaken;
        }
    }
}
