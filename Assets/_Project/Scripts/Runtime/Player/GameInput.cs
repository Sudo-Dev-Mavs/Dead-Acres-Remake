using System;
using UnityEngine.InputSystem;

namespace DeadAcres.Player
{
    /// <summary>
    /// Acciones de juego definidas en código (teclado y ratón). Se usa el Input System nuevo:
    /// el proyecto tiene el Input Manager antiguo desactivado.
    /// </summary>
    public sealed class GameInput : IDisposable
    {
        public const int SlotCount = 10;

        private readonly InputActionMap map;
        private readonly InputAction[] slots = new InputAction[SlotCount];

        public GameInput()
        {
            map = new InputActionMap("Gameplay");

            Move = map.AddAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            Look = map.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            Jump = map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            Sprint = map.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            Primary = map.AddAction("Primary", InputActionType.Button, "<Mouse>/leftButton");
            Interact = map.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            Ready = map.AddAction("Ready", InputActionType.Button, "<Keyboard>/enter");
            ReleaseCursor = map.AddAction("ReleaseCursor", InputActionType.Button, "<Keyboard>/escape");
            Scroll = map.AddAction("HotbarScroll", InputActionType.Value, "<Mouse>/scroll/y");

            for (int i = 0; i < SlotCount; i++)
            {
                // Teclas 1..9 y 0 para la décima casilla.
                int digit = (i + 1) % 10;
                slots[i] = map.AddAction($"Slot{i + 1}", InputActionType.Button, $"<Keyboard>/{digit}");
            }
        }

        public InputAction Move { get; }
        public InputAction Look { get; }
        public InputAction Jump { get; }
        public InputAction Sprint { get; }
        public InputAction Primary { get; }
        public InputAction Interact { get; }

        /// <summary>Botón "Listo": pasar del día al atardecer.</summary>
        public InputAction Ready { get; }

        public InputAction ReleaseCursor { get; }
        public InputAction Scroll { get; }

        public void Enable() => map.Enable();

        public void Disable() => map.Disable();

        /// <returns>Índice 0..9 de la casilla cuya tecla se pulsó este frame, o -1.</returns>
        public int GetPressedSlot()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i].WasPressedThisFrame())
                {
                    return i;
                }
            }

            return -1;
        }

        public void Dispose()
        {
            map.Disable();
            map.Dispose();
        }
    }
}
