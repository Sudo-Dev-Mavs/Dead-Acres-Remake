using System;
using System.Collections.Generic;
using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>
    /// Inventario de un jugador y su casilla de hotbar seleccionada. La hotbar son las
    /// primeras <see cref="HotbarSize"/> casillas del inventario.
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        public const int HotbarSize = 10;

        [Serializable]
        private struct StartingItem
        {
            public ItemDefinition item;
            public int count;
        }

        [SerializeField, Min(HotbarSize)] private int slotCount = 30;
        [SerializeField] private List<StartingItem> startingItems = new List<StartingItem>();

        private Inventory inventory;
        private int selectedSlot;

        public event Action<int> SelectedSlotChanged;

        public Inventory Inventory
        {
            get
            {
                EnsureCreated();
                return inventory;
            }
        }

        public int SelectedSlot => selectedSlot;

        public ItemStack SelectedStack => Inventory.GetSlot(selectedSlot);

        private void Awake() => EnsureCreated();

        public void SelectSlot(int index)
        {
            int wrapped = ((index % HotbarSize) + HotbarSize) % HotbarSize;
            if (wrapped == selectedSlot)
            {
                return;
            }

            selectedSlot = wrapped;
            SelectedSlotChanged?.Invoke(selectedSlot);
        }

        public void CycleSlot(int direction) => SelectSlot(selectedSlot + direction);

        // Creación perezosa: otros componentes pueden pedir el inventario en su OnEnable,
        // antes de que se ejecute este Awake.
        private void EnsureCreated()
        {
            if (inventory != null)
            {
                return;
            }

            inventory = new Inventory(Mathf.Max(HotbarSize, slotCount));
            for (int i = 0; i < startingItems.Count; i++)
            {
                inventory.Add(startingItems[i].item, startingItems[i].count);
            }
        }

        internal void ConfigureLoadout(int slots, IEnumerable<KeyValuePair<ItemDefinition, int>> items)
        {
            slotCount = Mathf.Max(HotbarSize, slots);
            startingItems = new List<StartingItem>();
            foreach (KeyValuePair<ItemDefinition, int> pair in items)
            {
                startingItems.Add(new StartingItem { item = pair.Key, count = pair.Value });
            }

            inventory = null;
        }
    }
}
