using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadAcres.Settings
{
    /// <summary>
    /// Las acciones del jugador y sus teclas por defecto, en un solo sitio: las usa el jugador
    /// para jugar y el menú de opciones para enseñarlas y cambiarlas. Los cambios se guardan
    /// en las preferencias como diferencias sobre estas teclas por defecto.
    /// </summary>
    public static class GameInput
    {
        /// <summary>Acción y nombre que ve el jugador, en el orden del menú de controles.</summary>
        public static readonly (string action, string label)[] Rebindable =
        {
            ("Primary", "Usar / disparar"),
            ("Secondary", "Acción secundaria"),
            ("Jump", "Saltar"),
            ("Sprint", "Correr"),
            ("Interact", "Interactuar"),
            ("Reload", "Recargar"),
            ("Ready", "Listo (terminar el día)"),
            ("Inventory", "Inventario"),
            ("RotateLeft", "Rotar a la izquierda"),
            ("RotateRight", "Rotar a la derecha"),
            ("HotbarNext", "Hotbar: siguiente"),
            ("HotbarPrevious", "Hotbar: anterior"),
        };

        /// <summary>Partes del movimiento con teclado, en el orden del menú.</summary>
        public static readonly (string part, string label)[] MoveParts =
        {
            ("up", "Mover: adelante"),
            ("down", "Mover: atrás"),
            ("left", "Mover: izquierda"),
            ("right", "Mover: derecha"),
        };

        private const string PrefKey = "dead_acres.bindings";

        /// <summary>Se guardaron o se restablecieron las teclas: quien tenga un mapa vivo lo recarga.</summary>
        public static event Action BindingsChanged;

        public static InputActionMap Create()
        {
            var map = new InputActionMap("Player");

            InputAction move = map.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            map.AddAction("LookMouse", InputActionType.Value, "<Mouse>/delta");
            map.AddAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick");
            map.AddAction("Scroll", InputActionType.Value, "<Mouse>/scroll/y");

            Button(map, "Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            Button(map, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Button(map, "Primary", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            Button(map, "Secondary", "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
            Button(map, "Interact", "<Keyboard>/f", "<Gamepad>/buttonWest");
            Button(map, "Reload", "<Keyboard>/r", "<Gamepad>/buttonNorth");

            // Start queda para la pausa: "Listo" pasa a la cruceta hacia arriba.
            Button(map, "Ready", "<Keyboard>/enter", "<Gamepad>/dpad/up");
            Button(map, "Inventory", "<Keyboard>/tab", "<Gamepad>/select");
            Button(map, "RotateLeft", "<Keyboard>/q", "<Gamepad>/dpad/left");
            Button(map, "RotateRight", "<Keyboard>/e", "<Gamepad>/dpad/right");
            Button(map, "HotbarNext", "<Keyboard>/period", "<Gamepad>/rightShoulder");
            Button(map, "HotbarPrevious", "<Keyboard>/comma", "<Gamepad>/leftShoulder");

            for (int i = 1; i <= 9; i++)
            {
                map.AddAction($"Slot{i}", InputActionType.Button, $"<Keyboard>/{i}");
            }

            return map;
        }

        /// <summary>Deja el mapa con las teclas por defecto más los cambios guardados.</summary>
        public static void LoadOverrides(InputActionMap map)
        {
            map.RemoveAllBindingOverrides();
            string json = PlayerPrefs.GetString(PrefKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                map.LoadBindingOverridesFromJson(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"No se pudieron cargar las teclas guardadas: {exception.Message}");
                map.RemoveAllBindingOverrides();
            }
        }

        public static void SaveOverrides(InputActionMap map)
        {
            PlayerPrefs.SetString(PrefKey, map.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
            BindingsChanged?.Invoke();
        }

        public static void ResetOverrides()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.Save();
            BindingsChanged?.Invoke();
        }

        /// <summary>Índice de la tecla (teclado/ratón) o del botón (mando) de una acción. -1 si no tiene.</summary>
        public static int FindBinding(InputAction action, bool gamepad)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite)
                {
                    continue;
                }

                bool isGamepad = binding.path != null && binding.path.StartsWith("<Gamepad>");
                if (isGamepad == gamepad)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Índice de una parte ("up", "down"...) del movimiento con teclado. -1 si no existe.</summary>
        public static int FindCompositePart(InputAction action, string part)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].isPartOfComposite
                    && string.Equals(action.bindings[i].name, part, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Nombre legible de lo que hay asignado ("R", "Botón izquierdo"...).</summary>
        public static string GetDisplayName(InputAction action, int bindingIndex)
        {
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                return "-";
            }

            string path = action.bindings[bindingIndex].effectivePath;
            return string.IsNullOrEmpty(path)
                ? "-"
                : InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        private static void Button(InputActionMap map, string name, string keyboardOrMouse, string gamepad)
        {
            InputAction action = map.AddAction(name, InputActionType.Button, keyboardOrMouse);
            action.AddBinding(gamepad);
        }
    }
}
