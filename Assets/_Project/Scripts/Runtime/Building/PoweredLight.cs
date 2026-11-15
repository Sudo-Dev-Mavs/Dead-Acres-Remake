using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>Luz que solo se enciende con energía (la luz portátil).</summary>
    public sealed class PoweredLight : MonoBehaviour
    {
        [SerializeField] private PowerConsumer power;
        [SerializeField] private Light target;

        private void Awake()
        {
            if (target != null) target.enabled = false;
        }

        private void OnEnable()
        {
            if (power != null) power.PoweredChanged += Apply;
        }

        private void OnDisable()
        {
            if (power != null) power.PoweredChanged -= Apply;
        }

        private void Apply(bool powered)
        {
            if (target != null) target.enabled = powered;
        }

        internal void Configure(PowerConsumer consumer, Light light)
        {
            power = consumer;
            target = light;
        }
    }
}
