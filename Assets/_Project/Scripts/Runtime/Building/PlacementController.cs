using DeadAcres.Items;
using DeadAcres.Player;
using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>
    /// Coloca el ítem seleccionado de la hotbar en la rejilla: fantasma verde/rojo,
    /// Q/E para rotar, clic para colocar. Respeta rejilla, alcance y límites del modo.
    /// </summary>
    public sealed class PlacementController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private FarmWorld world;
        [SerializeField] private Camera viewCamera;

        [Tooltip("Capas donde se puede apuntar para colocar (terreno).")]
        [SerializeField] private LayerMask groundMask = ~0;

        [Tooltip("Alcance en metros. En el original la azada llegaba absurdamente lejos.")]
        [SerializeField, Min(1f)] private float maxReach = 6f;

        [SerializeField] private Material ghostValidMaterial;
        [SerializeField] private Material ghostInvalidMaterial;

        private PlacementGhost ghost;
        private ItemDefinition ghostItem;
        private int quarterTurns;
        private RectInt targetRect;
        private bool hasTarget;
        private bool targetValid;

        private Inventory Inventory => playerInventory.Inventory;

        private void Awake()
        {
            ghost = new PlacementGhost(ghostValidMaterial, ghostInvalidMaterial);
        }

        private void OnEnable()
        {
            input.PrimaryPressed += TryPlace;
            input.RotatePressed += Rotate;
            Inventory.SelectionChanged += OnSelectionChanged;
            Inventory.SlotChanged += OnSlotChanged;
            RefreshGhostItem();
        }

        private void OnDisable()
        {
            input.PrimaryPressed -= TryPlace;
            input.RotatePressed -= Rotate;
            Inventory.SelectionChanged -= OnSelectionChanged;
            Inventory.SlotChanged -= OnSlotChanged;
            ghost.Hide();
        }

        private void OnDestroy() => ghost?.Dispose();

        private void Update()
        {
            if (ghostItem == null)
            {
                return;
            }

            hasTarget = TryGetTarget(out targetRect);
            if (!hasTarget)
            {
                ghost.Hide();
                return;
            }

            targetValid = world.Grid.IsFree(targetRect) && world.Limiter.CanPlace(ghostItem.PlaceableCategory);
            ghost.Show(world.Grid.RectCenterToWorld(targetRect), CurrentRotation, targetValid);
        }

        private Quaternion CurrentRotation => Quaternion.Euler(0f, 90f * quarterTurns, 0f);

        private bool TryGetTarget(out RectInt rect)
        {
            rect = default;
            Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out RaycastHit hit, maxReach, groundMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            Vector2Int size = PlacementGrid.RotateFootprint(ghostItem.Footprint, quarterTurns);
            rect = PlacementGrid.RectAround(world.Grid.WorldToCell(hit.point), size);
            return world.Grid.IsInside(rect);
        }

        private void TryPlace()
        {
            if (ghostItem == null || !hasTarget || !targetValid)
            {
                return;
            }

            if (world.TryPlace(ghostItem, targetRect, CurrentRotation) == null)
            {
                return;
            }

            // Quitar del inventario al final: puede vaciar la ranura y disparar RefreshGhostItem.
            Inventory.RemoveAt(Inventory.SelectedIndex, 1);
        }

        internal void Configure(PlayerInputReader inputReader, PlayerInventory inventory, FarmWorld farmWorld,
            Camera camera, Material validMaterial, Material invalidMaterial)
        {
            input = inputReader;
            playerInventory = inventory;
            world = farmWorld;
            viewCamera = camera;
            ghostValidMaterial = validMaterial;
            ghostInvalidMaterial = invalidMaterial;
        }

        private void Rotate(int direction)
        {
            if (ghostItem == null) return;
            quarterTurns = (quarterTurns + direction + 4) % 4;
        }

        private void OnSelectionChanged(int _) => RefreshGhostItem();

        private void OnSlotChanged(int index)
        {
            if (index == Inventory.SelectedIndex) RefreshGhostItem();
        }

        private void RefreshGhostItem()
        {
            ItemStack selected = Inventory.Selected;
            ItemDefinition next = !selected.IsEmpty && selected.Item.IsPlaceable ? selected.Item : null;
            if (next == ghostItem) return;

            ghostItem = next;
            hasTarget = false;
            if (ghostItem != null)
            {
                ghost.Build(ghostItem.PlacedPrefab);
            }
            else
            {
                ghost.Hide();
            }
        }
    }
}
