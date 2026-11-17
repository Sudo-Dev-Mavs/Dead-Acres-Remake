using System.Collections.Generic;
using UnityEngine;

namespace DeadAcres.Items
{
    /// <summary>
    /// Todos los ítems del juego, para encontrarlos por su Id estable (al cargar una partida y,
    /// más adelante, al recibirlos por red).
    /// </summary>
    [CreateAssetMenu(menuName = "Dead Acres/Item Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

        private Dictionary<string, ItemDefinition> byId;

        [System.NonSerialized] private Dictionary<string, ItemDefinition> runtimeItems;

        public IReadOnlyList<ItemDefinition> Items => items;

        public bool TryGet(string id, out ItemDefinition item)
        {
            if (string.IsNullOrEmpty(id))
            {
                item = null;
                return false;
            }

            if (runtimeItems != null && runtimeItems.TryGetValue(id, out item))
            {
                return true;
            }

            if (byId == null)
            {
                byId = new Dictionary<string, ItemDefinition>(items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null && !string.IsNullOrEmpty(items[i].Id))
                    {
                        byId[items[i].Id] = items[i];
                    }
                }
            }

            return byId.TryGetValue(id, out item);
        }

        /// <summary>
        /// Añade un ítem solo en memoria (los de los mods): se encuentra por Id como los demás,
        /// pero no se escribe en el asset.
        /// </summary>
        public void RegisterRuntime(ItemDefinition item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id))
            {
                return;
            }

            if (runtimeItems == null) runtimeItems = new Dictionary<string, ItemDefinition>();
            runtimeItems[item.Id] = item;
        }

        public void ClearRuntime() => runtimeItems = null;

        internal void Configure(IEnumerable<ItemDefinition> newItems)
        {
            items = new List<ItemDefinition>(newItems);
            byId = null;
        }
    }
}
