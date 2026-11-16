using DeadAcres.DayNight;
using DeadAcres.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace DeadAcres.UI
{
    /// <summary>
    /// Lo que comparten las ventanas que interrumpen el juego (tienda, inventario): sueltan el
    /// cursor, apagan el input de juego y, con un solo jugador, detienen el reloj. En co-op el
    /// reloj no podrá pararse.
    /// </summary>
    public static class UiModal
    {
        public static void Enter(PlayerInputReader playerInput, DayNightCycle dayNight)
        {
            EnsureUiInput();
            if (playerInput != null) playerInput.enabled = false;
            if (dayNight != null) dayNight.SetPaused(true);
            FirstPersonController.SetCursorLocked(false);
        }

        public static void Exit(PlayerInputReader playerInput, DayNightCycle dayNight)
        {
            if (dayNight != null) dayNight.SetPaused(false);
            if (playerInput != null) playerInput.enabled = true;
            FirstPersonController.SetCursorLocked(true);
        }

        /// <summary>
        /// Sin acciones asignadas, el módulo de UI del Input System no recibe clics. Lo llaman
        /// las ventanas al abrirse y el menú principal al arrancar.
        /// </summary>
        public static void EnsureUiInput()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError("Las ventanas necesitan un EventSystem en la escena para recibir clics.");
                return;
            }

            var module = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (module != null && module.actionsAsset == null)
            {
                module.AssignDefaultActions();
            }
        }
    }
}
