using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>Da energía a los aparatos dentro de su radio (la caravana, un generador).</summary>
    public sealed class PowerSource : MonoBehaviour
    {
        [Tooltip("Vacío en objetos colocados: se toma del PlacedObject.")]
        [SerializeField] private FarmWorld world;
        [SerializeField, Min(0.5f)] private float radius = 9f;

        [Tooltip("Mitad del tamaño de la fuente en X y Z: el radio se cuenta desde ese borde. Cero: desde el centro.")]
        [SerializeField] private Vector2 halfExtents;

        private int sourceId;

        public float Radius => radius;

        // En Start y no en Awake: PlacedObject.Bind se llama justo después de instanciar.
        private void Start()
        {
            if (world == null && TryGetComponent(out PlacedObject placed))
            {
                world = placed.World;
            }

            if (world == null || world.Power == null)
            {
                Debug.LogWarning("PowerSource sin FarmWorld: no da energía.", this);
                return;
            }

            sourceId = world.Power.Add(transform.position, radius, halfExtents);
        }

        private void OnDestroy()
        {
            if (sourceId != 0 && world != null && world.Power != null)
            {
                world.Power.Remove(sourceId);
            }
        }

        internal void Configure(FarmWorld farmWorld, float newRadius, Vector2 newHalfExtents = default)
        {
            world = farmWorld;
            radius = Mathf.Max(0.5f, newRadius);
            halfExtents = newHalfExtents;
        }
    }
}
