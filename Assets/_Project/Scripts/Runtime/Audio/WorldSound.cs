using DeadAcres.Settings;
using UnityEngine;

namespace DeadAcres.Audio
{
    /// <summary>
    /// Sonido con posición en el mundo. Lo usan los objetos que suenan por sí mismos (torretas,
    /// TNT) con su propio AudioSource, y GameAudio para lo que ocurre lejos del jugador.
    /// </summary>
    public static class WorldSound
    {
        /// <summary>Deja un AudioSource preparado para sonar en 3D: se apaga con la distancia.</summary>
        public static void Configure(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 4f;
            source.maxDistance = 55f;
            source.dopplerLevel = 0f;
        }

        /// <summary>Reproduce un clip en ese AudioSource respetando el volumen de efectos de las opciones.</summary>
        public static void Play(AudioSource source, AudioClip clip, float volumeScale = 1f)
        {
            if (source != null && clip != null)
            {
                source.PlayOneShot(clip, GameSettings.EffectsVolume * volumeScale);
            }
        }
    }
}
