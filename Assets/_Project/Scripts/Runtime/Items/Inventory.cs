using System;

namespace DeadAcres.Items
{
    /// <summary>
    /// Hotbar de 9 ranuras (teclas 1-9) + mochila. Lo que se recibe va primero a la hotbar,
    /// y a la mochila cuando la hotbar está llena (GDD sección 6).
    /// C# puro: en co-op cada jugador tiene el suyo y el host valida las operaciones.
    /// </summary>
    public sealed class Inventory
    {
        public const int HotbarSize = 9;
        public const int DefaultBackpackSize = 24;

        private readonly ItemStack[] slots;

        public Inventory(int backpackSize = DefaultBackpackSize)
        {
            slots = new ItemStack[HotbarSize + Math.Max(0, backpackSize)];
        }

        /// <summary>Índice de la ranura que cambió.</summary>
        public event Action<int> SlotChanged;

        /// <summary>Nuevo índice seleccionado de la hotbar (0-8).</summary>
        public event Action<int> SelectionChanged;

        public int SlotCount => slots.Length;
        public int SelectedIndex { get; private set; }
        public ItemStack Selected => slots[SelectedIndex];

        public ItemStack this[int index] => slots[index];

        public static bool IsHotbar(int index) => index >= 0 && index < HotbarSize;

        public void Select(int hotbarIndex)
        {
            if (!IsHotbar(hotbarIndex) || hotbarIndex == SelectedIndex)
            {
                return;
            }

            SelectedIndex = hotbarIndex;
            SelectionChanged?.Invoke(SelectedIndex);
        }

        /// <summary>Rueda del ratón: +1 / -1, con vuelta circular.</summary>
        public void Cycle(int direction)
        {
            if (direction == 0) return;
            int next = (SelectedIndex + Math.Sign(direction) + HotbarSize) % HotbarSize;
            Select(next);
        }

        /// <summary>Añade ítems. Primero completa pilas existentes, luego huecos de la hotbar, luego la mochila.</summary>
        /// <returns>Cantidad que no cupo (0 = todo dentro).</returns>
        public int Add(ItemDefinition item, int count)
        {
            if (item == null || count <= 0)
            {
                return 0;
            }

            int remaining = count;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].Item == item && slots[i].SpaceLeft > 0)
                {
                    remaining = FillSlot(i, item, remaining);
                }
            }

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].IsEmpty)
                {
                    remaining = FillSlot(i, item, remaining);
                }
            }

            return remaining;
        }

        /// <summary>Cuántas unidades de este ítem caben todavía (pilas a medias + ranuras vacías).</summary>
        public int GetFreeSpaceFor(ItemDefinition item)
        {
            if (item == null)
            {
                return 0;
            }

            int space = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty)
                {
                    space += item.MaxStack;
                }
                else if (slots[i].Item == item)
                {
                    space += slots[i].SpaceLeft;
                }
            }

            return space;
        }

        public bool CanAdd(ItemDefinition item, int count) => count > 0 && GetFreeSpaceFor(item) >= count;

        /// <returns>True si había suficiente y se quitó.</returns>
        public bool RemoveAt(int index, int count = 1)
        {
            if (index < 0 || index >= slots.Length || count <= 0 || slots[index].Count < count)
            {
                return false;
            }

            slots[index] = slots[index].WithCount(slots[index].Count - count);
            SlotChanged?.Invoke(index);
            return true;
        }

        /// <summary>Quita unidades del ítem de donde las haya, empezando por el final (la mochila). Todo o nada.</summary>
        public bool Remove(ItemDefinition item, int count)
        {
            if (item == null || count <= 0 || CountOf(item) < count)
            {
                return false;
            }

            int remaining = count;
            for (int i = slots.Length - 1; i >= 0 && remaining > 0; i--)
            {
                if (slots[i].Item != item)
                {
                    continue;
                }

                int taken = Math.Min(remaining, slots[i].Count);
                slots[i] = slots[i].WithCount(slots[i].Count - taken);
                SlotChanged?.Invoke(i);
                remaining -= taken;
            }

            return true;
        }

        public int CountOf(ItemDefinition item)
        {
            int total = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Item == item) total += slots[i].Count;
            }

            return total;
        }

        /// <summary>Arrastrar en la UI: intercambia ranuras, o junta pilas del mismo ítem.</summary>
        public void Move(int from, int to)
        {
            if (from == to || from < 0 || to < 0 || from >= slots.Length || to >= slots.Length || slots[from].IsEmpty)
            {
                return;
            }

            ItemStack source = slots[from];
            ItemStack target = slots[to];

            if (target.Item == source.Item && target.SpaceLeft > 0)
            {
                int moved = Math.Min(target.SpaceLeft, source.Count);
                slots[to] = target.WithCount(target.Count + moved);
                slots[from] = source.WithCount(source.Count - moved);
            }
            else
            {
                slots[to] = source;
                slots[from] = target;
            }

            SlotChanged?.Invoke(from);
            SlotChanged?.Invoke(to);
        }

        private int FillSlot(int index, ItemDefinition item, int remaining)
        {
            int current = slots[index].IsEmpty ? 0 : slots[index].Count;
            int added = Math.Min(item.MaxStack - current, remaining);
            if (added <= 0) return remaining;

            slots[index] = new ItemStack(item, current + added);
            SlotChanged?.Invoke(index);
            return remaining - added;
        }
    }
}
