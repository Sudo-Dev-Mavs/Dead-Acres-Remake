using System.Collections.Generic;
using DeadAcres.Settings;
using DeadAcres.Core.Session;
using DeadAcres.Difficulty;
using DeadAcres.GameMode;
using DeadAcres.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeadAcres.UI
{
    /// <summary>
    /// Menú principal: continuar la partida guardada o empezar una nueva eligiendo dificultad y
    /// modo. Monta su interfaz por código y deja la elección en la <see cref="GameSession"/>.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private List<DifficultyProfile> difficulties = new List<DifficultyProfile>();
        [SerializeField] private List<GameModeRules> modes = new List<GameModeRules>();
        [SerializeField] private string gameSceneName = "Greybox";

        private int difficultyIndex;
        private int modeIndex;
        private SaveData save;
        private bool confirmNewGame;

        private Button continueButton;
        private Button newGameButton;
        private Button difficultyButton;
        private Button modeButton;
        private OptionsPanel options;
        private Text summary;
        private Text message;

        private void Awake()
        {
            // Se puede llegar aquí desde una partida en pausa.
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameSettings.Apply();

            difficultyIndex = Mathf.Max(0, session != null ? difficulties.IndexOf(session.Difficulty) : 0);
            modeIndex = Mathf.Max(0, session != null ? modes.IndexOf(session.Mode) : 0);
            SaveFile.TryRead(out save);

            Build();
            Refresh();
        }

        // En Start: el EventSystem se registra como actual en su OnEnable.
        private void Start() => UiModal.EnsureUiInput();

        private void Continue()
        {
            if (save == null || session == null)
            {
                return;
            }

            // La partida sigue con la dificultad y el modo con los que se guardó.
            DifficultyProfile difficulty = difficulties.Find(d => d != null && d.DisplayName == save.difficulty);
            GameModeRules mode = modes.Find(m => m != null && m.DisplayName == save.mode);
            session.Select(difficulty != null ? difficulty : Selected(difficulties, difficultyIndex),
                mode != null ? mode : Selected(modes, modeIndex));
            session.ContinueFromSave = true;
            LoadGame();
        }

        private void NewGame()
        {
            if (session == null)
            {
                return;
            }

            // Empezar de cero borra la partida guardada: se pide un segundo clic.
            if (save != null && !confirmNewGame)
            {
                confirmNewGame = true;
                Refresh();
                return;
            }

            session.Select(Selected(difficulties, difficultyIndex), Selected(modes, modeIndex));
            session.ContinueFromSave = false;
            if (!CanLoadGame())
            {
                return;
            }

            SaveFile.Delete();
            SceneManager.LoadScene(gameSceneName);
        }

        private void LoadGame()
        {
            if (CanLoadGame())
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }

        private bool CanLoadGame()
        {
            if (Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                return true;
            }

            message.text = "Falta la escena de juego: vuelve a generar las escenas.";
            return false;
        }

        private void NextDifficulty()
        {
            if (difficulties.Count > 0) difficultyIndex = (difficultyIndex + 1) % difficulties.Count;
            confirmNewGame = false;
            Refresh();
        }

        private void OpenOptions()
        {
            if (options != null) options.Show();
        }

        private void NextMode()
        {
            if (modes.Count > 0) modeIndex = (modeIndex + 1) % modes.Count;
            confirmNewGame = false;
            Refresh();
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

        private static T Selected<T>(List<T> list, int index) where T : class
        {
            return list.Count > 0 ? list[Mathf.Clamp(index, 0, list.Count - 1)] : null;
        }

        private void Refresh()
        {
            continueButton.interactable = save != null;
            MenuUi.SetLabel(continueButton, save != null
                ? $"Continuar  ·  Día {save.dayNumber}  ({save.difficulty}, {save.mode})"
                : "Continuar  (no hay partida guardada)");

            MenuUi.SetLabel(newGameButton, confirmNewGame
                ? "Se borrará la partida guardada. Pulsa otra vez"
                : "Nueva partida");

            DifficultyProfile difficulty = Selected(difficulties, difficultyIndex);
            GameModeRules mode = Selected(modes, modeIndex);
            MenuUi.SetLabel(difficultyButton, $"Dificultad: {(difficulty != null ? difficulty.DisplayName : "-")}");
            MenuUi.SetLabel(modeButton, $"Modo: {(mode != null ? mode.DisplayName : "-")}");

            string modeLine = mode == null
                ? string.Empty
                : mode.IsEndless ? "Sin final: aguanta todas las noches que puedas." : $"Sobrevive {mode.NightsToWin} noches.";
            summary.text = difficulty != null ? $"{difficulty.FlavorText}\n{modeLine}" : modeLine;
        }

        private void Build()
        {
            Font font = MenuUi.LoadFont();
            Canvas canvas = MenuUi.CreateCanvas("MainMenu", transform, 0);

            Image background = UiFactory.CreateImage("Background", canvas.transform, new Color(0.06f, 0.09f, 0.07f, 1f), true);
            UiFactory.Stretch(background.rectTransform);
            Transform root = background.transform;

            Text title = UiFactory.CreateText("Title", root, font, 96, TextAnchor.MiddleCenter);
            title.text = "DEAD ACRES";
            title.color = new Color(0.85f, 0.92f, 0.70f);
            UiFactory.Place(title.rectTransform, UiFactory.TopCenter, UiFactory.TopCenter, new Vector2(0f, -110f), new Vector2(1200f, 120f));

            Text subtitle = UiFactory.CreateText("Subtitle", root, font, 26, TextAnchor.MiddleCenter);
            subtitle.text = "Granja de día. Zombis de noche.";
            subtitle.color = new Color(0.75f, 0.75f, 0.70f);
            UiFactory.Place(subtitle.rectTransform, UiFactory.TopCenter, UiFactory.TopCenter, new Vector2(0f, -230f), new Vector2(1200f, 40f));

            const float top = -330f;
            continueButton = MenuUi.AddButton(root, font, "Continuar", 0, top);
            continueButton.onClick.AddListener(Continue);
            newGameButton = MenuUi.AddButton(root, font, "Nueva partida", 1, top);
            newGameButton.onClick.AddListener(NewGame);
            difficultyButton = MenuUi.AddButton(root, font, "Dificultad", 2, top);
            difficultyButton.onClick.AddListener(NextDifficulty);
            modeButton = MenuUi.AddButton(root, font, "Modo", 3, top);
            modeButton.onClick.AddListener(NextMode);
            MenuUi.AddButton(root, font, "Opciones", 4, top).onClick.AddListener(OpenOptions);
            MenuUi.AddButton(root, font, "Salir", 5, top).onClick.AddListener(Quit);

            // Las etiquetas largas (continuar, confirmar) necesitan más ancho que el resto.
            foreach (Button wide in new[] { continueButton, newGameButton })
            {
                var rect = wide.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(760f, rect.sizeDelta.y);
            }

            summary = UiFactory.CreateText("Summary", root, font, 22, TextAnchor.UpperCenter);
            summary.color = new Color(0.85f, 0.85f, 0.80f);
            UiFactory.Place(summary.rectTransform, UiFactory.TopCenter, UiFactory.TopCenter,
                new Vector2(0f, top - 6 * (MenuUi.ButtonHeight + MenuUi.ButtonSpacing) - 10f), new Vector2(1200f, 70f));

            message = UiFactory.CreateText("Message", root, font, 20, TextAnchor.MiddleCenter);
            message.color = new Color(1f, 0.6f, 0.45f);
            UiFactory.Place(message.rectTransform, UiFactory.BottomCenter, UiFactory.BottomCenter, new Vector2(0f, 40f), new Vector2(1200f, 30f));

            options = OptionsPanel.Create(canvas.transform, font);
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
