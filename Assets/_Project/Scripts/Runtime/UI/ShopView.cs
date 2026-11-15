using System.Collections.Generic;
using DeadAcres.DayNight;
using DeadAcres.Economy;
using DeadAcres.Items;
using DeadAcres.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DeadAcres.UI
{
    /// <summary>
    /// Ventana provisional de la tienda de Jack para el jugador local. Mientras está abierta se
    /// suelta el cursor, se apaga el input de juego y, como en el original, se detiene el reloj
    /// (solo tiene sentido con un jugador: en co-op el reloj no podrá pararse).
    /// Solo pinta y pide operaciones a <see cref="ShopCommands"/>.
    /// </summary>
    public sealed class ShopView : MonoBehaviour
    {
        private const float RowHeight = 38f;
        private const float RowSpacing = 4f;
        private const float RowWidth = 500f;
        private const int MaxRows = 13;

        [Header("Fuentes de datos")]
        [SerializeField] private Shopkeeper shopkeeper;
        [SerializeField] private DayNightCycle dayNight;
        [SerializeField] private PlayerInputReader playerInput;

        [Header("Widgets")]
        [Tooltip("Raíz de la ventana; se activa al abrir la tienda.")]
        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform buyList;
        [SerializeField] private RectTransform sellList;
        [SerializeField] private Text goldLabel;
        [SerializeField] private Text messageLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Font font;

        private readonly List<ItemDefinition> itemBuffer = new List<ItemDefinition>();
        private Inventory inventory;
        private Wallet wallet;
        private GameObject pendingPlayer;

        public bool IsOpen { get; private set; }

        private void OnEnable()
        {
            if (shopkeeper != null) shopkeeper.ShopRequested += RequestOpen;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (panel != null) panel.SetActive(false);
        }

        private void OnDisable()
        {
            Close();
            if (shopkeeper != null) shopkeeper.ShopRequested -= RequestOpen;
            pendingPlayer = null;
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        // El input de juego está apagado mientras la tienda está abierta, así que las teclas de
        // cerrar se leen directamente del teclado.
        private void Update()
        {
            if (pendingPlayer != null)
            {
                GameObject player = pendingPlayer;
                pendingPlayer = null;
                Open(player);

                // La tecla que abrió la tienda sigue "recién pulsada" en este frame: no debe cerrarla.
                return;
            }

            if (!IsOpen)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame))
            {
                Close();
            }
        }

        // La petición llega dentro de un callback del Input System. Abrir implica apagar ese
        // mismo input, así que se deja para el Update de este frame.
        private void RequestOpen(Shopkeeper keeper, GameObject player)
        {
            if (!IsOpen)
            {
                pendingPlayer = player;
            }
        }

        private void Open(GameObject player)
        {
            if (IsOpen || panel == null || player == null)
            {
                return;
            }

            var playerInventory = player.GetComponent<PlayerInventory>();
            var playerWallet = player.GetComponent<PlayerWallet>();
            if (playerInventory == null || playerWallet == null)
            {
                Debug.LogError("La tienda necesita PlayerInventory y PlayerWallet en el jugador.", player);
                return;
            }

            inventory = playerInventory.Inventory;
            wallet = playerWallet.Wallet;
            IsOpen = true;

            UiModal.Enter(playerInput, dayNight);
            SetMessage(string.Empty);
            panel.SetActive(true);
            Rebuild();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            if (panel != null) panel.SetActive(false);
            UiModal.Exit(playerInput, dayNight);
            inventory = null;
            wallet = null;
        }

        // Se reconstruyen las dos listas tras cada operación: son pocas filas y solo ocurre al
        // pulsar un botón, nunca por frame.
        private void Rebuild()
        {
            int day = dayNight != null && dayNight.Clock != null ? dayNight.Clock.DayNumber : 1;
            goldLabel.text = $"Oro: {wallet.Gold}";

            ClearRows(buyList);
            itemBuffer.Clear();
            shopkeeper.Catalog.GetAvailable(day, itemBuffer);
            for (int i = 0; i < itemBuffer.Count && i < MaxRows; i++)
            {
                AddBuyRow(i, itemBuffer[i], day);
            }

            ClearRows(sellList);
            itemBuffer.Clear();
            CollectSellableItems(itemBuffer);
            for (int i = 0; i < itemBuffer.Count && i < MaxRows; i++)
            {
                AddSellRow(i, itemBuffer[i]);
            }
        }

        private void CollectSellableItems(List<ItemDefinition> results)
        {
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                ItemStack stack = inventory[i];
                if (!stack.IsEmpty && stack.Item.SellPrice > 0 && !results.Contains(stack.Item))
                {
                    results.Add(stack.Item);
                }
            }
        }

        private void AddBuyRow(int index, ItemDefinition item, int day)
        {
            RectTransform row = CreateRow(buyList, index, item, item.DisplayName, $"{item.BuyPrice} oro");

            Button buy = UiFactory.CreateButton("Buy", row, font, "Comprar");
            UiFactory.Place(buy.GetComponent<RectTransform>(), UiFactory.MiddleRight, UiFactory.MiddleRight,
                new Vector2(-6f, 0f), new Vector2(110f, 30f));
            buy.interactable =
                ShopCommands.CanBuy(shopkeeper.Catalog, item, 1, day, wallet, inventory) == ShopResult.Ok;
            buy.onClick.AddListener(() =>
                Report(ShopCommands.Buy(shopkeeper.Catalog, item, 1, day, wallet, inventory)));
        }

        private void AddSellRow(int index, ItemDefinition item)
        {
            int owned = inventory.CountOf(item);
            RectTransform row = CreateRow(sellList, index, item, $"{item.DisplayName}  ×{owned}",
                $"{item.SellPrice} c/u");

            Button sellAll = UiFactory.CreateButton("SellAll", row, font, "Todo");
            UiFactory.Place(sellAll.GetComponent<RectTransform>(), UiFactory.MiddleRight, UiFactory.MiddleRight,
                new Vector2(-6f, 0f), new Vector2(70f, 30f));
            sellAll.onClick.AddListener(() => Report(ShopCommands.Sell(item, inventory.CountOf(item), wallet, inventory)));

            Button sellOne = UiFactory.CreateButton("SellOne", row, font, "1");
            UiFactory.Place(sellOne.GetComponent<RectTransform>(), UiFactory.MiddleRight, UiFactory.MiddleRight,
                new Vector2(-82f, 0f), new Vector2(46f, 30f));
            sellOne.onClick.AddListener(() => Report(ShopCommands.Sell(item, 1, wallet, inventory)));
        }

        private RectTransform CreateRow(RectTransform list, int index, ItemDefinition item, string title, string price)
        {
            Image background = UiFactory.CreateImage("Row", list, new Color(1f, 1f, 1f, 0.07f));
            RectTransform row = background.rectTransform;
            UiFactory.Place(row, UiFactory.TopLeft, UiFactory.TopLeft,
                new Vector2(0f, -index * (RowHeight + RowSpacing)), new Vector2(RowWidth, RowHeight));

            Image icon = UiFactory.CreateImage("Icon", row, item.Icon != null ? Color.white : item.Tint);
            icon.sprite = item.Icon;
            UiFactory.Place(icon.rectTransform, UiFactory.MiddleLeft, UiFactory.MiddleLeft,
                new Vector2(8f, 0f), new Vector2(28f, 28f));

            Text name = UiFactory.CreateText("Name", row, font, 20, TextAnchor.MiddleLeft, false);
            name.text = title;
            UiFactory.Place(name.rectTransform, UiFactory.MiddleLeft, UiFactory.MiddleLeft,
                new Vector2(46f, 0f), new Vector2(230f, RowHeight));

            Text priceText = UiFactory.CreateText("Price", row, font, 18, TextAnchor.MiddleRight, false);
            priceText.text = price;
            priceText.color = new Color(1f, 0.86f, 0.35f);
            UiFactory.Place(priceText.rectTransform, UiFactory.MiddleRight, UiFactory.MiddleRight,
                new Vector2(-140f, 0f), new Vector2(90f, RowHeight));

            return row;
        }

        private static void ClearRows(RectTransform list)
        {
            for (int i = list.childCount - 1; i >= 0; i--)
            {
                // Destroy es diferido: se desactiva ya para que no se vea junto a las filas nuevas.
                GameObject child = list.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private void Report(ShopResult result)
        {
            SetMessage(GetMessage(result));
            Rebuild();
        }

        private void SetMessage(string message)
        {
            if (messageLabel != null)
            {
                messageLabel.text = message;
            }
        }

        private static string GetMessage(ShopResult result)
        {
            switch (result)
            {
                case ShopResult.Ok: return string.Empty;
                case ShopResult.NotEnoughGold: return "No te alcanza el oro";
                case ShopResult.InventoryFull: return "Inventario lleno";
                case ShopResult.NotAvailableYet: return "Jack todavía no lo trae";
                case ShopResult.NothingToSell: return "No tienes eso para vender";
                case ShopResult.NotSellable: return "Jack no compra eso";
                default: return "Jack no vende eso";
            }
        }

        internal void Configure(Shopkeeper keeper, DayNightCycle cycle, PlayerInputReader input, GameObject panelRoot,
            RectTransform buyRoot, RectTransform sellRoot, Text gold, Text message, Button close, Font uiFont)
        {
            shopkeeper = keeper;
            dayNight = cycle;
            playerInput = input;
            panel = panelRoot;
            buyList = buyRoot;
            sellList = sellRoot;
            goldLabel = gold;
            messageLabel = message;
            closeButton = close;
            font = uiFont;
        }
    }
}
