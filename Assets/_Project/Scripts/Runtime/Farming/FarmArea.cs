using DeadAcres.Building;
using DeadAcres.DayNight;
using UnityEngine;

namespace DeadAcres.Farming
{
    /// <summary>
    /// Adaptador de escena de <see cref="FarmField"/>. Usa la rejilla y el contador de límites de
    /// <see cref="FarmWorld"/>, así cultivos y objetos colocados comparten las mismas celdas.
    /// Hace avanzar el crecimiento; toda la lógica vive en el FarmField.
    /// </summary>
    public sealed class FarmArea : MonoBehaviour
    {
        [SerializeField] private FarmWorld world;
        [SerializeField] private CropCatalog catalog;

        [Tooltip("Opcional. Si se asigna, los cultivos no crecen con el reloj en pausa y crecen más despacio de noche.")]
        [SerializeField] private DayNightCycle dayNight;

        [Tooltip("Velocidad de crecimiento durante la noche respecto al día (0,1 = una décima).")]
        [SerializeField, Range(0f, 1f)] private float nightGrowthMultiplier = 0.1f;

        private FarmField field;

        /// <summary>Se crea al primer uso; FarmWorld ya ha creado su rejilla en Awake (orden -90).</summary>
        public FarmField Field
        {
            get
            {
                if (field == null)
                {
                    PlacementGrid grid = world.Grid;
                    field = new FarmField(new RectInt(0, 0, grid.Width, grid.Depth), world.Limiter, null, grid);
                }

                return field;
            }
        }

        public CropCatalog Catalog => catalog;
        public float CellSize => world.Grid.CellSize;

        private void Update()
        {
            DayNightClock clock = dayNight != null ? dayNight.Clock : null;
            if (clock == null)
            {
                Field.Tick(Time.deltaTime);
                return;
            }

            if (clock.IsPaused)
            {
                return;
            }

            Field.Tick(Time.deltaTime * GrowthRules.GetTimeScale(clock.Phase, nightGrowthMultiplier));
        }

        /// <returns>False si el punto cae fuera de la rejilla.</returns>
        public bool TryWorldToCell(Vector3 worldPosition, out Vector2Int cell)
        {
            cell = world.Grid.WorldToCell(worldPosition);
            return Field.Bounds.Contains(cell);
        }

        /// <summary>Centro de la celda, a ras de suelo.</summary>
        public Vector3 CellToWorld(Vector2Int cell)
        {
            return world.Grid.RectCenterToWorld(new RectInt(cell, Vector2Int.one));
        }

        internal void Configure(FarmWorld farmWorld, CropCatalog newCatalog, DayNightCycle cycle)
        {
            world = farmWorld;
            catalog = newCatalog;
            dayNight = cycle;
            field = null;
        }
    }
}
