using System;
using DeadAcres.DayNight;
using DeadAcres.Farming;
using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>
    /// Convierte el input del jugador local en intenciones: cambiar de casilla, arar, plantar,
    /// cosechar, dar el día por terminado. No modifica estado por su cuenta: llama a
    /// <see cref="FarmField"/> y <see cref="FarmCommands"/>, que son quienes validan. En co-op,
    /// esas llamadas pasarán a ser peticiones al host y este componente no cambia.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler input;
        [SerializeField] private PlayerInventory inventory;

        [Tooltip("Origen y dirección del rayo de interacción (la cámara).")]
        [SerializeField] private Transform eye;

        [SerializeField] private FarmArea farm;

        [Tooltip("Opcional. Para el botón \"Listo\" (Enter).")]
        [SerializeField] private DayNightCycle dayNight;

        [SerializeField, Min(0.5f)] private float reach = 4.5f;
        [SerializeField] private LayerMask targetMask = Physics.DefaultRaycastLayers;

        private InteractionContext context;

        /// <summary>Cambió lo que mira el jugador o lo que podría hacer ahí.</summary>
        public event Action<InteractionContext> ContextChanged;

        /// <summary>Una acción de granja fue rechazada; el HUD decide si avisa.</summary>
        public event Action<FarmActionResult> ActionRejected;

        public event Action<ItemDefinition, int> Harvested;

        public InteractionContext Context => context;
        public FarmArea Farm => farm;

        private void Update()
        {
            if (input == null || inventory == null)
            {
                return;
            }

            if (input.SlotPressed >= 0)
            {
                inventory.SelectSlot(input.SlotPressed);
            }
            else if (input.ScrollStep != 0)
            {
                // Rueda hacia abajo = siguiente casilla.
                inventory.CycleSlot(-input.ScrollStep);
            }

            UpdateContext();

            if (input.PrimaryPressed)
            {
                UsePrimary();
            }

            if (input.InteractPressed)
            {
                Harvest();
            }

            if (input.ReadyPressed && dayNight != null)
            {
                dayNight.TrySkipDay();
            }
        }

        private void UpdateContext()
        {
            InteractionContext current = BuildContext();
            if (current.Equals(context))
            {
                return;
            }

            context = current;
            ContextChanged?.Invoke(context);
        }

        private InteractionContext BuildContext()
        {
            if (farm == null || !TryFindTargetCell(out Vector2Int cell))
            {
                return default;
            }

            bool hasPlot = farm.Field.TryGetPlot(cell, out PlotState plot);
            ItemStack selected = inventory.SelectedStack;
            PrimaryActionKind primary = PrimaryActionKind.None;
            CropDefinition cropToPlant = null;

            if (!selected.IsEmpty)
            {
                if (selected.Item.Tool == ToolKind.Hoe)
                {
                    if (!hasPlot)
                    {
                        primary = PrimaryActionKind.Till;
                    }
                }
                else if (hasPlot && plot.Crop == null && TryGetCropFor(selected.Item, out CropDefinition crop))
                {
                    primary = PrimaryActionKind.Plant;
                    cropToPlant = crop;
                }
            }

            return new InteractionContext(true, cell, primary, cropToPlant, hasPlot, plot.Stage, plot.Crop);
        }

        private bool TryFindTargetCell(out Vector2Int cell)
        {
            cell = default;
            if (eye == null)
            {
                return false;
            }

            var ray = new Ray(eye.position, eye.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, reach, targetMask, QueryTriggerInteraction.Collide))
            {
                return false;
            }

            // Se empuja el punto un poco hacia dentro de lo que se golpeó: al apuntar al costado
            // de una planta, la celda elegida es la de la planta y no la vecina.
            return farm.TryWorldToCell(hit.point - hit.normal * 0.02f, out cell);
        }

        private bool TryGetCropFor(ItemDefinition item, out CropDefinition crop)
        {
            crop = null;
            return item.Category == ItemCategory.Seed
                && farm.Catalog != null
                && farm.Catalog.TryGetBySeed(item, out crop);
        }

        private void UsePrimary()
        {
            if (!context.HasTarget)
            {
                return;
            }

            ItemStack selected = inventory.SelectedStack;
            if (selected.IsEmpty)
            {
                return;
            }

            FarmActionResult result;
            if (selected.Item.Tool == ToolKind.Hoe)
            {
                result = farm.Field.Till(context.Cell);
            }
            else if (TryGetCropFor(selected.Item, out CropDefinition crop))
            {
                result = FarmCommands.PlantFromSlot(farm.Field, context.Cell, crop, inventory.Inventory,
                    inventory.SelectedSlot);
            }
            else
            {
                return;
            }

            Report(result);
        }

        private void Harvest()
        {
            if (!context.HasTarget)
            {
                return;
            }

            FarmActionResult result = FarmCommands.HarvestInto(farm.Field, context.Cell, inventory.Inventory,
                out ItemDefinition produce, out int amount);

            if (result == FarmActionResult.Ok)
            {
                Harvested?.Invoke(produce, amount);
            }

            Report(result);
        }

        private void Report(FarmActionResult result)
        {
            if (result != FarmActionResult.Ok)
            {
                ActionRejected?.Invoke(result);
            }

            // La acción puede haber cambiado la parcela o la casilla: se recalcula en el mismo frame.
            UpdateContext();
        }

        internal void Configure(PlayerInputHandler inputHandler, PlayerInventory playerInventory, Transform eyeTransform,
            FarmArea farmArea, DayNightCycle cycle)
        {
            input = inputHandler;
            inventory = playerInventory;
            eye = eyeTransform;
            farm = farmArea;
            dayNight = cycle;
        }
    }
}
