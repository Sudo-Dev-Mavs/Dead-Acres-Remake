using System;
using DeadAcres.DayNight;
using DeadAcres.Player;
using UnityEngine;

namespace DeadAcres.Economy
{
    /// <summary>
    /// Jack en la escena. Como en el original, está de día (amanecer, día y atardecer) y se va
    /// por la noche. Al interactuar con él pide abrir la tienda; la interfaz la pone ShopView.
    /// </summary>
    public sealed class Shopkeeper : MonoBehaviour, IInteractable
    {
        [SerializeField] private ShopCatalog catalog;
        [SerializeField] private PhaseEventChannel phaseChanged;

        [Tooltip("Lo que se oculta de noche (modelo y collider de Jack).")]
        [SerializeField] private GameObject visualRoot;

        [SerializeField] private string displayName = "Jack";

        /// <summary>(tendero, jugador que quiere comerciar).</summary>
        public event Action<Shopkeeper, GameObject> ShopRequested;

        public ShopCatalog Catalog => catalog;
        public bool IsPresent { get; private set; } = true;

        public string Prompt => $"Comerciar con {displayName}";
        public bool CanInteract => IsPresent;

        private void OnEnable()
        {
            if (phaseChanged != null) phaseChanged.Subscribe(OnPhaseChanged);
        }

        private void OnDisable()
        {
            if (phaseChanged != null) phaseChanged.Unsubscribe(OnPhaseChanged);
        }

        public void Interact(GameObject player)
        {
            if (IsPresent)
            {
                ShopRequested?.Invoke(this, player);
            }
        }

        private void OnPhaseChanged(PhaseChange change)
        {
            IsPresent = change.Phase != GamePhase.Night;
            if (visualRoot != null)
            {
                visualRoot.SetActive(IsPresent);
            }
        }

        internal void Configure(ShopCatalog shopCatalog, PhaseEventChannel phaseChannel, GameObject visual)
        {
            catalog = shopCatalog;
            phaseChanged = phaseChannel;
            visualRoot = visual;
        }
    }
}
