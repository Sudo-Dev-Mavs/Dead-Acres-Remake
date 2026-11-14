using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>
    /// Lee el input del jugador local una vez por frame y lo expone como valores simples.
    /// El resto de componentes del jugador no tocan el Input System: en co-op, los jugadores
    /// remotos simplemente no tendrán este componente activo.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class PlayerInputHandler : MonoBehaviour
    {
        private GameInput input;

        public Vector2 Move { get; private set; }

        /// <summary>Movimiento del ratón en píxeles desde el frame anterior.</summary>
        public Vector2 Look { get; private set; }

        public bool SprintHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool PrimaryPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool ReadyPressed { get; private set; }

        /// <summary>Casilla 0..9 pulsada este frame, o -1.</summary>
        public int SlotPressed { get; private set; } = -1;

        /// <summary>-1, 0 o 1 según la rueda del ratón.</summary>
        public int ScrollStep { get; private set; }

        /// <summary>Con el cursor libre (Esc) el juego ignora el input hasta el siguiente clic.</summary>
        public bool CursorLocked { get; private set; }

        private void OnEnable()
        {
            input = new GameInput();
            input.Enable();
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            SetCursorLocked(false);
            input.Dispose();
            input = null;
            Clear();
        }

        private void Update()
        {
            bool relockClick = false;

            if (input.ReleaseCursor.WasPressedThisFrame())
            {
                SetCursorLocked(false);
            }
            else if (!CursorLocked && input.Primary.WasPressedThisFrame())
            {
                // Ese clic solo recupera el control; no debe usar la herramienta.
                SetCursorLocked(true);
                relockClick = true;
            }

            if (!CursorLocked || relockClick)
            {
                Clear();
                return;
            }

            Move = Vector2.ClampMagnitude(input.Move.ReadValue<Vector2>(), 1f);
            Look = input.Look.ReadValue<Vector2>();
            SprintHeld = input.Sprint.IsPressed();
            JumpPressed = input.Jump.WasPressedThisFrame();
            PrimaryPressed = input.Primary.WasPressedThisFrame();
            InteractPressed = input.Interact.WasPressedThisFrame();
            ReadyPressed = input.Ready.WasPressedThisFrame();
            SlotPressed = input.GetPressedSlot();

            float scroll = input.Scroll.ReadValue<float>();
            ScrollStep = scroll > 0f ? 1 : scroll < 0f ? -1 : 0;
        }

        private void Clear()
        {
            Move = Vector2.zero;
            Look = Vector2.zero;
            SprintHeld = false;
            JumpPressed = false;
            PrimaryPressed = false;
            InteractPressed = false;
            ReadyPressed = false;
            SlotPressed = -1;
            ScrollStep = 0;
        }

        private void SetCursorLocked(bool locked)
        {
            CursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
