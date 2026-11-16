using System;

namespace DeadAcres.Audio
{
    /// <summary>Los pasos del botón de volumen de los menús: 100, 75, 50, 25, 0 y vuelta. C# puro.</summary>
    public static class VolumeSteps
    {
        private static readonly float[] Steps = { 1f, 0.75f, 0.5f, 0.25f, 0f };

        /// <summary>El paso siguiente al más cercano a <paramref name="current"/>.</summary>
        public static float Next(float current)
        {
            int nearest = 0;
            for (int i = 1; i < Steps.Length; i++)
            {
                if (Math.Abs(Steps[i] - current) < Math.Abs(Steps[nearest] - current))
                {
                    nearest = i;
                }
            }

            return Steps[(nearest + 1) % Steps.Length];
        }

        public static string Format(float volume)
        {
            int percent = (int)Math.Round(Math.Min(1f, Math.Max(0f, volume)) * 100f);
            return percent == 0 ? "Volumen: silencio" : $"Volumen: {percent} %";
        }
    }
}
