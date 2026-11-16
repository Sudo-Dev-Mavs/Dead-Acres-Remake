using System;
using UnityEngine;
using DeadAcres.Settings;
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
        private InputAction primary;
        private readonly InputAction[] hotbarSlots = new InputAction[9];

        public event Action JumpPressed;
        public event Action PrimaryPressed;
        public event Action SecondaryPressed;
        public event Action InteractPressed;
        public event Action ReadyPressed;

        public event Action ReloadPressed;
        public event Action InventoryPressed;

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

        /// <summary>La acción primaria sigue pulsada (armas automáticas).</summary>
        public bool IsPrimaryHeld => primary != null && primary.IsPressed();

        private void Awake()
        {
            // Las acciones y sus teclas por defecto viven en GameInput: el menú de opciones usa
            // las mismas para enseñarlas y cambiarlas.
            Actions = GameInput.Create();
            GameInput.LoadOverrides(Actions);

            move = Actions.FindAction("Move");
            lookMouse = Actions.FindAction("LookMouse");
            lookStick = Actions.FindAction("LookStick");
            sprint = Actions.FindAction("Sprint");
            scroll = Actions.FindAction("Scroll");
            primary = Actions.FindAction("Primary");

            Hook("Jump", () => JumpPressed?.Invoke());
            Hook("Primary", () => PrimaryPressed?.Invoke());
            Hook("Secondary", () => SecondaryPressed?.Invoke());
            Hook("Interact", () => InteractPressed?.Invoke());
            Hook("Reload", () => ReloadPressed?.Invoke());
            Hook("Ready", () => ReadyPressed?.Invoke());
            Hook("Inventory", () => InventoryPressed?.Invoke());
            Hook("RotateLeft", () => RotatePressed?.Invoke(-1));
            Hook("RotateRight", () => RotatePressed?.Invoke(1));
            Hook("HotbarNext", () => HotbarCycled?.Invoke(1));
            Hook("HotbarPrevious", () => HotbarCycled?.Invoke(-1));

            for (int i = 0; i < hotbarSlots.Length; i++)
            {
                int slot = i;
                hotbarSlots[i] = Hook($"Slot{i + 1}", () => HotbarSlotPressed?.Invoke(slot));
            }

            scroll.performed += OnScroll;
            GameInput.BindingsChanged += ReloadBindings;
        }

        // El jugador cambió teclas en las opciones (con este componente apagado, en pausa).
        private void ReloadBindings() => GameInput.LoadOverrides(Actions);

        /// <summary>Nombre legible de la tecla de una acción ("Enter", "R"...), para el HUD.</summary>
        public string GetKeyName(string actionName)
        {
            InputAction action = Actions != null ? Actions.FindAction(actionName) : null;
            return action != null ? GameInput.GetDisplayName(action, GameInput.FindBinding(action, false)) : "-";
        }

        private void OnEnable() => Actions.Enable();

        private void OnDisable() => Actions.Disable();

        private void OnDestroy()
        {
            GameInput.BindingsChanged -= ReloadBindings;
            Actions.Dispose();
        }

        private void OnScroll(InputAction.CallbackContext context)
        {
            float value = context.ReadValue<float>();
            if (Mathf.Abs(value) < 0.01f) return;

            // Rueda hacia abajo = siguiente ranura, como en la mayoría de juegos con hotbar.
            HotbarCycled?.Invoke(value < 0f ? 1 : -1);
        }

        private InputAction Hook(string actionName, Action onPressed)
        {
            InputAction action = Actions.FindAction(actionName, true);

            // La lambda se crea una sola vez aquí, no por pulsación.
            action.performed += _ => onPressed();
            return action;
        }
    }
}
