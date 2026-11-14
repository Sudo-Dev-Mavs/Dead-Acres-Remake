using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>Cámara en primera persona: el cuerpo gira en horizontal y el pivote de la cámara en vertical.</summary>
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler input;
        [SerializeField] private Transform cameraPivot;

        [Tooltip("Grados por píxel de movimiento del ratón.")]
        [SerializeField, Min(0f)] private float sensitivity = 0.12f;

        [SerializeField, Range(10f, 89f)] private float maxPitch = 85f;

        private float pitch;

        private void Update()
        {
            if (input == null || cameraPivot == null)
            {
                return;
            }

            // El delta del ratón ya es "por frame": no se multiplica por deltaTime.
            Vector2 delta = input.Look * sensitivity;
            if (delta == Vector2.zero)
            {
                return;
            }

            transform.Rotate(0f, delta.x, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - delta.y, -maxPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        internal void Configure(PlayerInputHandler inputHandler, Transform pivot)
        {
            input = inputHandler;
            cameraPivot = pivot;
        }
    }
}
