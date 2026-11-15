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

        public int FirstNightCount => firstNightCount;
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
    }
}
