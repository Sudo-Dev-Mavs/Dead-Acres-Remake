using System;
using DeadAcres.Building;
using DeadAcres.DayNight;
using DeadAcres.Farming;
using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>
    /// Convierte el input del jugador local en intenciones: arar (azada), plantar (semillas) y
    /// cosechar (hoz dentada) con la acción primaria, hablar con lo que tenga delante
    /// (<see cref="IInteractable"/>) con Interactuar, y dar el día por terminado. No modifica estado por su cuenta: llama a <see cref="FarmField"/>
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
        private readonly RaycastHit[] rayHits = new RaycastHit[16];
        private bool cursorWasLocked;
        private Collider lastHitCollider;
        private IInteractable lastHitInteractable;

        /// <summary>Cambió lo que mira el jugador o lo que podría hacer ahí.</summary>
        public event Action<InteractionContext> ContextChanged;

        /// <summary>Una acción de granja fue rechazada; el HUD decide si avisa.</summary>
        public event Action<FarmActionResult> ActionRejected;

        public event Action<ItemDefinition, int> Harvested;

        /// <summary>Se aró, se plantó o se cosechó con éxito.</summary>
        public event Action<PrimaryActionKind> ActionPerformed;

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
            if (eye == null)
            {
                return default;
            }

            var ray = new Ray(eye.position, eye.forward);
            Vector3 aimPoint;
            if (TryRaycastIgnoringSelf(ray, out RaycastHit hit))
            {
                IInteractable interactable = GetInteractable(hit.collider);
                if (interactable != null && interactable.CanInteract)
                {
                    return new InteractionContext(interactable);
                }

                // Se empuja el punto un poco hacia dentro de lo que se golpeó: al apuntar al costado
                // de una planta, la celda elegida es la de la planta y no la vecina.
                aimPoint = hit.point - hit.normal * 0.02f;
            }
            else if (farm == null || !GroundAim.TryGetPoint(ray, farm.GroundHeight, reach, out aimPoint))
            {
                // Mirando más lejos que el alcance, la celda es la del borde del alcance.
                return default;
            }

            // Con un arma no se trabaja la tierra, y lo colocable ya enseña su propio fantasma:
            // en los dos casos sobra el recuadro de la celda.
            ItemStack held = Inventory.Selected;
            if (!held.IsEmpty && (held.Item.Kind == ItemKind.Weapon || held.Item.IsPlaceable))
            {
                return default;
            }

            if (farm == null || !farm.TryWorldToCell(aimPoint, out Vector2Int cell))
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
                else if (selected.Item.Tool == ToolKind.Sickle)
                {
                    if (farm.Field.CanHarvest(cell) == FarmActionResult.Ok)
                    {
                        primary = PrimaryActionKind.Harvest;
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

        // Lo más cercano que toca el rayo sin contar los colliders del propio jugador: al moverse,
        // el rayo puede salir desde fuera de su cápsula y chocar con ella.
        private bool TryRaycastIgnoringSelf(Ray ray, out RaycastHit nearest)
        {
            nearest = default;
            int count = Physics.RaycastNonAlloc(ray, rayHits, reach, targetMask, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (rayHits[i].distance < best && !rayHits[i].collider.transform.IsChildOf(transform))
                {
                    best = rayHits[i].distance;
                    nearest = rayHits[i];
                }
            }

            return best < float.MaxValue;
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
            else if (selected.Item.Tool == ToolKind.Sickle)
            {
                result = FarmCommands.HarvestInto(farm.Field, context.Cell, Inventory,
                    out ItemDefinition produce, out int amount);
                if (result == FarmActionResult.Ok)
                {
                    Harvested?.Invoke(produce, amount);
                }
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

                // El mismo objeto puede ofrecer ahora otra cosa (o nada): se recalcula y se avisa
                // aunque siga siendo el mismo interactuable.
                context = BuildContext();
                ContextChanged?.Invoke(context);
            }
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
            else
            {
                // context sigue siendo el de antes de la acción: dice qué se acaba de hacer.
                ActionPerformed?.Invoke(context.Primary);
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
