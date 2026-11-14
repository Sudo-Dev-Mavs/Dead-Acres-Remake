using UnityEngine;
using UnityEngine.UI;

namespace DeadAcres.UI
{
    /// <summary>
    /// Piezas de uGUI creadas por código para la interfaz provisional. Usa el texto "legacy",
    /// que no necesita importar los recursos de TextMeshPro. Lo usan el generador de escena
    /// (en el editor) y las vistas que crean filas en runtime.
    /// </summary>
    public static class UiFactory
    {
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 MiddleLeft = new Vector2(0f, 0.5f);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 MiddleRight = new Vector2(1f, 0.5f);
        public static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        private const int UiLayer = 5;

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var element = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rect = (RectTransform)element.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static Text CreateText(string name, Transform parent, Font font, int fontSize, TextAnchor alignment,
            bool withShadow = true)
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

            if (withShadow)
            {
                // Para que se lea sobre el cielo y sobre la hierba.
                var shadow = rect.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                shadow.effectDistance = new Vector2(1.5f, -1.5f);
            }

            return text;
        }

        /// <param name="blocksClicks">True para fondos de ventana y botones; false para adornos del HUD.</param>
        public static Image CreateImage(string name, Transform parent, Color color, bool blocksClicks = false)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = blocksClicks;
            return image;
        }

        public static Button CreateButton(string name, Transform parent, Font font, string label, int fontSize = 18)
        {
            Image background = CreateImage(name, parent, new Color(0.25f, 0.42f, 0.25f, 1f), true);
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
            button.colors = colors;

            Text text = CreateText("Label", background.transform, font, fontSize, TextAnchor.MiddleCenter, false);
            text.text = label;
            Stretch(text.rectTransform);
            return button;
        }

        /// <summary>Tamaño fijo, anclado a un punto del padre.</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Ocupa todo el padre, con un margen opcional.</summary>
        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
