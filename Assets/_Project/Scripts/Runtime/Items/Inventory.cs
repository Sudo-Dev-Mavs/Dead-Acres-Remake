using System;

namespace DeadAcres.Items
{
    /// <summary>
    /// Inventario de casillas en C# puro (sin MonoBehaviour): testeable y, en co-op,
    /// modificado solo por el host. Las vistas escuchan <see cref="SlotChanged"/>.
    /// </summary>
    public sealed class Inventory
    {
        private readonly ItemStack[] slots;

        public Inventory(int slotCount)
        {
            if (slotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount));
            }

            slots = new ItemStack[slotCount];
        }

        /// <summary>Índice de la casilla que cambió.</summary>
        public event Action<int> SlotChanged;

        public int SlotCount => slots.Length;

        public ItemStack GetSlot(int index) => slots[index];

        public int CountOf(ItemDefinition item)
        {
            if (item == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Item == item)
                {
                    total += slots[i].Count;
                }
            }

            return total;
        }

        /// <summary>Cuántas unidades de este ítem caben todavía (pilas a medias + casillas vacías).</summary>
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
                    space += Math.Max(0, item.MaxStack - slots[i].Count);
                }
            }

            return space;
        }

        public bool CanAdd(ItemDefinition item, int count) => count > 0 && GetFreeSpaceFor(item) >= count;

        /// <summary>
        /// Añade lo que quepa: primero rellena pilas existentes y después usa casillas vacías.
        /// </summary>
        /// <returns>Unidades que no cupieron (0 si entró todo).</returns>
        public int Add(ItemDefinition item, int count)
        {
            if (item == null || count <= 0)
            {
                return Math.Max(0, count);
            }

            int remaining = count;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].Item == item && slots[i].Count < item.MaxStack)
                {
                    int moved = Math.Min(remaining, item.MaxStack - slots[i].Count);
                    SetSlot(i, new ItemStack(item, slots[i].Count + moved));
                    remaining -= moved;
                }
            }

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].IsEmpty)
                {
                    int moved = Math.Min(remaining, item.MaxStack);
                    SetSlot(i, new ItemStack(item, moved));
                    remaining -= moved;
                }
            }

            return remaining;
        }

        /// <summary>Añade todo o nada.</summary>
        public bool TryAdd(ItemDefinition item, int count)
        {
            if (!CanAdd(item, count))
            {
                return false;
            }

            Add(item, count);
            return true;
        }

        /// <summary>Quita unidades de una casilla concreta. Todo o nada.</summary>
        public bool TryRemoveAt(int index, int count)
        {
            if (index < 0 || index >= slots.Length || count <= 0 || slots[index].Count < count)
            {
                return false;
            }

            ItemStack stack = slots[index];
            SetSlot(index, new ItemStack(stack.Item, stack.Count - count));
            return true;
        }

        /// <summary>Quita unidades del ítem de donde las haya, empezando por el final. Todo o nada.</summary>
        public bool TryRemove(ItemDefinition item, int count)
        {
            if (count <= 0 || CountOf(item) < count)
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
                SetSlot(i, new ItemStack(item, slots[i].Count - taken));
                remaining -= taken;
            }

            return true;
        }

        /// <summary>
        /// Arrastrar una casilla sobre otra: si tienen el mismo ítem se apilan (lo que no quepa
        /// se queda en el origen); si no, se intercambian.
        /// </summary>
        public bool Move(int from, int to)
        {
            if (from < 0 || from >= slots.Length || to < 0 || to >= slots.Length || from == to || slots[from].IsEmpty)
            {
                return false;
            }

            ItemStack source = slots[from];
            ItemStack target = slots[to];

            if (!target.IsEmpty && target.Item == source.Item)
            {
                int moved = Math.Min(source.Count, source.Item.MaxStack - target.Count);
                if (moved <= 0)
                {
                    return false;
                }

                SetSlot(to, new ItemStack(source.Item, target.Count + moved));
                SetSlot(from, new ItemStack(source.Item, source.Count - moved));
                return true;
            }

            SetSlot(to, source);
            SetSlot(from, target);
            return true;
        }

        private void SetSlot(int index, ItemStack stack)
        {
            slots[index] = stack;
            SlotChanged?.Invoke(index);
        }
    }
}
