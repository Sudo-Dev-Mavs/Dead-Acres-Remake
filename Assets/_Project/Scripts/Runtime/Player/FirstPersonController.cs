using DeadAcres.Settings;
using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>Movimiento y cámara en primera persona sobre CharacterController.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        [Tooltip("Hijo que rota en pitch (lleva la cámara). El root rota solo en yaw.")]
        [SerializeField] private Transform cameraPivot;

        [Header("Movimiento")]
        [SerializeField, Min(0f)] private float walkSpeed = 4.5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 7f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -20f;

        [Header("Cámara")]
        [SerializeField, Min(0.001f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(1f)] private float stickDegreesPerSecond = 180f;
        [SerializeField, Range(10f, 89f)] private float maxPitch = 85f;
        [SerializeField] private bool invertY;

        private CharacterController controller;
        private float pitch;
        private float verticalVelocity;
        private bool jumpRequested;

        public float MouseSensitivity
        {
            get => mouseSensitivity;
            set => mouseSensitivity = Mathf.Max(0.001f, value);
        }

        public bool InvertY
        {
            get => invertY;
            set => invertY = value;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            input.JumpPressed += OnJumpPressed;
            input.PrimaryPressed += OnPrimaryPressed;
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            input.JumpPressed -= OnJumpPressed;
            input.PrimaryPressed -= OnPrimaryPressed;
            SetCursorLocked(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Look(dt);
            Move(dt);
        }

        /// <summary>Al abrir tienda o inventario: libera el ratón y deja de girar la cámara.</summary>
        private const float StepLength = 2.2f;
        private float stepDistance;
        private Camera viewCamera;

        /// <summary>El jugador dio un paso (para el sonido).</summary>
        public event System.Action Stepped;

        private void Start()
        {
            viewCamera = cameraPivot != null ? cameraPivot.GetComponentInChildren<Camera>() : null;
            ApplySettings();
            GameSettings.Changed += ApplySettings;
        }

        private void OnDestroy() => GameSettings.Changed -= ApplySettings;

        private void ApplySettings()
        {
            if (viewCamera != null) viewCamera.fieldOfView = GameSettings.FieldOfView;
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void Look(float dt)
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Vector2 delta = input.LookMouseDelta * (mouseSensitivity * GameSettings.MouseSensitivity)
                            + input.LookStick * (stickDegreesPerSecond * GameSettings.StickSensitivity * dt);
            float ySign = invertY || GameSettings.InvertY ? 1f : -1f;

            pitch = Mathf.Clamp(pitch + delta.y * ySign, -maxPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            transform.Rotate(0f, delta.x, 0f, Space.Self);
        }

        private void Move(float dt)
        {
            Vector2 axis = Vector2.ClampMagnitude(input.Move, 1f);
            float speed = input.IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 horizontal = (transform.right * axis.x + transform.forward * axis.y) * speed;

            if (controller.isGrounded)
            {
                // Pequeña fuerza hacia abajo para que isGrounded no parpadee en pendientes.
                verticalVelocity = -2f;

                if (jumpRequested)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }

            jumpRequested = false;
            verticalVelocity += gravity * dt;

            controller.Move((horizontal + Vector3.up * verticalVelocity) * dt);

            // Un paso cada cierta distancia andada con los pies en el suelo.
            if (controller.isGrounded)
            {
                stepDistance += new Vector2(horizontal.x, horizontal.z).magnitude * dt;
                if (stepDistance >= StepLength)
                {
                    stepDistance = 0f;
                    Stepped?.Invoke();
                }
            }
        }

        private void OnJumpPressed() => jumpRequested = true;

        // Tras soltar el cursor (Esc en el editor), un clic en el juego lo vuelve a capturar.
        private void OnPrimaryPressed()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorLocked(true);
            }
        }

        internal void Configure(PlayerInputReader inputReader, Transform pivot)
        {
            input = inputReader;
            cameraPivot = pivot;
        }
    }
}
