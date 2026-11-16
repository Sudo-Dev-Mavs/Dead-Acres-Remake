using System;
using System.Collections.Generic;
using DeadAcres.Audio;
using DeadAcres.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DeadAcres.UI
{
    /// <summary>
    /// Ventana de opciones, compartida por el menú principal y el de pausa. Dos pestañas:
    /// ajustes (sonido, ratón y mando, cámara, pantalla) y controles (teclas y botones del mando).
    /// Monta su interfaz por código y guarda cada cambio al momento.
    /// </summary>
    public sealed class OptionsPanel : MonoBehaviour
    {
        private const float RowHeight = 38f;
        private const float RowSpacing = 6f;

        private readonly List<Action> refreshers = new List<Action>();
        private GameObject root;
        private GameObject settingsTab;
        private GameObject controlsTab;
        private Text message;
        private Font font;
        private InputActionMap actions;
        private InputActionRebindingExtensions.RebindingOperation rebind;
        private List<Resolution> resolutions;

        /// <summary>Se cerró la ventana.</summary>
        public event Action Closed;

        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>Está esperando una tecla para asignarla: quien lea el teclado debe ignorarlo.</summary>
        public bool IsCapturingInput => rebind != null;

        /// <summary>Crea la ventana, oculta, dentro de un canvas.</summary>
        public static OptionsPanel Create(Transform canvas, Font uiFont)
        {
            Image dim = UiFactory.CreateImage("Options", canvas, new Color(0f, 0f, 0f, 0.8f), true);
            UiFactory.Stretch(dim.rectTransform);
            var panel = dim.gameObject.AddComponent<OptionsPanel>();
            panel.Build(dim.gameObject, uiFont);
            dim.gameObject.SetActive(false);
            return panel;
        }

        public void Show()
        {
            // Mapa propio, nunca activado: solo sirve para enseñar y cambiar las teclas.
            actions?.Dispose();
            actions = GameInput.Create();
            GameInput.LoadOverrides(actions);
            ShowTab(true);
            message.text = string.Empty;
            root.SetActive(true);
            RefreshAll();
        }

        public void Hide()
        {
            CancelRebind();
            if (root != null && root.activeSelf)
            {
                root.SetActive(false);
                Closed?.Invoke();
            }
        }

        private void OnDestroy()
        {
            CancelRebind();
            actions?.Dispose();
        }

        private void Build(GameObject rootObject, Font uiFont)
        {
            root = rootObject;
            font = uiFont;

            Image window = UiFactory.CreateImage("Window", root.transform, new Color(0.12f, 0.13f, 0.15f, 0.98f), true);
            UiFactory.Place(window.rectTransform, UiFactory.Center, UiFactory.Center, Vector2.zero, new Vector2(1200f, 900f));
            Transform frame = window.transform;

            Text title = UiFactory.CreateText("Title", frame, font, 36, TextAnchor.MiddleLeft, false);
            title.text = "Opciones";
            UiFactory.Place(title.rectTransform, UiFactory.TopLeft, UiFactory.TopLeft, new Vector2(36f, -22f), new Vector2(400f, 46f));

            Button settingsButton = UiFactory.CreateButton("TabSettings", frame, font, "Ajustes", 20);
            UiFactory.Place(settingsButton.GetComponent<RectTransform>(), UiFactory.TopRight, UiFactory.TopRight,
                new Vector2(-236f, -26f), new Vector2(190f, 40f));
            settingsButton.onClick.AddListener(() => ShowTab(true));

            Button controlsButton = UiFactory.CreateButton("TabControls", frame, font, "Controles", 20);
            UiFactory.Place(controlsButton.GetComponent<RectTransform>(), UiFactory.TopRight, UiFactory.TopRight,
                new Vector2(-36f, -26f), new Vector2(190f, 40f));
            controlsButton.onClick.AddListener(() => ShowTab(false));

            settingsTab = UiFactory.CreateRect("Settings", frame).gameObject;
            UiFactory.Stretch((RectTransform)settingsTab.transform);
            controlsTab = UiFactory.CreateRect("Controls", frame).gameObject;
            UiFactory.Stretch((RectTransform)controlsTab.transform);

            BuildSettings(settingsTab.transform);
            BuildControls(controlsTab.transform);

            message = UiFactory.CreateText("Message", frame, font, 18, TextAnchor.MiddleLeft, false);
            message.color = new Color(1f, 0.8f, 0.45f);
            UiFactory.Place(message.rectTransform, Vector2.zero, Vector2.zero, new Vector2(36f, 24f), new Vector2(820f, 36f));

            Button close = UiFactory.CreateButton("Close", frame, font, "Volver", 22);
            UiFactory.Place(close.GetComponent<RectTransform>(), UiFactory.BottomRight, UiFactory.BottomRight,
                new Vector2(-36f, 22f), new Vector2(220f, 46f));
            close.onClick.AddListener(Hide);
        }

        private void ShowTab(bool settings)
        {
            CancelRebind();
            settingsTab.SetActive(settings);
            controlsTab.SetActive(!settings);
            if (message != null) message.text = string.Empty;
        }

        // ---------- pestaña de ajustes: cada fila es un botón que pasa al valor siguiente

        private void BuildSettings(Transform parent)
        {
            int left = 0;
            int right = 0;

            AddHeader(parent, "Sonido", 0, left++);
            AddSetting(parent, 0, left++, () => VolumeSteps.Format("Volumen general", GameSettings.MasterVolume),
                () => GameSettings.MasterVolume = VolumeSteps.Next(GameSettings.MasterVolume));
            AddSetting(parent, 0, left++, () => VolumeSteps.Format("Efectos", GameSettings.EffectsVolume),
                () => GameSettings.EffectsVolume = VolumeSteps.Next(GameSettings.EffectsVolume));
            AddSetting(parent, 0, left++, () => VolumeSteps.Format("Música", GameSettings.MusicVolume),
                () => GameSettings.MusicVolume = VolumeSteps.Next(GameSettings.MusicVolume));

            left++;
            AddHeader(parent, "Ratón y mando", 0, left++);
            AddSetting(parent, 0, left++, () => $"Sensibilidad del ratón: {GameSettings.MouseSensitivity:0.##}×",
                () => GameSettings.MouseSensitivity = StepCycle.Next(GameSettings.SensitivitySteps, GameSettings.MouseSensitivity));
            AddSetting(parent, 0, left++, () => $"Sensibilidad del mando: {GameSettings.StickSensitivity:0.##}×",
                () => GameSettings.StickSensitivity = StepCycle.Next(GameSettings.SensitivitySteps, GameSettings.StickSensitivity));
            AddSetting(parent, 0, left++, () => $"Invertir eje vertical: {YesNo(GameSettings.InvertY)}",
                () => GameSettings.InvertY = !GameSettings.InvertY);

            AddHeader(parent, "Cámara", 1, right++);
            AddSetting(parent, 1, right++, () => $"Campo de visión: {GameSettings.FieldOfView:0}°",
                () => GameSettings.FieldOfView = StepCycle.Next(GameSettings.FieldOfViewSteps, GameSettings.FieldOfView));

            right++;
            AddHeader(parent, "Pantalla", 1, right++);
            AddSetting(parent, 1, right++, () => $"Pantalla completa: {YesNo(GameSettings.Fullscreen)}",
                () => GameSettings.Fullscreen = !GameSettings.Fullscreen);
            AddSetting(parent, 1, right++, ResolutionLabel, NextResolution);
            AddSetting(parent, 1, right++, () => $"Sincronía vertical: {YesNo(GameSettings.VSync)}",
                () => GameSettings.VSync = !GameSettings.VSync);
            AddSetting(parent, 1, right++,
                () => GameSettings.FrameLimit > 0 ? $"Límite de FPS: {GameSettings.FrameLimit}" : "Límite de FPS: sin límite",
                () => GameSettings.FrameLimit = (int)StepCycle.Next(GameSettings.FrameLimitSteps, GameSettings.FrameLimit));
        }

        private static string YesNo(bool value) => value ? "sí" : "no";

        private string ResolutionLabel()
        {
            int width = GameSettings.ResolutionWidth > 0 ? GameSettings.ResolutionWidth : Screen.width;
            int height = GameSettings.ResolutionHeight > 0 ? GameSettings.ResolutionHeight : Screen.height;
            return $"Resolución: {width} × {height}";
        }

        private void NextResolution()
        {
            if (resolutions == null)
            {
                // Screen.resolutions repite cada tamaño por frecuencia de refresco: se queda con uno.
                resolutions = new List<Resolution>();
                foreach (Resolution candidate in Screen.resolutions)
                {
                    if (!resolutions.Exists(r => r.width == candidate.width && r.height == candidate.height))
                    {
                        resolutions.Add(candidate);
                    }
                }
            }

            if (resolutions.Count == 0)
            {
                return;
            }

            int current = resolutions.FindIndex(r => r.width == GameSettings.ResolutionWidth && r.height == GameSettings.ResolutionHeight);
            Resolution next = resolutions[(current + 1) % resolutions.Count];
            GameSettings.SetResolution(next.width, next.height);
            if (Application.isEditor) message.text = "En el editor la resolución y la pantalla completa no cambian: se ven en el juego compilado.";
        }

        private void AddHeader(Transform parent, string text, int column, int row)
        {
            Text header = UiFactory.CreateText("Header", parent, font, 22, TextAnchor.MiddleLeft, false);
            header.text = text;
            header.color = new Color(0.85f, 0.92f, 0.70f);
            UiFactory.Place(header.rectTransform, UiFactory.TopLeft, UiFactory.TopLeft, CellPosition(column, row), new Vector2(540f, RowHeight));
        }

        private void AddSetting(Transform parent, int column, int row, Func<string> label, Action change)
        {
            Button button = UiFactory.CreateButton("Setting", parent, font, string.Empty, 20);
            UiFactory.Place(button.GetComponent<RectTransform>(), UiFactory.TopLeft, UiFactory.TopLeft, CellPosition(column, row),
                new Vector2(540f, RowHeight));
            button.onClick.AddListener(() =>
            {
                change();
                RefreshAll();
            });
            refreshers.Add(() => MenuUi.SetLabel(button, label()));
        }

        private static Vector2 CellPosition(int column, int row)
        {
            return new Vector2(36f + column * 588f, -92f - row * (RowHeight + RowSpacing));
        }

        // ---------- pestaña de controles

        private void BuildControls(Transform parent)
        {
            AddColumnTitle(parent, "Acción", 36f, 420f);
            AddColumnTitle(parent, "Teclado y ratón", 470f, 330f);
            AddColumnTitle(parent, "Mando", 816f, 330f);

            int row = 1;
            foreach ((string part, string label) in GameInput.MoveParts)
            {
                AddControlRow(parent, row++, label, "Move", part, false);
            }

            foreach ((string action, string label) in GameInput.Rebindable)
            {
                AddControlRow(parent, row++, label, action, null, true);
            }

            Button reset = UiFactory.CreateButton("Reset", parent, font, "Restablecer controles", 18);
            UiFactory.Place(reset.GetComponent<RectTransform>(), UiFactory.BottomRight, UiFactory.BottomRight,
                new Vector2(-276f, 22f), new Vector2(280f, 46f));
            reset.onClick.AddListener(() =>
            {
                CancelRebind();
                GameInput.ResetOverrides();
                GameInput.LoadOverrides(actions);
                message.text = "Controles restablecidos.";
                RefreshAll();
            });
        }

        private void AddColumnTitle(Transform parent, string text, float x, float width)
        {
            Text label = UiFactory.CreateText("ColumnTitle", parent, font, 20, TextAnchor.MiddleLeft, false);
            label.text = text;
            label.color = new Color(0.85f, 0.92f, 0.70f);
            UiFactory.Place(label.rectTransform, UiFactory.TopLeft, UiFactory.TopLeft, new Vector2(x, -86f), new Vector2(width, 32f));
        }

        private void AddControlRow(Transform parent, int row, string label, string actionName, string compositePart,
            bool withGamepad)
        {
            float y = -86f - row * (34f + 5f);
            Text name = UiFactory.CreateText("Action", parent, font, 19, TextAnchor.MiddleLeft, false);
            name.text = label;
            UiFactory.Place(name.rectTransform, UiFactory.TopLeft, UiFactory.TopLeft, new Vector2(36f, y), new Vector2(420f, 34f));

            AddBindingButton(parent, new Vector2(470f, y), actionName, compositePart, false);
            if (withGamepad)
            {
                AddBindingButton(parent, new Vector2(816f, y), actionName, null, true);
            }
            else
            {
                Text fixedStick = UiFactory.CreateText("Fixed", parent, font, 18, TextAnchor.MiddleLeft, false);
                fixedStick.text = "Stick izquierdo";
                fixedStick.color = new Color(0.7f, 0.7f, 0.7f);
                UiFactory.Place(fixedStick.rectTransform, UiFactory.TopLeft, UiFactory.TopLeft, new Vector2(826f, y), new Vector2(330f, 34f));
            }
        }

        private void AddBindingButton(Transform parent, Vector2 position, string actionName, string compositePart, bool gamepad)
        {
            Button button = UiFactory.CreateButton("Binding", parent, font, "-", 18);
            UiFactory.Place(button.GetComponent<RectTransform>(), UiFactory.TopLeft, UiFactory.TopLeft, position, new Vector2(330f, 34f));
            button.onClick.AddListener(() => StartRebind(button, actionName, compositePart, gamepad));
            refreshers.Add(() =>
            {
                InputAction action = actions != null ? actions.FindAction(actionName) : null;
                int index = FindIndex(action, compositePart, gamepad);
                button.interactable = index >= 0;
                MenuUi.SetLabel(button, GameInput.GetDisplayName(action, index));
            });
        }

        private static int FindIndex(InputAction action, string compositePart, bool gamepad)
        {
            if (action == null) return -1;
            return compositePart != null ? GameInput.FindCompositePart(action, compositePart) : GameInput.FindBinding(action, gamepad);
        }

        private void StartRebind(Button button, string actionName, string compositePart, bool gamepad)
        {
            CancelRebind();
            InputAction action = actions.FindAction(actionName);
            int index = FindIndex(action, compositePart, gamepad);
            if (index < 0)
            {
                return;
            }

            MenuUi.SetLabel(button, "…");
            message.text = gamepad ? "Pulsa un botón del mando. Esc cancela." : "Pulsa una tecla o un botón del ratón. Esc cancela.";

            rebind = action.PerformInteractiveRebinding(index)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f);

            // Cada columna solo escucha a su dispositivo, y nunca al movimiento del ratón.
            if (gamepad)
            {
                rebind.WithControlsExcluding("<Keyboard>").WithControlsExcluding("<Mouse>").WithControlsExcluding("<Pointer>");
            }
            else
            {
                rebind.WithControlsExcluding("<Gamepad>")
                    .WithControlsExcluding("<Mouse>/delta")
                    .WithControlsExcluding("<Mouse>/position")
                    .WithControlsExcluding("<Mouse>/scroll")
                    .WithControlsExcluding("<Pointer>/delta")
                    .WithControlsExcluding("<Pointer>/position");
            }

            rebind.OnComplete(_ => FinishRebind(true)).OnCancel(_ => FinishRebind(false)).Start();
        }

        private void FinishRebind(bool changed)
        {
            rebind?.Dispose();
            rebind = null;
            if (changed)
            {
                GameInput.SaveOverrides(actions);
                message.text = "Guardado.";
            }
            else
            {
                message.text = string.Empty;
            }

            RefreshAll();
        }

        private void CancelRebind()
        {
            if (rebind == null)
            {
                return;
            }

            // Cancel llama a OnCancel, que ya limpia.
            rebind.Cancel();
        }

        private void RefreshAll()
        {
            for (int i = 0; i < refreshers.Count; i++)
            {
                refreshers[i]();
            }
        }
    }
}
