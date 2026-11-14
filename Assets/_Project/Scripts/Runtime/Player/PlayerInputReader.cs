using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadAcres.Player
{
    /// <summary>
    /// Única puerta de entrada del input del jugador. Traduce acciones del Input System
    /// a eventos C# (botones) y valores (ejes). Los demás componentes no tocan el Input System.
    /// </summary>
    /// <remarks>
    /// Las acciones se crean en código para no depender todavía de un .inputactions;
    /// el rebinding funciona igual sobre <see cref="Actions"/>. Teclas por defecto:
    /// WASD, ratón, Espacio saltar, Shift correr, clic izq. usar/colocar, clic der. secundario,
    /// Q/E rotar (como el original), F interactuar (Jack), 1-9 y rueda para la hotbar.
    /// </remarks>
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private InputAction move;
        private InputAction lookMouse;
        private InputAction lookStick;
        private InputAction sprint;
        private InputAction scroll;
        private readonly InputAction[] hotbarSlots = new InputAction[9];

        public event Action JumpPressed;
        public event Action PrimaryPressed;
        public event Action SecondaryPressed;
        public event Action InteractPressed;
        public event Action ReadyPressed;

        /// <summary>-1 = Q (antihorario), +1 = E (horario).</summary>
        public event Action<int> RotatePressed;

        /// <summary>Índice 0-8 de la tecla 1-9 pulsada.</summary>
        public event Action<int> HotbarSlotPressed;

        /// <summary>+1 / -1 por cada paso de rueda o bumper del mando.</summary>
        public event Action<int> HotbarCycled;

        public InputActionMap Actions { get; private set; }

        public Vector2 Move => move.ReadValue<Vector2>();

        /// <summary>Delta del ratón en píxeles de este frame (no escalar por deltaTime).</summary>
        public Vector2 LookMouseDelta => lookMouse.ReadValue<Vector2>();

        /// <summary>Stick derecho en -1..1 (sí escalar por deltaTime).</summary>
        public Vector2 LookStick => lookStick.ReadValue<Vector2>();

        public bool IsSprinting => sprint.IsPressed();

        private void Awake()
        {
            Actions = new InputActionMap("Player");

            move = Actions.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            lookMouse = Actions.AddAction("LookMouse", InputActionType.Value, "<Mouse>/delta");
            lookStick = Actions.AddAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick");
            sprint = Actions.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            sprint.AddBinding("<Gamepad>/leftStickPress");
            scroll = Actions.AddAction("Scroll", InputActionType.Value, "<Mouse>/scroll/y");

            Button("Jump", () => JumpPressed?.Invoke(), "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Button("Primary", () => PrimaryPressed?.Invoke(), "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            Button("Secondary", () => SecondaryPressed?.Invoke(), "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
            Button("Interact", () => InteractPressed?.Invoke(), "<Keyboard>/f", "<Gamepad>/buttonWest");
            Button("Ready", () => ReadyPressed?.Invoke(), "<Keyboard>/r", "<Gamepad>/start");
            Button("RotateLeft", () => RotatePressed?.Invoke(-1), "<Keyboard>/q", "<Gamepad>/dpad/left");
            Button("RotateRight", () => RotatePressed?.Invoke(1), "<Keyboard>/e", "<Gamepad>/dpad/right");
            Button("HotbarNext", () => HotbarCycled?.Invoke(1), "<Gamepad>/rightShoulder", null);
            Button("HotbarPrevious", () => HotbarCycled?.Invoke(-1), "<Gamepad>/leftShoulder", null);

            for (int i = 0; i < hotbarSlots.Length; i++)
            {
                int slot = i;
                hotbarSlots[i] = Button($"Slot{i + 1}", () => HotbarSlotPressed?.Invoke(slot), $"<Keyboard>/{i + 1}", null);
            }

            scroll.performed += OnScroll;
        }

        private void OnEnable() => Actions.Enable();

        private void OnDisable() => Actions.Disable();

        private void OnDestroy()
        {
            Actions.Dispose();
        }

        private void OnScroll(InputAction.CallbackContext context)
        {
            float value = context.ReadValue<float>();
            if (Mathf.Abs(value) < 0.01f) return;

            // Rueda hacia abajo = siguiente ranura, como en la mayoría de juegos con hotbar.
            HotbarCycled?.Invoke(value < 0f ? 1 : -1);
        }

        private InputAction Button(string name, Action onPressed, string keyboardOrMouse, string gamepad)
        {
            InputAction action = Actions.AddAction(name, InputActionType.Button, keyboardOrMouse);
            if (gamepad != null) action.AddBinding(gamepad);

            // La lambda se crea una sola vez aquí, no por pulsación.
            action.performed += _ => onPressed();
            return action;
        }
    }
}
