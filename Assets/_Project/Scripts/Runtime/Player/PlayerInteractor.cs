using System;
using DeadAcres.DayNight;
using DeadAcres.Farming;
using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>
    /// Convierte el input del jugador local en intenciones: arar, plantar, cosechar, hablar con
    /// lo que tenga delante (<see cref="IInteractable"/>) y dar el día por terminado. No modifica estado por su cuenta: llama a <see cref="FarmField"/>
    /// y <see cref="FarmCommands"/>, que son quienes validan. En co-op esas llamadas pasarán a ser
    /// peticiones al host y este componente no cambia.
    /// Colocar objetos es cosa de PlacementController; aquí solo se usan la azada y las semillas.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerInventory playerInventory;

        [Tooltip("Origen y dirección del rayo de interacción (la cámara).")]
        [SerializeField] private Transform eye;

        [SerializeField] private FarmArea farm;

        [Tooltip("Opcional. Para el botón \"Listo\".")]
        [SerializeField] private DayNightCycle dayNight;

        [SerializeField, Min(0.5f)] private float reach = 6f;
        [SerializeField] private LayerMask targetMask = Physics.DefaultRaycastLayers;

        private InteractionContext context;
        private bool cursorWasLocked;
        private Collider lastHitCollider;
        private IInteractable lastHitInteractable;

        /// <summary>Cambió lo que mira el jugador o lo que podría hacer ahí.</summary>
        public event Action<InteractionContext> ContextChanged;

        /// <summary>Una acción de granja fue rechazada; el HUD decide si avisa.</summary>
        public event Action<FarmActionResult> ActionRejected;

        public event Action<ItemDefinition, int> Harvested;

        public InteractionContext Context => context;
        public FarmArea Farm => farm;

        private Inventory Inventory => playerInventory.Inventory;

        private void OnEnable()
        {
            input.PrimaryPressed += UsePrimary;
            input.InteractPressed += Interact;
            input.ReadyPressed += SkipDay;
        }

        private void OnDisable()
        {
            input.PrimaryPressed -= UsePrimary;
            input.InteractPressed -= Interact;
            input.ReadyPressed -= SkipDay;
        }

        // Lo que se mira cambia de forma continua con la cámara: se recalcula cada frame,
        // pero el evento solo sale cuando el resultado cambia.
        private void Update()
        {
            UpdateContext();
            cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
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
            if (eye == null
                || !Physics.Raycast(new Ray(eye.position, eye.forward), out RaycastHit hit, reach, targetMask,
                    QueryTriggerInteraction.Collide))
            {
                return default;
            }

            IInteractable interactable = GetInteractable(hit.collider);
            if (interactable != null && interactable.CanInteract)
            {
                return new InteractionContext(interactable);
            }

            // Se empuja el punto un poco hacia dentro de lo que se golpeó: al apuntar al costado
            // de una planta, la celda elegida es la de la planta y no la vecina.
            if (farm == null || !farm.TryWorldToCell(hit.point - hit.normal * 0.02f, out Vector2Int cell))
            {
                return default;
            }

            bool hasPlot = farm.Field.TryGetPlot(cell, out PlotState plot);
            ItemStack selected = Inventory.Selected;
            PrimaryActionKind primary = PrimaryActionKind.None;
            CropDefinition cropToPlant = null;

            if (!selected.IsEmpty)
            {
                if (selected.Item.Tool == ToolKind.Hoe)
                {
                    if (farm.Field.CanTill(cell) == FarmActionResult.Ok)
                    {
                        primary = PrimaryActionKind.Till;
                    }
                }
                else if (TryGetCropFor(selected.Item, out CropDefinition crop)
                         && farm.Field.CanPlant(cell, crop) == FarmActionResult.Ok)
                {
                    primary = PrimaryActionKind.Plant;
                    cropToPlant = crop;
                }
            }

            return new InteractionContext(true, cell, primary, cropToPlant, hasPlot, plot.Stage, plot.Crop);
        }

        // Solo se busca el componente cuando cambia el collider que se mira.
        private IInteractable GetInteractable(Collider hitCollider)
        {
            if (hitCollider != lastHitCollider)
            {
                lastHitCollider = hitCollider;
                lastHitInteractable = hitCollider.GetComponentInParent<IInteractable>();
            }

            return lastHitInteractable;
        }

        private bool TryGetCropFor(ItemDefinition item, out CropDefinition crop)
        {
            crop = null;
            return item.Kind == ItemKind.Seed
                && farm.Catalog != null
                && farm.Catalog.TryGetBySeed(item, out crop);
        }

        private void UsePrimary()
        {
            // El clic que recupera el cursor (tras Esc) no debe usar la herramienta.
            if (!cursorWasLocked || farm == null)
            {
                return;
            }

            // Los eventos de input llegan antes que Update: se refresca lo que se mira.
            UpdateContext();
            if (!context.HasTarget)
            {
                return;
            }

            ItemStack selected = Inventory.Selected;
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
                result = FarmCommands.PlantFromSlot(farm.Field, context.Cell, crop, Inventory, Inventory.SelectedIndex);
            }
            else
            {
                return;
            }

            Report(result);
        }

        private void Interact()
        {
            if (!cursorWasLocked)
            {
                return;
            }

            UpdateContext();

            if (context.Interactable != null)
            {
                context.Interactable.Interact(gameObject);
                return;
            }

            if (farm == null || !context.HasTarget)
            {
                return;
            }

            FarmActionResult result = FarmCommands.HarvestInto(farm.Field, context.Cell, Inventory,
                out ItemDefinition produce, out int amount);

            if (result == FarmActionResult.Ok)
            {
                Harvested?.Invoke(produce, amount);
            }

            Report(result);
        }

        private void SkipDay()
        {
            if (dayNight != null)
            {
                dayNight.TrySkipDay();
            }
        }

        private void Report(FarmActionResult result)
        {
            if (result != FarmActionResult.Ok)
            {
                ActionRejected?.Invoke(result);
            }

            // La acción puede haber cambiado la parcela o la ranura: se recalcula ya.
            UpdateContext();
        }

        internal void Configure(PlayerInputReader inputReader, PlayerInventory inventory, Transform eyeTransform,
            FarmArea farmArea, DayNightCycle cycle)
        {
            input = inputReader;
            playerInventory = inventory;
            eye = eyeTransform;
            farm = farmArea;
            dayNight = cycle;
        }
    }
}
