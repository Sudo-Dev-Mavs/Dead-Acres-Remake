using System;
using System.Collections.Generic;
using DeadAcres.DayNight;
using DeadAcres.Items;
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

        [Tooltip("Cuántos objetos distintos trae como mucho. Los primeros días trae menos (ver ShopStock.GetSize).")]
        [SerializeField, Min(1)] private int stockSize = ShopStock.DefaultSize;

        [Tooltip("Semillas que nunca faltan en el surtido del día.")]
        [SerializeField, Min(0)] private int guaranteedSeeds = 2;

        private readonly List<ItemDefinition> pool = new List<ItemDefinition>();
        private readonly List<ItemDefinition> stock = new List<ItemDefinition>();
        private int stockDay = -1;
        private int stockSeed;

        public ShopCatalog Catalog => catalog;

        private void Awake() => stockSeed = UnityEngine.Random.Range(1, int.MaxValue);

        /// <summary>
        /// Lo que Jack trae ese día: un surtido al azar del catálogo, el mismo durante todo el día.
        /// </summary>
        public IReadOnlyList<ItemDefinition> GetStock(int dayNumber)
        {
            if (dayNumber != stockDay && catalog != null)
            {
                stockDay = dayNumber;
                pool.Clear();
                catalog.GetAvailable(dayNumber, pool);
                int size = ShopStock.GetSize(dayNumber, stockSeed, stockSize);
                ShopStock.Pick(pool, dayNumber, stockSeed, size, guaranteedSeeds, stock);
            }

            return stock;
        }
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
