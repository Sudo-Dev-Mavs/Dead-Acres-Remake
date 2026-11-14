namespace DeadAcres.Items
{
    public enum ItemCategory : byte
    {
        Misc = 0,
        Tool = 1,
        Seed = 2,
        Produce = 3,
        Placeable = 4,
        Consumable = 5,
        Weapon = 6,
    }

    /// <summary>Qué hace un ítem de categoría Tool al usarlo con la acción primaria.</summary>
    public enum ToolKind : byte
    {
        None = 0,

        /// <summary>Azada: ara una celda de la granja.</summary>
        Hoe = 1,
    }
}
