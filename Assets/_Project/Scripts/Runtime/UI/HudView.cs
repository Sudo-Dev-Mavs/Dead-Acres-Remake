using System;
using System.Collections.Generic;
using DeadAcres.Building;
using DeadAcres.DayNight;
using DeadAcres.Farming;
using DeadAcres.GameMode;
using DeadAcres.Items;
using DeadAcres.Player;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAcres.UI
{
    /// <summary>
    /// HUD provisional del jugador local: reloj, fase, contadores de plantas y objetos, hotbar, ayuda contextual
    /// y avisos. Solo escucha eventos y pinta; no modifica estado de juego.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [Serializable]
        internal sealed class SlotWidgets
        {
            public Image frame;
            public Image icon;
            public Text count;
        }

        [Header("Fuentes de datos")]
        [SerializeField] private DayNightCycle dayNight;
        [SerializeField] private PhaseEventChannel phaseChanged;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private FarmWorld world;

        [Header("Widgets")]
        [SerializeField] private Text clockLabel;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Text plantsLabel;
        [SerializeField] private Text selectedItemLabel;
        [SerializeField] private Text hintLabel;
        [SerializeField] private Text noticeLabel;
        [SerializeField] private List<SlotWidgets> slots = new List<SlotWidgets>();

        [Header("Estilo")]
        [SerializeField] private Color slotColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Color selectedSlotColor = new Color(1f, 0.82f, 0.25f, 0.9f);
        [SerializeField, Min(0.5f)] private float noticeSeconds = 2.5f;

        private int lastClockStamp = -1;
        private float noticeTimeLeft;
        private PlacementLimiter limiter;

        private void OnEnable()
        {
            if (phaseChanged != null) phaseChanged.Subscribe(OnPhaseChanged);

            if (playerInventory != null)
            {
                playerInventory.Inventory.SlotChanged += OnSlotChanged;
                playerInventory.Inventory.SelectionChanged += OnSelectionChanged;
            }

            if (interactor != null)
            {
                interactor.ContextChanged += OnContextChanged;
                interactor.ActionRejected += OnActionRejected;
                interactor.Harvested += OnHarvested;
            }

            limiter = world != null ? world.Limiter : null;
            if (limiter != null) limiter.CountChanged += OnPlacementCountChanged;
        }

        private void OnDisable()
        {
            if (phaseChanged != null) phaseChanged.Unsubscribe(OnPhaseChanged);

            if (playerInventory != null)
            {
                playerInventory.Inventory.SlotChanged -= OnSlotChanged;
                playerInventory.Inventory.SelectionChanged -= OnSelectionChanged;
            }

            if (interactor != null)
            {
                interactor.ContextChanged -= OnContextChanged;
                interactor.ActionRejected -= OnActionRejected;
                interactor.Harvested -= OnHarvested;
            }

            if (limiter != null) limiter.CountChanged -= OnPlacementCountChanged;
            limiter = null;
        }

        private void Start()
        {
            SetText(hintLabel, string.Empty);
            SetText(noticeLabel, string.Empty);

            // El reloj suele arrancar antes que este Start (y ya emitió su primer cambio de fase),
            // así que la etiqueta se inicializa leyendo su estado en vez de borrarla.
            DayNightClock clock = dayNight != null ? dayNight.Clock : null;
            SetText(phaseLabel, clock != null && clock.IsRunning ? GetPhaseName(clock.Phase) : string.Empty);

            RefreshAllSlots();
            RefreshPlantsLabel();
        }

        // El reloj avanza de forma continua y no hay evento "cambió el minuto": se consulta cada
        // frame, pero el texto solo se regenera cuando cambia el minuto mostrado.
        private void Update()
        {
            DayNightClock clock = dayNight != null ? dayNight.Clock : null;
            if (clock != null && clockLabel != null)
            {
                float hour = DayNightTime.GetHour(clock.Phase, clock.PhaseProgress);
                DayNightTime.ToHoursMinutes(hour, out int hours, out int minutes);
                int stamp = clock.DayNumber * 10000 + hours * 100 + minutes;
                if (stamp != lastClockStamp)
                {
                    lastClockStamp = stamp;
                    clockLabel.text = $"Día {clock.DayNumber}   {hours:00}:{minutes:00}";
                }
            }

            if (noticeTimeLeft > 0f)
            {
                noticeTimeLeft -= Time.unscaledDeltaTime;
                if (noticeTimeLeft <= 0f)
                {
                    SetText(noticeLabel, string.Empty);
                }
            }
        }

        private void OnPhaseChanged(PhaseChange change) => SetText(phaseLabel, GetPhaseName(change.Phase));

        private void OnSlotChanged(int index)
        {
            RefreshSlot(index);
            if (index == playerInventory.Inventory.SelectedIndex)
            {
                RefreshSelectedLabel();
            }
        }

        private void OnSelectionChanged(int index)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].frame != null)
                {
                    slots[i].frame.color = i == index ? selectedSlotColor : slotColor;
                }
            }

            RefreshSelectedLabel();
        }

        private void OnContextChanged(InteractionContext context) => SetText(hintLabel, BuildHint(context));

        private void OnActionRejected(FarmActionResult result)
        {
            string message = GetRejectionMessage(result);
            if (message != null)
            {
                ShowNotice(message);
            }
        }

        private void OnHarvested(ItemDefinition item, int amount) => ShowNotice($"+{amount} {item.DisplayName}");

        private void OnPlacementCountChanged(PlaceableCategory category, int count, int limit)
        {
            RefreshPlantsLabel();
        }

        private void RefreshAllSlots()
        {
            if (playerInventory == null)
            {
                return;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                RefreshSlot(i);
            }

            OnSelectionChanged(playerInventory.Inventory.SelectedIndex);
        }

        private void RefreshSlot(int index)
        {
            if (index < 0 || index >= slots.Count || index >= playerInventory.Inventory.SlotCount)
            {
                return;
            }

            ItemStack stack = playerInventory.Inventory[index];
            SlotWidgets widgets = slots[index];

            if (widgets.icon != null)
            {
                widgets.icon.enabled = !stack.IsEmpty;
                if (!stack.IsEmpty)
                {
                    widgets.icon.sprite = stack.Item.Icon;
                    widgets.icon.color = stack.Item.Icon != null ? Color.white : stack.Item.Tint;
                }
            }

            SetText(widgets.count, stack.Count > 1 ? stack.Count.ToString() : string.Empty);
        }

        private void RefreshSelectedLabel()
        {
            ItemStack stack = playerInventory.Inventory.Selected;
            SetText(selectedItemLabel, stack.IsEmpty ? string.Empty : stack.Item.DisplayName);
        }

        private void RefreshPlantsLabel()
        {
            if (limiter == null)
            {
                SetText(plantsLabel, string.Empty);
                return;
            }

            SetText(plantsLabel,
                $"Plantas {limiter.GetCount(PlaceableCategory.Plant)}/{limiter.GetLimit(PlaceableCategory.Plant)}" +
                $"   Objetos {limiter.GetCount(PlaceableCategory.Object)}/{limiter.GetLimit(PlaceableCategory.Object)}");
        }

        private void ShowNotice(string message)
        {
            SetText(noticeLabel, message);
            noticeTimeLeft = noticeSeconds;
        }

        private static string BuildHint(InteractionContext context)
        {
            if (!context.HasTarget)
            {
                return string.Empty;
            }

            if (context.CanHarvest)
            {
                return $"[F] Cosechar {context.PlotCrop.DisplayName}";
            }

            switch (context.Primary)
            {
                case PrimaryActionKind.Till:
                    return "[Clic] Arar";
                case PrimaryActionKind.Plant:
                    return $"[Clic] Plantar {context.CropToPlant.DisplayName}";
            }

            if (context.HasPlot && context.PlotCrop != null)
            {
                return $"{context.PlotCrop.DisplayName}: creciendo";
            }

            return string.Empty;
        }

        private static string GetPhaseName(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Dawn: return "Amanecer";
                case GamePhase.Day: return "Día  ·  [R] Listo";
                case GamePhase.Dusk: return "Atardecer";
                default: return "Noche";
            }
        }

        /// <returns>Null para los rechazos que no merecen aviso (clics repetidos, mirar al vacío).</returns>
        private static string GetRejectionMessage(FarmActionResult result)
        {
            switch (result)
            {
                case FarmActionResult.NotTilled: return "Primero hay que arar la tierra";
                case FarmActionResult.Occupied: return "Ya hay algo plantado aquí";
                case FarmActionResult.NotRipe: return "Todavía no está maduro";
                case FarmActionResult.PlantLimitReached: return "Límite de plantas alcanzado";
                case FarmActionResult.InventoryFull: return "Inventario lleno";
                case FarmActionResult.Blocked: return "Hay un objeto en esa celda";
                default: return null;
            }
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }

        internal void Configure(DayNightCycle cycle, PhaseEventChannel phaseChannel, PlayerInventory inventory,
            PlayerInteractor playerInteractor, FarmWorld farmWorld, Text clock, Text phase, Text plants,
            Text selectedItem, Text hint, Text notice, List<SlotWidgets> slotWidgets)
        {
            dayNight = cycle;
            phaseChanged = phaseChannel;
            playerInventory = inventory;
            interactor = playerInteractor;
            world = farmWorld;
            clockLabel = clock;
            phaseLabel = phase;
            plantsLabel = plants;
            selectedItemLabel = selectedItem;
            hintLabel = hint;
            noticeLabel = notice;
            slots = slotWidgets;
        }
    }
}
