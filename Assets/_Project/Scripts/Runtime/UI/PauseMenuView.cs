using DeadAcres.Settings;
using DeadAcres.DayNight;
using DeadAcres.Match;
using DeadAcres.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeadAcres.UI
{
    /// <summary>
    /// Menú de pausa (Esc o P): detiene el juego entero, no solo el reloj. Monta su propia
    /// ventana por código. Con la partida terminada también se abre, para poder volver al menú.
    /// </summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader playerInput;
        [SerializeField] private DayNightCycle dayNight;
        [SerializeField] private MatchDirector match;
        [SerializeField] private string menuSceneName = "MainMenu";

        private GameObject panel;
        private Text message;
        private OptionsPanel options;
        private bool inputWasEnabled;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            GameSettings.Apply();
            BuildPanel();
        }

        private void OnDisable() => Close();

        // Por si se destruye con el juego parado (cambio de escena).
        private void OnDestroy() => Time.timeScale = 1f;

        private void Update()
        {
            // Mientras las opciones esperan una tecla para asignarla, esa pulsación es suya.
            if (options != null && options.IsCapturingInput)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            bool pressed = (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame))
                           || (gamepad != null && gamepad.startButton.wasPressedThisFrame);
            if (!pressed)
            {
                return;
            }

            // Con las opciones abiertas, la tecla de pausa solo las cierra.
            if (options != null && options.IsOpen)
            {
                options.Hide();
                return;
            }

            if (IsOpen)
            {
                Close();
                return;
            }

            // Con la tienda o el inventario abiertos el input del jugador está apagado: ese Esc es
            // suyo. Se mira el fotograma anterior porque pueden cerrarse en este mismo.
            bool matchOver = match != null && match.State.IsOver;
            bool noOtherWindow = playerInput != null && playerInput.enabled && inputWasEnabled;
            if (matchOver || noOtherWindow)
            {
                Open();
            }
        }

        private void LateUpdate() => inputWasEnabled = playerInput != null && playerInput.enabled;

        public void Open()
        {
            if (IsOpen || panel == null)
            {
                return;
            }

            IsOpen = true;
            UiModal.Enter(playerInput, dayNight);
            Time.timeScale = 0f;
            message.text = string.Empty;
            panel.SetActive(true);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            Time.timeScale = 1f;
            if (options != null) options.Hide();
            if (panel != null) panel.SetActive(false);

            // Con la partida terminada el input sigue apagado: lo decide MatchDirector.
            if (match != null && match.State.IsOver)
            {
                return;
            }

            UiModal.Exit(playerInput, dayNight);
        }

        private void OpenOptions()
        {
            if (options != null) options.Show();
        }

        private void GoToMenu()
        {
            if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
            {
                message.text = "Falta la escena del menú: vuelve a generar las escenas.";
                return;
            }

            // Se sale sin pasar por Close: no hay que devolver el control al jugador ni
            // bloquear el cursor justo antes de entrar al menú.
            IsOpen = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(menuSceneName);
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

        private void BuildPanel()
        {
            Font font = MenuUi.LoadFont();
            Canvas canvas = MenuUi.CreateCanvas("PauseMenu", transform, 100);

            Image dim = UiFactory.CreateImage("Dim", canvas.transform, new Color(0f, 0f, 0f, 0.7f), true);
            UiFactory.Stretch(dim.rectTransform);
            panel = dim.gameObject;

            Image window = UiFactory.CreateImage("Window", dim.transform, new Color(0.12f, 0.13f, 0.15f, 0.97f), true);
            UiFactory.Place(window.rectTransform, UiFactory.Center, UiFactory.Center, Vector2.zero, new Vector2(620f, 530f));

            Text title = UiFactory.CreateText("Title", window.transform, font, 40, TextAnchor.MiddleCenter, false);
            title.text = "Pausa";
            UiFactory.Place(title.rectTransform, UiFactory.TopCenter, UiFactory.TopCenter, new Vector2(0f, -24f), new Vector2(560f, 50f));

            Text note = UiFactory.CreateText("Note", window.transform, font, 18, TextAnchor.MiddleCenter, false);
            note.color = new Color(0.8f, 0.8f, 0.8f);
            note.text = "La partida se guarda sola cada amanecer.\nSi sales ahora, continuarás desde el último.";
            UiFactory.Place(note.rectTransform, UiFactory.TopCenter, UiFactory.TopCenter, new Vector2(0f, -84f), new Vector2(560f, 50f));

            const float top = -160f;
            MenuUi.AddButton(window.transform, font, "Continuar  [Esc]", 0, top).onClick.AddListener(Close);
            MenuUi.AddButton(window.transform, font, "Opciones", 1, top).onClick.AddListener(OpenOptions);
            MenuUi.AddButton(window.transform, font, "Menú principal", 2, top).onClick.AddListener(GoToMenu);
            MenuUi.AddButton(window.transform, font, "Salir del juego", 3, top).onClick.AddListener(Quit);

            message = UiFactory.CreateText("Message", window.transform, font, 18, TextAnchor.MiddleCenter, false);
            message.color = new Color(1f, 0.6f, 0.45f);
            UiFactory.Place(message.rectTransform, UiFactory.BottomCenter, UiFactory.BottomCenter, new Vector2(0f, 18f), new Vector2(580f, 30f));

            // La ventana de opciones va encima de la de pausa, en el mismo canvas.
            options = OptionsPanel.Create(canvas.transform, font);
            panel.SetActive(false);
        }

        internal void Configure(PlayerInputReader input, DayNightCycle cycle, MatchDirector matchDirector)
        {
            playerInput = input;
            dayNight = cycle;
            match = matchDirector;
        }
    }
}
