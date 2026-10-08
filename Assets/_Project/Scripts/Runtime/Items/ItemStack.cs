namespace DeadAcres.Items
{
    /// <summary>Contenido de una ranura. Struct: el inventario es un array plano sin alocaciones por ranura.</summary>
    public readonly struct ItemStack
    {
        public static readonly ItemStack Empty = default;

        public readonly ItemDefinition Item;
        public readonly int Count;

        public ItemStack(ItemDefinition item, int count)
        {
            Item = count > 0 ? item : null;
            Count = item != null && count > 0 ? count : 0;
        }

        public bool IsEmpty => Item == null || Count <= 0;

        public int SpaceLeft => IsEmpty ? 0 : Item.MaxStack - Count;

        public ItemStack WithCount(int count) => new ItemStack(Item, count);
    }
}
