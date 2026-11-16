using System;
using DeadAcres.Settings;

namespace DeadAcres.Audio
{
    /// <summary>Los pasos de los botones de volumen: 100, 75, 50, 25, 0 y vuelta. C# puro.</summary>
    public static class VolumeSteps
    {
        private static readonly float[] Steps = { 1f, 0.75f, 0.5f, 0.25f, 0f };

        /// <summary>El paso siguiente al más cercano a <paramref name="current"/>.</summary>
        public static float Next(float current) => StepCycle.Next(Steps, current);

        public static string Format(float volume) => Format("Volumen", volume);

        public static string Format(string label, float volume)
        {
            int percent = (int)Math.Round(Math.Min(1f, Math.Max(0f, volume)) * 100f);
            return percent == 0 ? $"{label}: silencio" : $"{label}: {percent} %";
        }
    }
}
