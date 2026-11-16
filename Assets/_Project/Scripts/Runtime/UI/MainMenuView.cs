using System.Collections.Generic;
using DeadAcres.Core.Session;
using DeadAcres.Difficulty;
using DeadAcres.GameMode;
using DeadAcres.Saving;
using DeadAcres.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeadAcres.UI
{
    /// <summary>
    /// Menú principal: un panel a la izquierda sobre la partida de demostración. Va por páginas
    /// con botón de volver: Jugar → modo → dificultad → ranura de guardado, o cargar una partida.
    /// Monta su interfaz por código y deja la elección en la <see cref="GameSession"/>.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        private enum Page
        {
            Root,
            Play,
            Difficulty,
            NewSlot,
            LoadSlot,
        }

        private const float PanelWidth = 700f;
        private const float WideButton = 620f;

        [SerializeField] private GameSession session;
        [SerializeField] private List<DifficultyProfile> difficulties = new List<DifficultyProfile>();
        [SerializeField] private List<GameModeRules> modes = new List<GameModeRules>();
        [SerializeField] private string gameSceneName = "Greybox";

        private readonly SaveData[] slots = new SaveData[SaveFile.SlotCount];
        private Font font;
        private RectTransform content;
        private Text heading;
        private Text message;
        private InputField nameField;
        private OptionsPanel options;
        private GameModeRules chosenMode;
        private DifficultyProfile chosenDifficulty;
        private string typedName = string.Empty;
        private int confirmSlot = -1;

        private void Awake()
        {
            // Se puede llegar aquí desde una partida en pausa.
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameSettings.Apply();
            SaveFile.MigrateLegacy();

            Build();
            ShowPage(Page.Root);
        }

        // En Start: el EventSystem se registra como actual en su OnEnable.
        private void Start() => UiModal.EnsureUiInput();

        // ---------- páginas

        private void ShowPage(Page page)
        {
            // Lo que se estuviera escribiendo se conserva al cambiar de página.
            if (nameField != null) typedName = nameField.text;
            nameField = null;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                GameObject child = content.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            message.text = string.Empty;
            switch (page)
            {
                case Page.Play: BuildPlayPage(); break;
                case Page.Difficulty: BuildDifficultyPage(); break;
                case Page.NewSlot: BuildSlotPage(false); break;
                case Page.LoadSlot: BuildSlotPage(true); break;
                default: BuildRootPage(); break;
            }
        }

        private void BuildRootPage()
        {
            heading.text = string.Empty;
            AddButton("Jugar", 0, () => ShowPage(Page.Play));
            AddButton("Opciones", 1, () => options.Show());
            AddButton("Salir", 2, Quit);
        }

        private void BuildPlayPage()
        {
            heading.text = "Jugar";
            ReadSlots();

            int row = 0;
            foreach (GameModeRules mode in modes)
            {
                if (mode == null) continue;
                GameModeRules picked = mode;
                AddButton(mode.DisplayName, row++, () =>
                {
                    chosenMode = picked;
                    ShowPage(Page.Difficulty);
                });
            }

            Button load = AddButton("Cargar partida", row++, () => ShowPage(Page.LoadSlot));
            load.interactable = System.Array.Exists(slots, slot => slot != null);
            AddButton("Volver", row++, () => ShowPage(Page.Root));

            var lines = new System.Text.StringBuilder();
            foreach (GameModeRules mode in modes)
            {
                if (mode != null) lines.AppendLine($"{mode.DisplayName}: {DescribeMode(mode)}");
            }

            AddNote(lines.ToString(), row);
        }

        private void BuildDifficultyPage()
        {
            heading.text = chosenMode != null ? $"{chosenMode.DisplayName}  ·  Dificultad" : "Dificultad";

            int row = 0;
            foreach (DifficultyProfile difficulty in difficulties)
            {
                if (difficulty == null) continue;
                DifficultyProfile picked = difficulty;
                AddButton(difficulty.DisplayName, row++, () =>
                {
                    chosenDifficulty = picked;
                    confirmSlot = -1;
                    ShowPage(Page.NewSlot);
                });
            }

            AddButton("Volver", row++, () => ShowPage(Page.Play));

            var lines = new System.Text.StringBuilder();
            foreach (DifficultyProfile difficulty in difficulties)
            {
                if (difficulty != null) lines.AppendLine($"{difficulty.DisplayName}: {difficulty.FlavorText}");
            }

            AddNote(lines.ToString(), row);
        }

        private void BuildSlotPage(bool loading)
        {
            ReadSlots();
            int row = 0;

            if (loading)
            {
                heading.text = "Cargar partida";
            }
            else
            {
                heading.text = "Elige dónde guardar";
                nameField = AddNameField(row++);
                nameField.text = typedName;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                int slot = i;
                string label = SaveSummary.Describe(slot, slots[slot]);
                if (!loading && confirmSlot == slot)
                {
                    label = $"Ranura {slot + 1}: se sobrescribirá y se perderá esa partida. Pulsa otra vez";
                }

                Button button = AddButton(label, row++, () =>
                {
                    if (loading) LoadGame(slot);
                    else PickSlotForNewGame(slot);
                }, true);
                if (loading) button.interactable = slots[slot] != null;
            }

            AddButton("Volver", row++, () =>
            {
                confirmSlot = -1;
                ShowPage(loading ? Page.Play : Page.Difficulty);
            });

            if (!loading)
            {
                AddNote("La partida se guarda sola en su ranura cada amanecer.\nHay cinco ranuras en total, para todos los modos.", row);
            }
        }

        // ---------- acciones

        private void PickSlotForNewGame(int slot)
        {
            // Ranura ocupada: se avisa y hace falta un segundo clic sobre la misma.
            if (slots[slot] != null && confirmSlot != slot)
            {
                confirmSlot = slot;
                ShowPage(Page.NewSlot);
                return;
            }

            if (session == null || !CanLoadGameScene())
            {
                return;
            }

            session.Select(chosenDifficulty != null ? chosenDifficulty : First(difficulties),
                chosenMode != null ? chosenMode : First(modes));
            session.SaveSlot = slot;
            session.SaveName = SaveSummary.CleanName(nameField != null ? nameField.text : typedName, slot);
            session.ContinueFromSave = false;

            // La partida anterior de esa ranura se borra ya: la nueva guardará en su primer amanecer.
            SaveFile.Delete(slot);
            SceneManager.LoadScene(gameSceneName);
        }

        private void LoadGame(int slot)
        {
            SaveData save = slots[slot];
            if (save == null || session == null || !CanLoadGameScene())
            {
                return;
            }

            // La partida sigue con la dificultad y el modo con los que se guardó.
            DifficultyProfile difficulty = difficulties.Find(d => d != null && d.DisplayName == save.difficulty);
            GameModeRules mode = modes.Find(m => m != null && m.DisplayName == save.mode);
            session.Select(difficulty != null ? difficulty : First(difficulties), mode != null ? mode : First(modes));
            session.SaveSlot = slot;
            session.SaveName = save.name;
            session.ContinueFromSave = true;
            SceneManager.LoadScene(gameSceneName);
        }

        private void ReadSlots()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                SaveFile.TryRead(i, out slots[i]);
            }
        }

        private bool CanLoadGameScene()
        {
            if (Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                return true;
            }

            message.text = "Falta la escena de juego: vuelve a generar las escenas.";
            return false;
        }

        private void Quit()
        {
            // En el editor Application.Quit no hace nada.
            if (Application.isEditor)
            {
                message.text = "En el editor, detén el Play para salir.";
                return;
            }

            Application.Quit();
        }

        private static string DescribeMode(GameModeRules mode)
        {
            if (mode.IsOneLife)
            {
                return $"{mode.NightsToWin} noches con 1 de vida, tú y la caravana. Si pierdes, la partida se borra.";
            }

            return mode.IsEndless ? "sin final: aguanta todas las noches que puedas." : $"sobrevive {mode.NightsToWin} noches.";
        }

        private static T First<T>(List<T> list) where T : class => list.Count > 0 ? list[0] : null;

        // ---------- montaje

        private void Build()
        {
            font = MenuUi.LoadFont();
            Canvas canvas = MenuUi.CreateCanvas("MainMenu", transform, 0);

            // Panel a la izquierda, de arriba abajo; el resto de la pantalla es la escena de fondo.
            Image panel = UiFactory.CreateImage("Panel", canvas.transform, new Color(0.04f, 0.06f, 0.05f, 0.9f), true);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(PanelWidth, 0f);
            Transform root = panel.transform;

            Text title = UiFactory.CreateText("Title", root, font, 84, TextAnchor.MiddleCenter);
            title.text = "DEAD ACRES";
            title.color = new Color(0.85f, 0.92f, 0.70f);
            UiFactory.Place(title.rectTransform, UiFactory.TopCenter, UiFactory.TopCenter, new Vector2(0f, -70f), new Vector2(PanelWidth, 100f));

            Text subtitle = UiFactory.CreateText("Subtitle", root, font, 24, TextAnchor.MiddleCenter);
            subtitle.text = "Granja de día. Zombis de noche.";
            subtitle.color = new Color(0.75f, 0.75f, 0.70f);
            UiFactory.Place(subtitle.rectTransform, UiFactory.TopCenter, UiFactory.TopCenter, new Vector2(0f, -170f), new Vector2(PanelWidth, 36f));

            heading = UiFactory.CreateText("Heading", root, font, 30, TextAnchor.MiddleCenter);
            heading.color = new Color(1f, 0.86f, 0.35f);
            UiFactory.Place(heading.rectTransform, UiFactory.TopCenter, UiFactory.TopCenter, new Vector2(0f, -236f), new Vector2(PanelWidth, 40f));

            content = UiFactory.CreateRect("Content", root);
            UiFactory.Place(content, UiFactory.TopCenter, UiFactory.TopCenter, new Vector2(0f, -300f), new Vector2(PanelWidth, 700f));

            message = UiFactory.CreateText("Message", root, font, 20, TextAnchor.MiddleCenter);
            message.color = new Color(1f, 0.6f, 0.45f);
            UiFactory.Place(message.rectTransform, UiFactory.BottomCenter, UiFactory.BottomCenter, new Vector2(0f, 30f), new Vector2(PanelWidth - 40f, 30f));

            options = OptionsPanel.Create(canvas.transform, font);
        }

        private Button AddButton(string label, int row, UnityEngine.Events.UnityAction onClick, bool wide = false)
        {
            Button button = MenuUi.AddButton(content, font, label, row, 0f);
            if (wide)
            {
                var rect = button.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(WideButton, rect.sizeDelta.y);
                var text = button.GetComponentInChildren<Text>();
                if (text != null) text.fontSize = 18;
            }

            button.onClick.AddListener(onClick);
            return button;
        }

        private void AddNote(string text, int row)
        {
            Text note = UiFactory.CreateText("Note", content, font, 19, TextAnchor.UpperCenter);
            note.text = text;
            note.color = new Color(0.82f, 0.82f, 0.78f);
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Place(note.rectTransform, UiFactory.TopCenter, UiFactory.TopCenter,
                new Vector2(0f, -row * (MenuUi.ButtonHeight + MenuUi.ButtonSpacing) - 12f), new Vector2(WideButton, 220f));
        }

        private InputField AddNameField(int row)
        {
            Image background = UiFactory.CreateImage("NameField", content, new Color(1f, 1f, 1f, 0.14f), true);
            UiFactory.Place(background.rectTransform, UiFactory.TopCenter, UiFactory.TopCenter,
                new Vector2(0f, -row * (MenuUi.ButtonHeight + MenuUi.ButtonSpacing)), new Vector2(WideButton, MenuUi.ButtonHeight));

            Text text = UiFactory.CreateText("Text", background.transform, font, 22, TextAnchor.MiddleLeft, false);
            text.supportRichText = false;
            UiFactory.Stretch(text.rectTransform, 12f);

            Text placeholder = UiFactory.CreateText("Placeholder", background.transform, font, 22, TextAnchor.MiddleLeft, false);
            placeholder.text = "Nombre de la partida (opcional)";
            placeholder.color = new Color(1f, 1f, 1f, 0.45f);
            UiFactory.Stretch(placeholder.rectTransform, 12f);

            var field = background.gameObject.AddComponent<InputField>();
            field.targetGraphic = background;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.characterLimit = SaveSummary.MaxNameLength;
            field.lineType = InputField.LineType.SingleLine;
            return field;
        }

        internal void Configure(GameSession gameSession, List<DifficultyProfile> difficultyProfiles,
            List<GameModeRules> gameModes)
        {
            session = gameSession;
            difficulties = difficultyProfiles;
            modes = gameModes;
        }
    }
}
