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

        private PowerMap map;

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
            Refresh();
        }

        private void OnDestroy()
        {
            if (map != null)
            {
                map.Changed -= Refresh;
            }
        }

        private void Refresh()
        {
            bool powered = map.IsPowered(transform.position);
            if (powered == IsPowered)
            {
                return;
            }

            IsPowered = powered;
            PoweredChanged?.Invoke(powered);
        }
    }
}
