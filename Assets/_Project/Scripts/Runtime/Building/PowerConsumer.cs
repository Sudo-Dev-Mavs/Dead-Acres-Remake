using System;
using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>
    /// Aparato que necesita energía (torreta, luz portátil). Como en el original, solo funciona
    /// cerca de una fuente. Avisa cuando cambia; no consulta cada frame.
    /// </summary>
    public sealed class PowerConsumer : MonoBehaviour
    {
        [Tooltip("Vacío en objetos colocados: se toma del PlacedObject.")]
        [SerializeField] private FarmWorld world;

        [Tooltip("Cuánta energía gasta. Si a ninguna fuente cercana le queda tanta, no funciona.")]
        [SerializeField, Min(0f)] private float demand = 1f;

        private PowerMap map;
        private int consumerId;

        public float Demand => demand;

        /// <summary>Empieza en false; pasa a true (con evento) si hay una fuente en rango.</summary>
        public bool IsPowered { get; private set; }

        public event Action<bool> PoweredChanged;

        private void Start()
        {
            if (world == null && TryGetComponent(out PlacedObject placed))
            {
                world = placed.World;
            }

            map = world != null ? world.Power : null;
            if (map == null)
            {
                return;
            }

            map.Changed += Refresh;
            consumerId = map.AddConsumer(transform.position, demand);
            Refresh();
        }

        private void OnDestroy()
        {
            if (map != null)
            {
                map.Changed -= Refresh;
                map.RemoveConsumer(consumerId);
            }
        }

        private void Refresh()
        {
            bool powered = map.IsConsumerPowered(consumerId);
            if (powered == IsPowered)
            {
                return;
            }

            IsPowered = powered;
            PoweredChanged?.Invoke(powered);
        }

        internal void ConfigureDemand(float newDemand) => demand = Mathf.Max(0f, newDemand);
    }
}
