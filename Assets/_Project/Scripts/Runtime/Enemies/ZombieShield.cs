using DeadAcres.Combat;
using UnityEngine;

namespace DeadAcres.Enemies
{
    /// <summary>
    /// Escudo antidisturbios fusionado al brazo: alterna entre ir por delante (para las balas
    /// que llegan de frente al cuerpo) y a un costado (no para nada). La cabeza nunca la tapa.
    /// </summary>
    public sealed class ZombieShield : MonoBehaviour
    {
        [SerializeField] private Transform shield;
        [SerializeField] private Vector3 frontLocalPosition;
        [SerializeField] private Vector3 sideLocalPosition;
        [SerializeField, Min(0.5f)] private float frontSeconds = 4f;
        [SerializeField, Min(0.5f)] private float sideSeconds = 3f;

        [Tooltip("Mitad del arco, por delante, que cubre el escudo cuando va al frente.")]
        [SerializeField, Range(10f, 90f)] private float blockHalfAngle = 70f;

        private float nextSwitch;

        public bool IsFront { get; private set; } = true;

        private void OnEnable()
        {
            IsFront = true;
            nextSwitch = Time.time + frontSeconds;
            ApplyPose();
        }

        private void Update()
        {
            if (Time.time < nextSwitch)
            {
                return;
            }

            IsFront = !IsFront;
            nextSwitch = Time.time + (IsFront ? frontSeconds : sideSeconds);
            ApplyPose();
        }

        /// <summary>¿El escudo para este disparo del jugador?</summary>
        public bool Blocks(Vector3 shotDirection, HitZone zone)
        {
            return IsFront && zone != HitZone.Head && Covers(transform.forward, shotDirection, blockHalfAngle);
        }

        /// <summary>
        /// ¿Una bala que viaja en <paramref name="shotDirection"/> llega por el arco frontal de
        /// alguien que mira hacia <paramref name="forward"/>? Función pura, en horizontal.
        /// </summary>
        public static bool Covers(Vector3 forward, Vector3 shotDirection, float halfAngleDegrees)
        {
            forward.y = 0f;
            shotDirection.y = 0f;
            if (forward.sqrMagnitude < 0.0001f || shotDirection.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            // La bala viene de frente si viaja en sentido contrario a hacia donde mira.
            return Vector3.Angle(forward, -shotDirection) <= halfAngleDegrees;
        }

        private void ApplyPose()
        {
            if (shield == null)
            {
                return;
            }

            shield.localPosition = IsFront ? frontLocalPosition : sideLocalPosition;
            shield.localRotation = IsFront ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
        }

        internal void Configure(Transform shieldVisual, Vector3 front, Vector3 side)
        {
            shield = shieldVisual;
            frontLocalPosition = front;
            sideLocalPosition = side;
        }
    }
}
