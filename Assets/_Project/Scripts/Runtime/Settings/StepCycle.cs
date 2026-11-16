using System;
using System.Collections.Generic;

namespace DeadAcres.Settings
{
    /// <summary>Botones de opciones que pasan por una lista fija de valores y vuelven al principio. C# puro.</summary>
    public static class StepCycle
    {
        /// <summary>El valor siguiente al más cercano a <paramref name="current"/>.</summary>
        public static float Next(IReadOnlyList<float> steps, float current)
        {
            if (steps == null || steps.Count == 0)
            {
                return current;
            }

            return steps[(IndexOfNearest(steps, current) + 1) % steps.Count];
        }

        public static int IndexOfNearest(IReadOnlyList<float> steps, float current)
        {
            int nearest = 0;
            for (int i = 1; i < steps.Count; i++)
            {
                if (Math.Abs(steps[i] - current) < Math.Abs(steps[nearest] - current))
                {
                    nearest = i;
                }
            }

            return nearest;
        }
    }
}
