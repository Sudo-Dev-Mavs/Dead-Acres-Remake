using System;
using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>Conecta el <see cref="Inventory"/> del jugador con el input de la hotbar.</summary>
    [DefaultExecutionOrder(-80)]
    public sealed class PlayerInventory : MonoBehaviour
    {
        [Serializable]
        private struct StartingItem
        {
            public ItemDefinition item;
            [Min(1)] public int count;
        }

        [SerializeField] private PlayerInputReader input;
        [SerializeField, Min(0)] private int backpackSize = Inventory.DefaultBackpackSize;

        [Tooltip("Equipo inicial: azada, martillo, escopeta... (GDD sección 8).")]
        [SerializeField] private StartingItem[] startingItems = Array.Empty<StartingItem>();

        public Inventory Inventory { get; private set; }

        private void Awake()
        {
            Inventory = new Inventory(backpackSize);
            foreach (StartingItem start in startingItems)
            {
                Inventory.Add(start.item, start.count);
            }
        }

        private void OnEnable()
        {
            input.HotbarSlotPressed += Inventory.Select;
            input.HotbarCycled += Inventory.Cycle;
        }

        private void OnDisable()
        {
            input.HotbarSlotPressed -= Inventory.Select;
            input.HotbarCycled -= Inventory.Cycle;
        }
    }
}
