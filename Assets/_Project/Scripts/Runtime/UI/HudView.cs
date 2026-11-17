using System;
using System.Collections.Generic;
using DeadAcres.Building;
using DeadAcres.Combat;
using DeadAcres.DayNight;
using DeadAcres.Economy;
using DeadAcres.Enemies;
using DeadAcres.Farming;
using DeadAcres.GameMode;
using DeadAcres.Items;
using DeadAcres.Match;
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
        [SerializeField] private PlayerWallet playerWallet;
        [SerializeField] private Health playerHealth;
        [SerializeField] private Health rvHealth;
        [SerializeField] private MatchDirector match;
        [SerializeField] private WaveDirector waves;
        [SerializeField] private PlayerBuildTools buildTools;

        [Header("Widgets")]
        [SerializeField] private Text clockLabel;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Text plantsLabel;
        [SerializeField] private Text goldLabel;
        [SerializeField] private Text healthLabel;
        [SerializeField] private Text rvLabel;
        [SerializeField] private Text bannerLabel;
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
        private PowerMap powerMap;
        private PlayerWeapon weapon;
        private Wallet wallet;

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
            weapon = interactor != null ? interactor.GetComponent<PlayerWeapon>() : null;
            if (weapon != null)
            {
                weapon.AmmoChanged += RefreshSelectedLabel;
                weapon.Headshot += OnHeadshot;
            }
            powerMap = world != null ? world.Power : null;
            if (powerMap != null) powerMap.Changed += RefreshPlantsLabel;

            wallet = playerWallet != null ? playerWallet.Wallet : null;
            if (wallet != null) wallet.Changed += OnGoldChanged;

            if (playerHealth != null) playerHealth.HealthChanged += OnPlayerHealthChanged;
            if (rvHealth != null) rvHealth.HealthChanged += OnRvHealthChanged;
            if (match != null) match.Ended += OnMatchEnded;
            if (waves != null) waves.ReinforcementsIncoming += OnReinforcementsIncoming;
            if (buildTools != null) buildTools.Message += ShowNotice;
            if (waves != null) waves.StrongIncoming += OnStrongIncoming;
            if (playerInventory != null) playerInventory.Notice += ShowNotice;
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
            if (weapon != null)
            {
                weapon.AmmoChanged -= RefreshSelectedLabel;
                weapon.Headshot -= OnHeadshot;
            }
            weapon = null;
            if (powerMap != null) powerMap.Changed -= RefreshPlantsLabel;
            powerMap = null;

            if (wallet != null) wallet.Changed -= OnGoldChanged;
            wallet = null;

            if (playerHealth != null) playerHealth.HealthChanged -= OnPlayerHealthChanged;
            if (rvHealth != null) rvHealth.HealthChanged -= OnRvHealthChanged;
            if (match != null) match.Ended -= OnMatchEnded;
            if (waves != null) waves.ReinforcementsIncoming -= OnReinforcementsIncoming;
            if (buildTools != null) buildTools.Message -= ShowNotice;
            if (waves != null) waves.StrongIncoming -= OnStrongIncoming;
            if (playerInventory != null) playerInventory.Notice -= ShowNotice;
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
            OnGoldChanged(wallet != null ? wallet.Gold : 0);
            SetText(bannerLabel, string.Empty);

            // Health ya se inicializó en su Awake (y avisó antes de que hubiera nadie escuchando).
            if (playerHealth != null) OnPlayerHealthChanged(playerHealth.Current, playerHealth.Max);
            if (rvHealth != null) OnRvHealthChanged(rvHealth.Current, rvHealth.Max);
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

        private void OnPhaseChanged(PhaseChange change)
        {
            SetText(phaseLabel, GetPhaseName(change.Phase));
            if (change.Phase == GamePhase.Dusk)
            {
                ShowNotice("Se hace tarde: Jack se va al anochecer");
            }
        }

        private void OnPlayerHealthChanged(float current, float max) =>
            SetText(healthLabel, $"Vida {Mathf.CeilToInt(current)}");

        private void OnRvHealthChanged(float current, float max) =>
            SetText(rvLabel, $"Caravana {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}");

        private void OnHeadshot() => ShowNotice("¡En la cabeza!  Daño ×2");

        private void OnStrongIncoming(int count) =>
            ShowNotice(count == 1 ? "La cripta se abre: sale un zombi fuerte" : $"La cripta se abre: salen {count} zombis fuertes");

        private void OnReinforcementsIncoming(int count) =>
            ShowNotice($"¡Llegan {count} más! La noche se acelera");

        private void OnMatchEnded(MatchState state)
        {
            SetText(hintLabel, string.Empty);
            SetText(noticeLabel, string.Empty);
            noticeTimeLeft = 0f;

            if (state.Outcome == MatchOutcome.Victory)
            {
                SetText(bannerLabel, $"¡VICTORIA!\nSobreviviste {state.NightsSurvived} noches\n\n[Enter] Jugar de nuevo   [Esc] Menú");
                return;
            }

            string reason = state.DefeatReason == DefeatReason.RvDestroyed
                ? "La caravana fue destruida"
                : "No sobreviviste a la noche";
            SetText(bannerLabel, $"DERROTA\n{reason}\nNoches superadas: {state.NightsSurvived}\n\n[Enter] Reintentar desde cero   [Esc] Menú");
        }

        private void OnGoldChanged(int gold) => SetText(goldLabel, wallet != null ? $"Oro {gold}" : string.Empty);

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

        private string ReadyKeyName()
        {
            return interactor != null && interactor.TryGetComponent(out PlayerInputReader input)
                ? input.GetKeyName("Ready")
                : "Enter";
        }

        private void RefreshSelectedLabel()
        {
            ItemStack stack = playerInventory.Inventory.Selected;
            string ammo = weapon != null ? weapon.AmmoText : string.Empty;
            string label = stack.IsEmpty ? string.Empty : stack.Item.DisplayName;
            SetText(selectedItemLabel, string.IsNullOrEmpty(ammo) || stack.IsEmpty ? label : $"{label}   {ammo}");
        }

        private void RefreshPlantsLabel()
        {
            if (limiter == null)
            {
                SetText(plantsLabel, string.Empty);
                return;
            }

            string energy = powerMap != null && !float.IsInfinity(powerMap.TotalCapacity)
                ? $"   Energía {powerMap.TotalUsed:0}/{powerMap.TotalCapacity:0}"
                : string.Empty;
            SetText(plantsLabel,
                $"Plantas {limiter.GetCount(PlaceableCategory.Plant)}/{limiter.GetLimit(PlaceableCategory.Plant)}" +
                $"   Objetos {limiter.GetCount(PlaceableCategory.Object)}/{limiter.GetLimit(PlaceableCategory.Object)}" +
                energy);
        }

        private void ShowNotice(string message)
        {
            SetText(noticeLabel, message);
            noticeTimeLeft = noticeSeconds;
        }

        private static string BuildHint(InteractionContext context)
        {
            if (context.Interactable != null)
            {
                return $"[F] {context.Interactable.Prompt}";
            }

            if (!context.HasTarget)
            {
                return string.Empty;
            }

            switch (context.Primary)
            {
                case PrimaryActionKind.Harvest:
                    return $"[Clic] Cosechar {context.PlotCrop.DisplayName}";
                case PrimaryActionKind.Till:
                    return "[Clic] Arar";
                case PrimaryActionKind.Plant:
                    return $"[Clic] Plantar {context.CropToPlant.DisplayName}";
            }

            if (context.IsRipe)
            {
                return $"{context.PlotCrop.DisplayName}: listo (cosecha con la hoz dentada)";
            }

            if (context.HasPlot && context.PlotCrop != null)
            {
                return $"{context.PlotCrop.DisplayName}: creciendo";
            }

            return string.Empty;
        }

        private string GetPhaseName(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Dawn: return "Amanecer";
                case GamePhase.Day: return $"Día  ·  [{ReadyKeyName()}] Listo";
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
            PlayerInteractor playerInteractor, FarmWorld farmWorld, PlayerWallet walletOwner, Health player, Health rv,
            MatchDirector matchDirector, WaveDirector waveDirector, PlayerBuildTools tools, Text clock, Text phase, Text plants, Text gold, Text health, Text rvText,
            Text banner, Text selectedItem, Text hint, Text notice, List<SlotWidgets> slotWidgets)
        {
            dayNight = cycle;
            phaseChanged = phaseChannel;
            playerInventory = inventory;
            interactor = playerInteractor;
            world = farmWorld;
            playerWallet = walletOwner;
            playerHealth = player;
            rvHealth = rv;
            match = matchDirector;
            waves = waveDirector;
            buildTools = tools;
            goldLabel = gold;
            healthLabel = health;
            rvLabel = rvText;
            bannerLabel = banner;
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
