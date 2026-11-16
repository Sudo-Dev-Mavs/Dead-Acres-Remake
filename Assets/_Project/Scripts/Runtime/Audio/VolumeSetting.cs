using UnityEngine;

namespace DeadAcres.Audio
{
    /// <summary>Volumen general, recordado entre sesiones en las preferencias del jugador.</summary>
    public static class VolumeSetting
    {
        private const string Key = "dead_acres.volume";

        public static float Current => Mathf.Clamp01(PlayerPrefs.GetFloat(Key, 1f));

        /// <summary>Aplica el volumen guardado. Llamar al arrancar cada escena.</summary>
        public static void Apply() => AudioListener.volume = Current;

        /// <returns>El volumen nuevo.</returns>
        public static float Cycle()
        {
            float next = VolumeSteps.Next(Current);
            PlayerPrefs.SetFloat(Key, next);
            PlayerPrefs.Save();
            AudioListener.volume = next;
            return next;
        }
    }
}
