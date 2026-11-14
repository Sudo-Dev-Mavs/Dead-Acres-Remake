using DeadAcres.GameMode;
using UnityEngine;

namespace DeadAcres.Items
{
    public enum ItemKind : byte
    {
        Tool = 0,
        Weapon = 1,
        Seed = 2,
        Placeable = 3,
        Consumable = 4,
        Produce = 5,
    }

    /// <summary>Datos estáticos de un ítem. Una instancia por tipo de ítem; nunca se modifica en runtime.</summary>
    [CreateAssetMenu(menuName = "Dead Acres/Items/Item Definition", fileName = "Item_")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Tooltip("Identificador estable (p. ej. seed_strawberry). Es lo que se guarda y lo que viajará por red en co-op. No cambiar una vez usado.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        [Tooltip("Color del icono provisional en el HUD mientras no haya sprite.")]
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private ItemKind kind;
        [SerializeField, Min(1)] private int maxStack = 1;

        [Tooltip("Solo herramientas: qué hace la acción primaria.")]
        [SerializeField] private ToolKind tool = ToolKind.None;

        [Header("Tienda de Jack")]
        [SerializeField, Min(0)] private int buyPrice;
        [SerializeField, Min(0)] private int sellPrice;

        [Header("Solo colocables y semillas")]
        [SerializeField] private GameObject placedPrefab;

        [Tooltip("Tamaño en bloques (X, Z) con rotación 0.")]
        [SerializeField] private Vector2Int footprint = Vector2Int.one;

        [SerializeField] private PlaceableCategory placeableCategory = PlaceableCategory.Object;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Color Tint => tint;
        public ToolKind Tool => tool;
        public ItemKind Kind => kind;
        public int MaxStack => maxStack;
        public int BuyPrice => buyPrice;
        public int SellPrice => sellPrice;
        public GameObject PlacedPrefab => placedPrefab;
        public Vector2Int Footprint => footprint;
        public PlaceableCategory PlaceableCategory => placeableCategory;
        public bool IsPlaceable => (kind == ItemKind.Placeable || kind == ItemKind.Seed) && placedPrefab != null;

        internal void Configure(string name, ItemKind itemKind, int stack, Vector2Int size = default,
            PlaceableCategory category = PlaceableCategory.Object)
        {
            displayName = name;
            kind = itemKind;
            maxStack = Mathf.Max(1, stack);
            footprint = size == default ? Vector2Int.one : size;
            placeableCategory = category;
        }

        internal void ConfigurePrices(int buy, int sell)
        {
            buyPrice = Mathf.Max(0, buy);
            sellPrice = Mathf.Max(0, sell);
        }

        internal void ConfigureExtras(string newId, Color newTint, ToolKind newTool = ToolKind.None,
            GameObject prefab = null)
        {
            id = newId;
            tint = newTint;
            tool = newTool;
            placedPrefab = prefab;
        }
    }
}
