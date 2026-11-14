using System.Collections.Generic;
using DeadAcres.Building;
using DeadAcres.DayNight;
using DeadAcres.Items;
using DeadAcres.Player;
using DeadAcres.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAcres.EditorTools
{
    /// <summary>
    /// Monta por código el Canvas del HUD provisional (uGUI con texto "legacy", que no necesita
    /// importar los recursos de TextMeshPro). Diseñado sobre 1920×1080; escala con la pantalla.
    /// </summary>
    internal static class HudBuilder
    {
        private const float SlotSize = 72f;
        private const float SlotGap = 6f;

        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 MiddleLeft = new Vector2(0f, 0.5f);

        public static HudView Build(DayNightCycle cycle, PhaseEventChannel phaseChannel, PlayerInventory inventory,
            PlayerInteractor interactor, FarmWorld world)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                Debug.LogError("No se encontró la fuente integrada LegacyRuntime.ttf; el HUD saldrá sin texto.");
            }

            var canvasObject = new GameObject("HUD") { layer = LayerMask.NameToLayer("UI") };
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Transform root = canvasObject.transform;

            Text clock = CreateText("Clock", root, font, 30, TextAnchor.UpperRight);
            Place(clock.rectTransform, TopRight, TopRight, new Vector2(-24f, -18f), new Vector2(480f, 40f));

            Text phase = CreateText("Phase", root, font, 22, TextAnchor.UpperRight);
            Place(phase.rectTransform, TopRight, TopRight, new Vector2(-24f, -58f), new Vector2(480f, 30f));

            Text plants = CreateText("Plants", root, font, 24, TextAnchor.UpperLeft);
            Place(plants.rectTransform, TopLeft, TopLeft, new Vector2(24f, -18f), new Vector2(480f, 34f));

            Image crosshair = CreateImage("Crosshair", root, new Color(1f, 1f, 1f, 0.85f));
            Place(crosshair.rectTransform, Center, Center, Vector2.zero, new Vector2(6f, 6f));

            Text hint = CreateText("Hint", root, font, 22, TextAnchor.MiddleCenter);
            Place(hint.rectTransform, Center, Center, new Vector2(0f, -70f), new Vector2(900f, 32f));

            Text notice = CreateText("Notice", root, font, 22, TextAnchor.MiddleCenter);
            notice.color = new Color(1f, 0.9f, 0.45f);
            Place(notice.rectTransform, Center, Center, new Vector2(0f, -108f), new Vector2(900f, 32f));

            Text selectedItem = CreateText("SelectedItem", root, font, 22, TextAnchor.MiddleCenter);
            Place(selectedItem.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 104f), new Vector2(700f, 30f));

            List<HudView.SlotWidgets> slots = CreateHotbar(root, font);

            var hud = canvasObject.AddComponent<HudView>();
            hud.Configure(cycle, phaseChannel, inventory, interactor, world, clock, phase, plants, selectedItem,
                hint, notice, slots);
            return hud;
        }

        private static List<HudView.SlotWidgets> CreateHotbar(Transform root, Font font)
        {
            int count = Inventory.HotbarSize;
            float width = count * SlotSize + (count - 1) * SlotGap;

            RectTransform bar = CreateRect("Hotbar", root);
            Place(bar, BottomCenter, BottomCenter, new Vector2(0f, 24f), new Vector2(width, SlotSize));

            var slots = new List<HudView.SlotWidgets>(count);
            for (int i = 0; i < count; i++)
            {
                Image frame = CreateImage($"Slot {i + 1}", bar, new Color(0f, 0f, 0f, 0.55f));
                Place(frame.rectTransform, MiddleLeft, MiddleLeft, new Vector2(i * (SlotSize + SlotGap), 0f),
                    new Vector2(SlotSize, SlotSize));

                Image icon = CreateImage("Icon", frame.transform, Color.white);
                RectTransform iconRect = icon.rectTransform;
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(14f, 14f);
                iconRect.offsetMax = new Vector2(-14f, -14f);
                icon.enabled = false;

                Text key = CreateText("Key", frame.transform, font, 14, TextAnchor.UpperLeft);
                key.text = (i + 1).ToString();
                key.color = new Color(1f, 1f, 1f, 0.7f);
                Place(key.rectTransform, TopLeft, TopLeft, new Vector2(6f, -3f), new Vector2(24f, 18f));

                Text stackCount = CreateText("Count", frame.transform, font, 20, TextAnchor.LowerRight);
                Place(stackCount.rectTransform, BottomRight, BottomRight, new Vector2(-6f, 3f), new Vector2(60f, 24f));

                slots.Add(new HudView.SlotWidgets { frame = frame, icon = icon, count = stackCount });
            }

            return slots;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var element = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rect = (RectTransform)element.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static Text CreateText(string name, Transform parent, Font font, int fontSize, TextAnchor alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = string.Empty;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            // Sombra para que se lea sobre el cielo y sobre la hierba.
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
