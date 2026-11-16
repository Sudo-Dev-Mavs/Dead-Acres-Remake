using UnityEngine;
using UnityEngine.UI;

namespace DeadAcres.UI
{
    /// <summary>Piezas comunes de los menús que se montan por código (principal y pausa).</summary>
    public static class MenuUi
    {
        public const float ButtonWidth = 460f;
        public const float ButtonHeight = 54f;
        public const float ButtonSpacing = 14f;

        public static Font LoadFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Canvas a pantalla completa, con el mismo escalado que el HUD.</summary>
        public static Canvas CreateCanvas(string name, Transform parent, int sortingOrder)
        {
            var canvasObject = new GameObject(name) { layer = 5 };
            canvasObject.transform.SetParent(parent, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>Botón de la columna central: el número de fila cuenta desde arriba.</summary>
        public static Button AddButton(Transform parent, Font font, string label, int row, float top)
        {
            Button button = UiFactory.CreateButton(label, parent, font, label, 24);
            UiFactory.Place(button.GetComponent<RectTransform>(), UiFactory.TopCenter, UiFactory.TopCenter,
                new Vector2(0f, top - row * (ButtonHeight + ButtonSpacing)), new Vector2(ButtonWidth, ButtonHeight));
            return button;
        }

        public static void SetLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
        }
    }
}
