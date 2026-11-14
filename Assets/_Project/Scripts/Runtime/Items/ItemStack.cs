namespace DeadAcres.Items
{
    /// <summary>Una pila de ítems en una casilla. Struct inmutable: no genera basura al pasarla.</summary>
    public readonly struct ItemStack
    {
        public static readonly ItemStack Empty = default;

        public readonly ItemDefinition Item;
        public readonly int Count;

        public ItemStack(ItemDefinition item, int count)
        {
            bool valid = item != null && count > 0;
            Item = valid ? item : null;
            Count = valid ? count : 0;
        }

        public bool IsEmpty => Count <= 0;
    }
}
