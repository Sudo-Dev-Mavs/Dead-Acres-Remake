using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>Algo del mundo con lo que el jugador interactúa mirándolo y pulsando Interactuar (F).</summary>
    public interface IInteractable
    {
        /// <summary>Texto de ayuda del HUD, sin la tecla (p. ej. "Comerciar con Jack").</summary>
        string Prompt { get; }

        bool CanInteract { get; }

        /// <param name="player">GameObject raíz del jugador que interactúa.</param>
        void Interact(GameObject player);
    }
}
