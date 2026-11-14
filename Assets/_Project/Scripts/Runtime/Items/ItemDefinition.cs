using UnityEngine;

namespace DeadAcres.Items
{
    /// <summary>
    /// Datos de un tipo de ítem. El <see cref="Id"/> es estable: es lo que se guarda en disco
    /// y lo que viajará por red en co-op (nunca la referencia al asset).
    /// </summary>
    [CreateAssetMenu(menuName = "Dead Acres/Items/Item Definition", fileName = "Item_")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Tooltip("Identificador estable, en minúsculas y sin espacios (p. ej. seed_strawberry). No cambiar una vez usado.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private ItemCategory category = ItemCategory.Misc;
        [SerializeField] private ToolKind tool = ToolKind.None;
        [SerializeField, Min(1)] private int maxStack = 99;

        [Tooltip("Color del icono provisional en el HUD hasta que haya arte.")]
        [SerializeField] private Color color = Color.white;

        public string Id => id;
        public string DisplayName => displayName;
        public ItemCategory Category => category;
        public ToolKind Tool => tool;
        public int MaxStack => maxStack;
        public Color Color => color;

        internal void Configure(string newId, string name, ItemCategory newCategory, int newMaxStack,
            Color newColor, ToolKind newTool = ToolKind.None)
        {
            id = newId;
            displayName = name;
            category = newCategory;
            tool = newTool;
            maxStack = Mathf.Max(1, newMaxStack);
            color = newColor;
        }
    }
}
