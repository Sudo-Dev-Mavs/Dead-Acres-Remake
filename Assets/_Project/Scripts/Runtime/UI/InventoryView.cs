using System;
using System.Collections.Generic;
using DeadAcres.DayNight;
using DeadAcres.Items;
using DeadAcres.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DeadAcres.UI
{
    /// <summary>
    /// Ventana provisional del inventario (hotbar + mochila) del jugador local. Se abre con la
    /// acción Inventario (Tab). Para mover o intercambiar: clic en una ranura y luego en otra.
    /// Solo pinta y pide <see cref="Inventory.Move"/>.
    /// </summary>
    public sealed class InventoryView : MonoBehaviour
    {
        [Serializable]
        internal sealed class SlotWidgets
        {
            public Button button;
            public Image frame;
            public Image icon;
            public Text count;
        }

        [Header("Fuentes de datos")]
        [SerializeField] private PlayerInputReader playerInput;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private DayNightCycle dayNight;

        [Header("Widgets")]
        [Tooltip("Raíz de la ventana; se activa al abrir el inventario.")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Text infoLabel;
        [SerializeField] private List<SlotWidgets> slots = new List<SlotWidgets>();

        [Header("Estilo")]
        [SerializeField] private Color slotColor = new Color(1f, 1f, 1f, 0.10f);
        [SerializeField] private Color pickedColor = new Color(1f, 0.82f, 0.25f, 0.9f);

        private const string HelpText = "Clic en una ranura y luego en otra para mover o intercambiar";

        private Inventory inventory;
        private int pickedSlot = -1;
        private bool openRequested;
        private bool listenersAdded;

        public bool IsOpen { get; private set; }

        private void OnEnable()
        {
            if (playerInput != null) playerInput.InventoryPressed += RequestOpen;

            inventory = playerInventory != null ? playerInventory.Inventory : null;
            if (inventory != null) inventory.SlotChanged += RefreshSlot;

            if (!listenersAdded)
            {
                listenersAdded = true;
                for (int i = 0; i < slots.Count; i++)
                {
                    int index = i;
                    if (slots[i].button != null) slots[i].button.onClick.AddListener(() => OnSlotClicked(index));
                }
            }

            if (panel != null) panel.SetActive(false);
        }

        private void OnDisable()
        {
            Close();
            if (playerInput != null) playerInput.InventoryPressed -= RequestOpen;
            if (inventory != null) inventory.SlotChanged -= RefreshSlot;
            inventory = null;
            openRequested = false;
        }

        // El input de juego está apagado mientras la ventana está abierta: las teclas de cerrar
        // se leen directamente del teclado.
        private void Update()
        {
            if (openRequested)
            {
                // La tecla que abrió sigue "recién pulsada" en este frame: no debe cerrar.
                openRequested = false;
                Open();
                return;
            }

            if (!IsOpen)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.tabKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
            {
                Close();
            }
        }

        // Llega dentro de un callback del Input System; abrir apaga ese mismo input, así que se
        // deja para el Update de este frame.
        private void RequestOpen()
        {
            if (!IsOpen)
            {
                openRequested = true;
            }
        }

        private void Open()
        {
            if (IsOpen || panel == null || inventory == null)
            {
                return;
            }

            IsOpen = true;
            pickedSlot = -1;
            UiModal.Enter(playerInput, dayNight);
            panel.SetActive(true);
            SetInfo(HelpText);

            for (int i = 0; i < slots.Count; i++)
            {
                RefreshSlot(i);
            }
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            pickedSlot = -1;
            if (panel != null) panel.SetActive(false);
            UiModal.Exit(playerInput, dayNight);
        }

        private void OnSlotClicked(int index)
        {
            if (!IsOpen || index >= inventory.SlotCount)
            {
                return;
            }

            if (pickedSlot < 0)
            {
                ItemStack stack = inventory[index];
                if (stack.IsEmpty)
                {
                    return;
                }

                pickedSlot = index;
                SetInfo($"{stack.Item.DisplayName}: elige dónde ponerlo");
                RefreshSlot(index);
                return;
            }

            int from = pickedSlot;
            pickedSlot = -1;
            if (from != index)
            {
                inventory.Move(from, index);
            }

            SetInfo(HelpText);
            RefreshSlot(from);
            RefreshSlot(index);
        }

        private void RefreshSlot(int index)
        {
            if (!IsOpen || index < 0 || index >= slots.Count || index >= inventory.SlotCount)
            {
                return;
            }

            ItemStack stack = inventory[index];
            SlotWidgets widgets = slots[index];

            if (widgets.frame != null)
            {
                widgets.frame.color = index == pickedSlot ? pickedColor : slotColor;
            }

            if (widgets.icon != null)
            {
                widgets.icon.enabled = !stack.IsEmpty;
                if (!stack.IsEmpty)
                {
                    widgets.icon.sprite = stack.Item.Icon;
                    widgets.icon.color = stack.Item.Icon != null ? Color.white : stack.Item.Tint;
                }
            }

            if (widgets.count != null)
            {
                widgets.count.text = stack.Count > 1 ? stack.Count.ToString() : string.Empty;
            }
        }

        private void SetInfo(string text)
        {
            if (infoLabel != null)
            {
                infoLabel.text = text;
            }
        }

        internal void Configure(PlayerInputReader input, PlayerInventory inventoryOwner, DayNightCycle cycle,
            GameObject panelRoot, Text info, List<SlotWidgets> slotWidgets)
        {
            playerInput = input;
            playerInventory = inventoryOwner;
            dayNight = cycle;
            panel = panelRoot;
            infoLabel = info;
            slots = slotWidgets;
        }
    }
}
