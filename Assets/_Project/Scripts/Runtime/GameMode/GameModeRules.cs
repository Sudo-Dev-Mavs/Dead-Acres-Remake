using UnityEngine;

namespace DeadAcres.GameMode
{
    public enum PlaceableCategory : byte
    {
        /// <summary>Cultivos.</summary>
        Plant = 0,

        /// <summary>Muros, puertas, torretas, generadores, luces, trampas, tractor...</summary>
        Object = 1,
    }

    /// <summary>
    /// Reglas de un modo de juego. Campaña: 100/100 y 5 noches. Endless: 500/500 y noches infinitas.
    /// </summary>
    [CreateAssetMenu(menuName = "Dead Acres/Config/Game Mode Rules", fileName = "GameMode_")]
    public sealed class GameModeRules : ScriptableObject
    {
        public const int CategoryCount = 2;

        [SerializeField] private string displayName = "Campaña";
        [SerializeField, Min(1)] private int plantLimit = 100;
        [SerializeField, Min(1)] private int objectLimit = 100;

        [Tooltip("Noches a sobrevivir para ganar. 0 = Endless.")]
        [SerializeField, Min(0)] private int nightsToWin = 5;

        [Tooltip("Modo de una vida: jugador y caravana con 1 de vida.")]
        [SerializeField] private bool oneLife;

        [Header("Solo Endless")]
        [Tooltip("Crecimiento compuesto del número de enemigos por noche (0,12 = +12 %).")]
        [SerializeField, Min(0f)] private float enemyCountGrowthPerNight = 0.12f;

        [Tooltip("Crecimiento compuesto de la vida de los enemigos por noche (0,05 = +5 %).")]
        [SerializeField, Min(0f)] private float enemyHealthGrowthPerNight = 0.05f;

        public string DisplayName => displayName;
        public int NightsToWin => nightsToWin;
        public bool IsEndless => nightsToWin <= 0;

        /// <summary>Una vida: el jugador y la caravana caen de un golpe y morir borra la partida guardada.</summary>
        public bool IsOneLife => oneLife;
        public float EnemyCountGrowthPerNight => enemyCountGrowthPerNight;
        public float EnemyHealthGrowthPerNight => enemyHealthGrowthPerNight;

        public int GetLimit(PlaceableCategory category) =>
            category == PlaceableCategory.Plant ? plantLimit : objectLimit;

        internal void ConfigureOneLife(bool enabled) => oneLife = enabled;

        internal void Configure(string name, int plants, int objects, int nights,
            float countGrowth = 0.12f, float healthGrowth = 0.05f)
        {
            displayName = name;
            plantLimit = plants;
            objectLimit = objects;
            nightsToWin = nights;
            enemyCountGrowthPerNight = countGrowth;
            enemyHealthGrowthPerNight = healthGrowth;
        }
    }
}
