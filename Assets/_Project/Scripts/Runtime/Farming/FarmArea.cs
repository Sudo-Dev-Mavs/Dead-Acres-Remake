using DeadAcres.DayNight;
using DeadAcres.GameMode;
using UnityEngine;

namespace DeadAcres.Farming
{
    /// <summary>
    /// Adaptador de escena de <see cref="FarmField"/>: define la zona cultivable (centrada en este
    /// objeto, en su plano XZ local), convierte posiciones del mundo en celdas y hace avanzar el
    /// crecimiento. Toda la lógica vive en el FarmField.
    /// </summary>
    public sealed class FarmArea : MonoBehaviour
    {
        [SerializeField] private CropCatalog catalog;

        [Tooltip("Opcional. Límite de plantas del modo de juego.")]
        [SerializeField] private PlacementLimits limits;

        [Tooltip("Opcional. Si se asigna, los cultivos no crecen mientras el reloj está en pausa.")]
        [SerializeField] private DayNightCycle dayNight;

        [SerializeField] private Vector2Int sizeInCells = new Vector2Int(24, 24);
        [SerializeField, Min(0.25f)] private float cellSize = 1f;

        private FarmField field;

        public FarmField Field
        {
            get
            {
                if (field == null)
                {
                    var bounds = new RectInt(0, 0, sizeInCells.x, sizeInCells.y);
                    field = new FarmField(bounds, limits != null ? limits.Limiter : null);
                }

                return field;
            }
        }

        public CropCatalog Catalog => catalog;
        public float CellSize => cellSize;
        public Vector2Int SizeInCells => sizeInCells;

        private void Update()
        {
            if (dayNight != null && dayNight.Clock != null && dayNight.Clock.IsPaused)
            {
                return;
            }

            Field.Tick(Time.deltaTime);
        }

        /// <returns>False si el punto cae fuera de la zona cultivable.</returns>
        public bool TryWorldToCell(Vector3 worldPosition, out Vector2Int cell)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            cell = new Vector2Int(
                Mathf.FloorToInt(local.x / cellSize + sizeInCells.x * 0.5f),
                Mathf.FloorToInt(local.z / cellSize + sizeInCells.y * 0.5f));
            return Field.Bounds.Contains(cell);
        }

        /// <summary>Centro de la celda, a ras de suelo.</summary>
        public Vector3 CellToWorld(Vector2Int cell)
        {
            var local = new Vector3(
                (cell.x + 0.5f - sizeInCells.x * 0.5f) * cellSize,
                0f,
                (cell.y + 0.5f - sizeInCells.y * 0.5f) * cellSize);
            return transform.TransformPoint(local);
        }

        internal void Configure(CropCatalog newCatalog, PlacementLimits newLimits, DayNightCycle cycle,
            Vector2Int newSizeInCells, float newCellSize)
        {
            catalog = newCatalog;
            limits = newLimits;
            dayNight = cycle;
            sizeInCells = newSizeInCells;
            cellSize = Mathf.Max(0.25f, newCellSize);
            field = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.9f, 0.7f, 0.2f, 1f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(sizeInCells.x * cellSize, 0.05f, sizeInCells.y * cellSize));
        }
    }
}
