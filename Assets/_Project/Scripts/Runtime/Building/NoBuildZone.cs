using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>
    /// Reserva en la rejilla las celdas que ocupa este objeto más un margen alrededor: ahí no se
    /// puede arar, plantar ni colocar nada. Lo lleva la caravana.
    /// </summary>
    public sealed class NoBuildZone : MonoBehaviour
    {
        [SerializeField] private FarmWorld world;

        [Tooltip("Celdas libres obligatorias alrededor del objeto.")]
        [SerializeField, Min(0)] private int marginCells = 3;

        private int occupantId = PlacementGrid.NoOccupant;

        // En Start: la rejilla la crea FarmWorld en su Awake.
        private void Start()
        {
            if (world == null || world.Grid == null || !TryGetComponent(out Collider zoneCollider))
            {
                Debug.LogWarning("NoBuildZone sin FarmWorld o sin collider: no reserva nada.", this);
                return;
            }

            Bounds bounds = zoneCollider.bounds;
            occupantId = world.Grid.Reserve(world.Grid.CellsCovering(bounds.min, bounds.max, marginCells));
        }

        private void OnDestroy()
        {
            if (occupantId != PlacementGrid.NoOccupant && world != null && world.Grid != null)
            {
                world.Grid.Release(occupantId);
            }
        }

        internal void Configure(FarmWorld farmWorld, int margin)
        {
            world = farmWorld;
            marginCells = Mathf.Max(0, margin);
        }
    }
}
