using System;
using UnityEngine;

namespace DeadAcres.Settings
{
    /// <summary>
    /// Opciones del jugador, guardadas en sus preferencias (no en la partida): sonido, ratón y
    /// mando, cámara y pantalla. Quien las use se suscribe a <see cref="Changed"/>.
    /// </summary>
    public static class GameSettings
    {
        public static readonly float[] SensitivitySteps = { 0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f, 3f };
        public static readonly float[] FieldOfViewSteps = { 60f, 70f, 80f, 90f, 100f };
        public static readonly float[] FrameLimitSteps = { 0f, 30f, 60f, 120f, 144f, 240f };

        private const string Prefix = "dead_acres.";

        /// <summary>Cambió alguna opción.</summary>
        public static event Action Changed;

        public static float MasterVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "volume", 1f));
            set => Set("volume", Mathf.Clamp01(value));
        }

        public static float EffectsVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "volume_effects", 1f));
            set => Set("volume_effects", Mathf.Clamp01(value));
        }

        public static float MusicVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "volume_music", 1f));
            set => Set("volume_music", Mathf.Clamp01(value));
        }

        /// <summary>Multiplicador sobre la sensibilidad base del ratón.</summary>
        public static float MouseSensitivity
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "mouse_sensitivity", 1f), 0.1f, 5f);
            set => Set("mouse_sensitivity", Mathf.Clamp(value, 0.1f, 5f));
        }

        /// <summary>Multiplicador sobre la velocidad de giro con el stick.</summary>
        public static float StickSensitivity
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "stick_sensitivity", 1f), 0.1f, 5f);
            set => Set("stick_sensitivity", Mathf.Clamp(value, 0.1f, 5f));
        }

        public static bool InvertY
        {
            get => PlayerPrefs.GetInt(Prefix + "invert_y", 0) != 0;
            set => Set("invert_y", value);
        }

        public static float FieldOfView
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "fov", 60f), 50f, 110f);
            set => Set("fov", Mathf.Clamp(value, 50f, 110f));
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(Prefix + "fullscreen", 1) != 0;
            set => Set("fullscreen", value);
        }

        public static bool VSync
        {
            get => PlayerPrefs.GetInt(Prefix + "vsync", 1) != 0;
            set => Set("vsync", value);
        }

        /// <summary>Fotogramas por segundo como mucho. 0 = sin límite. Solo cuenta sin sincronía vertical.</summary>
        public static int FrameLimit
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "frame_limit", 0));
            set => Set("frame_limit", Mathf.Max(0, value));
        }

        /// <summary>Ancho guardado de la resolución. 0 = la que tenga la pantalla.</summary>
        public static int ResolutionWidth => PlayerPrefs.GetInt(Prefix + "res_w", 0);
        public static int ResolutionHeight => PlayerPrefs.GetInt(Prefix + "res_h", 0);

        public static void SetResolution(int width, int height)
        {
            PlayerPrefs.SetInt(Prefix + "res_w", Mathf.Max(0, width));
            PlayerPrefs.SetInt(Prefix + "res_h", Mathf.Max(0, height));
            Commit();
        }

        /// <summary>Aplica sonido y pantalla. Llamar al arrancar cada escena.</summary>
        public static void Apply()
        {
            AudioListener.volume = MasterVolume;
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = FrameLimit > 0 ? FrameLimit : -1;

            // En el editor la ventana Game manda: no se toca.
            if (Application.isEditor)
            {
                return;
            }

            int width = ResolutionWidth;
            int height = ResolutionHeight;
            bool fullscreen = Fullscreen;
            if (width > 0 && height > 0)
            {
                if (Screen.width != width || Screen.height != height || Screen.fullScreen != fullscreen)
                {
                    Screen.SetResolution(width, height, fullscreen);
                }
            }
            else if (Screen.fullScreen != fullscreen)
            {
                Screen.fullScreen = fullscreen;
            }
        }

        private static void Set(string key, float value)
        {
            PlayerPrefs.SetFloat(Prefix + key, value);
            Commit();
        }

        private static void Set(string key, int value)
        {
            PlayerPrefs.SetInt(Prefix + key, value);
            Commit();
        }

        private static void Set(string key, bool value) => Set(key, value ? 1 : 0);

        private static void Commit()
        {
            PlayerPrefs.Save();
            Apply();
            Changed?.Invoke();
        }
    }
}
