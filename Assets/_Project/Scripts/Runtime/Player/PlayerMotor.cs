using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>Movimiento en primera persona con CharacterController: andar, correr, saltar y gravedad.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler input;
        [SerializeField, Min(0f)] private float walkSpeed = 4.5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 7f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.1f;
        [SerializeField, Min(0f)] private float gravity = 20f;

        private CharacterController controller;
        private float verticalVelocity;

        private void Awake() => controller = GetComponent<CharacterController>();

        private void Update()
        {
            Vector2 move = input != null ? input.Move : Vector2.zero;
            Vector3 planar = transform.right * move.x + transform.forward * move.y;
            float speed = input != null && input.SprintHeld ? sprintSpeed : walkSpeed;

            if (controller.isGrounded)
            {
                // Pequeño empuje hacia abajo para que isGrounded no parpadee en pendientes.
                verticalVelocity = -2f;

                if (input != null && input.JumpPressed)
                {
                    verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
                }
            }

            verticalVelocity -= gravity * Time.deltaTime;

            Vector3 velocity = planar * speed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        internal void Configure(PlayerInputHandler inputHandler) => input = inputHandler;
    }
}
