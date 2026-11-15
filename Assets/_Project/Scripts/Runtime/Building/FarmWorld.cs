using DeadAcres.Core.Session;
using DeadAcres.GameMode;
using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>
    /// Dueño de la rejilla y del contador de límites de la partida.
    /// En co-op vivirá solo en el host; los clientes pedirán colocaciones por comando.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class FarmWorld : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField, Min(8)] private int width = 64;
        [SerializeField, Min(8)] private int depth = 64;
        [SerializeField, Min(0.25f)] private float cellSize = 1f;

        public PlacementGrid Grid { get; private set; }
        public PlacementLimiter Limiter { get; private set; }

        /// <summary>Fuentes de energía de la partida (caravana, generadores).</summary>
        public PowerMap Power { get; private set; }

        private void Awake()
        {
            // El transform marca la esquina (0,0) de la zona: así se puede mover la granja en la escena.
            Grid = new PlacementGrid(width, depth, transform.position, cellSize);
            Limiter = new PlacementLimiter(session.Mode);
            Power = new PowerMap();
        }

        internal void Configure(GameSession newSession, int newWidth, int newDepth, float newCellSize)
        {
            session = newSession;
            width = Mathf.Max(8, newWidth);
            depth = Mathf.Max(8, newDepth);
            cellSize = Mathf.Max(0.25f, newCellSize);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.6f);
            var size = new Vector3(width * cellSize, 0.05f, depth * cellSize);
            Gizmos.DrawWireCube(transform.position + size * 0.5f, size);
        }
#endif
    }
}
