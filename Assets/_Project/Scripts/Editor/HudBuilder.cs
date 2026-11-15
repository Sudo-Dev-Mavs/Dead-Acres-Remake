using System.Collections.Generic;
using DeadAcres.Building;
using DeadAcres.Combat;
using DeadAcres.DayNight;
using DeadAcres.Economy;
using DeadAcres.Enemies;
using DeadAcres.Items;
using DeadAcres.Match;
using DeadAcres.Player;
using DeadAcres.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAcres.EditorTools
{
    /// <summary>
    /// Monta por código el Canvas de la interfaz provisional: HUD y ventana de la tienda.
    /// Diseñado sobre 1920×1080; escala con la pantalla.
    /// </summary>
    internal static class HudBuilder
    {
        private const float SlotSize = 72f;
        private const float SlotGap = 6f;

        public static HudView Build(DayNightCycle cycle, PhaseEventChannel phaseChannel, PlayerInventory inventory,
            PlayerInteractor interactor, FarmWorld world, PlayerWallet wallet, Shopkeeper shopkeeper,
            PlayerInputReader input, Health playerHealth, Health rvHealth, MatchDirector match, WaveDirector waves)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                Debug.LogError("No se encontró la fuente integrada LegacyRuntime.ttf; la interfaz saldrá sin texto.");
            }

            var canvasObject = new GameObject("HUD") { layer = LayerMask.NameToLayer("UI") };
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Para los botones de la tienda. El HUD en sí no recibe clics.
            canvasObject.AddComponent<GraphicRaycaster>();

            Transform root = canvasObject.transform;

            Text clock = UiFactory.CreateText("Clock", root, font, 30, TextAnchor.UpperRight);
            UiFactory.Place(clock.rectTransform, UiFactory.TopRight, UiFactory.TopRight, new Vector2(-24f, -18f), new Vector2(480f, 40f));

            Text phase = UiFactory.CreateText("Phase", root, font, 22, TextAnchor.UpperRight);
            UiFactory.Place(phase.rectTransform, UiFactory.TopRight, UiFactory.TopRight, new Vector2(-24f, -58f), new Vector2(480f, 30f));

            Text plants = UiFactory.CreateText("Placed", root, font, 24, TextAnchor.UpperLeft);
            UiFactory.Place(plants.rectTransform, UiFactory.TopLeft, UiFactory.TopLeft, new Vector2(24f, -18f), new Vector2(600f, 34f));

            Text gold = UiFactory.CreateText("Gold", root, font, 26, TextAnchor.UpperLeft);
            gold.color = new Color(1f, 0.86f, 0.35f);
            UiFactory.Place(gold.rectTransform, UiFactory.TopLeft, UiFactory.TopLeft, new Vector2(24f, -54f), new Vector2(400f, 36f));

            Text health = UiFactory.CreateText("Health", root, font, 28, TextAnchor.LowerLeft);
            UiFactory.Place(health.rectTransform, Vector2.zero, Vector2.zero, new Vector2(24f, 24f), new Vector2(300f, 40f));

            Text rv = UiFactory.CreateText("RV", root, font, 24, TextAnchor.UpperCenter);
            UiFactory.Place(rv.rectTransform, UiFactory.TopCenter, UiFactory.TopCenter, new Vector2(0f, -18f), new Vector2(500f, 34f));

            Text banner = UiFactory.CreateText("Banner", root, font, 44, TextAnchor.MiddleCenter);
            UiFactory.Place(banner.rectTransform, UiFactory.Center, UiFactory.Center, new Vector2(0f, 120f), new Vector2(1200f, 360f));

            Image crosshair = UiFactory.CreateImage("Crosshair", root, new Color(1f, 1f, 1f, 0.85f));
            UiFactory.Place(crosshair.rectTransform, UiFactory.Center, UiFactory.Center, Vector2.zero, new Vector2(6f, 6f));

            Text hint = UiFactory.CreateText("Hint", root, font, 22, TextAnchor.MiddleCenter);
            UiFactory.Place(hint.rectTransform, UiFactory.Center, UiFactory.Center, new Vector2(0f, -70f), new Vector2(900f, 32f));

            Text notice = UiFactory.CreateText("Notice", root, font, 22, TextAnchor.MiddleCenter);
            notice.color = new Color(1f, 0.9f, 0.45f);
            UiFactory.Place(notice.rectTransform, UiFactory.Center, UiFactory.Center, new Vector2(0f, -108f), new Vector2(900f, 32f));

            Text selectedItem = UiFactory.CreateText("SelectedItem", root, font, 22, TextAnchor.MiddleCenter);
            UiFactory.Place(selectedItem.rectTransform, UiFactory.BottomCenter, UiFactory.BottomCenter, new Vector2(0f, 104f), new Vector2(700f, 30f));

            List<HudView.SlotWidgets> slots = CreateHotbar(root, font);

            var hud = canvasObject.AddComponent<HudView>();
            hud.Configure(cycle, phaseChannel, inventory, interactor, world, wallet, playerHealth, rvHealth, match,
                waves, clock, phase, plants, gold, health, rv, banner, selectedItem, hint, notice, slots);

            // La tienda va la última: se dibuja por encima del HUD.
            BuildShop(canvasObject, font, cycle, shopkeeper, input);
            return hud;
        }

        private static void BuildShop(GameObject canvasObject, Font font, DayNightCycle cycle, Shopkeeper shopkeeper,
            PlayerInputReader input)
        {
            // Fondo que oscurece el juego y se traga los clics fuera de la ventana.
            Image dim = UiFactory.CreateImage("Shop", canvasObject.transform, new Color(0f, 0f, 0f, 0.6f), true);
            UiFactory.Stretch(dim.rectTransform);

            Image window = UiFactory.CreateImage("Window", dim.transform, new Color(0.12f, 0.13f, 0.15f, 0.97f), true);
            UiFactory.Place(window.rectTransform, UiFactory.Center, UiFactory.Center, Vector2.zero, new Vector2(1140f, 720f));
            Transform root = window.transform;

            Text title = UiFactory.CreateText("Title", root, font, 30, TextAnchor.UpperLeft, false);
            title.text = "Tienda de Jack";
            UiFactory.Place(title.rectTransform, UiFactory.TopLeft, UiFactory.TopLeft, new Vector2(30f, -20f), new Vector2(500f, 40f));

            Text gold = UiFactory.CreateText("Gold", root, font, 26, TextAnchor.UpperRight, false);
            gold.color = new Color(1f, 0.86f, 0.35f);
            UiFactory.Place(gold.rectTransform, UiFactory.TopRight, UiFactory.TopRight, new Vector2(-30f, -22f), new Vector2(300f, 36f));

            RectTransform buyList = CreateColumn(root, font, "Comprar", 30f);
            RectTransform sellList = CreateColumn(root, font, "Vender", 610f);

            Text message = UiFactory.CreateText("Message", root, font, 20, TextAnchor.MiddleLeft, false);
            message.color = new Color(1f, 0.6f, 0.45f);
            UiFactory.Place(message.rectTransform, Vector2.zero, Vector2.zero, new Vector2(30f, 22f), new Vector2(760f, 34f));

            Button close = UiFactory.CreateButton("Close", root, font, "Cerrar  [Esc]");
            UiFactory.Place(close.GetComponent<RectTransform>(), UiFactory.BottomRight, UiFactory.BottomRight,
                new Vector2(-30f, 20f), new Vector2(190f, 40f));

            var shop = canvasObject.AddComponent<ShopView>();
            shop.Configure(shopkeeper, cycle, input, dim.gameObject, buyList, sellList, gold, message, close, font);
            dim.gameObject.SetActive(false);
        }

        private static RectTransform CreateColumn(Transform root, Font font, string header, float x)
        {
            Text title = UiFactory.CreateText($"{header} header", root, font, 22, TextAnchor.UpperLeft, false);
            title.text = header;
            title.color = new Color(1f, 1f, 1f, 0.7f);
            UiFactory.Place(title.rectTransform, UiFactory.TopLeft, UiFactory.TopLeft, new Vector2(x, -80f), new Vector2(500f, 30f));

            RectTransform list = UiFactory.CreateRect($"{header} list", root);
            UiFactory.Place(list, UiFactory.TopLeft, UiFactory.TopLeft, new Vector2(x, -120f), new Vector2(500f, 550f));
            return list;
        }

        private static List<HudView.SlotWidgets> CreateHotbar(Transform root, Font font)
        {
            int count = Inventory.HotbarSize;
            float width = count * SlotSize + (count - 1) * SlotGap;

            RectTransform bar = UiFactory.CreateRect("Hotbar", root);
            UiFactory.Place(bar, UiFactory.BottomCenter, UiFactory.BottomCenter, new Vector2(0f, 24f), new Vector2(width, SlotSize));

            var slots = new List<HudView.SlotWidgets>(count);
            for (int i = 0; i < count; i++)
            {
                Image frame = UiFactory.CreateImage($"Slot {i + 1}", bar, new Color(0f, 0f, 0f, 0.55f));
                UiFactory.Place(frame.rectTransform, UiFactory.MiddleLeft, UiFactory.MiddleLeft,
                    new Vector2(i * (SlotSize + SlotGap), 0f), new Vector2(SlotSize, SlotSize));

                Image icon = UiFactory.CreateImage("Icon", frame.transform, Color.white);
                UiFactory.Stretch(icon.rectTransform, 14f);
                icon.enabled = false;

                Text key = UiFactory.CreateText("Key", frame.transform, font, 14, TextAnchor.UpperLeft);
                key.text = (i + 1).ToString();
                key.color = new Color(1f, 1f, 1f, 0.7f);
                UiFactory.Place(key.rectTransform, UiFactory.TopLeft, UiFactory.TopLeft, new Vector2(6f, -3f), new Vector2(24f, 18f));

                Text stackCount = UiFactory.CreateText("Count", frame.transform, font, 20, TextAnchor.LowerRight);
                UiFactory.Place(stackCount.rectTransform, UiFactory.BottomRight, UiFactory.BottomRight, new Vector2(-6f, 3f), new Vector2(60f, 24f));

                slots.Add(new HudView.SlotWidgets { frame = frame, icon = icon, count = stackCount });
            }

            return slots;
        }
    }
}
