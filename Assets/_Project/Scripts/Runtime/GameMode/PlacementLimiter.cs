using System;

namespace DeadAcres.GameMode
{
    /// <summary>
    /// Cuenta plantas y objetos colocados contra los límites del modo.
    /// En co-op el contador es compartido (como en el original) y vive en el host.
    /// </summary>
    public sealed class PlacementLimiter
    {
        private readonly GameModeRules rules;
        private readonly int[] counts = new int[GameModeRules.CategoryCount];

        public PlacementLimiter(GameModeRules rules)
        {
            this.rules = rules ? rules : throw new ArgumentNullException(nameof(rules));
        }

        /// <summary>(categoría, cantidad actual, límite). Para el HUD "87/100".</summary>
        public event Action<PlaceableCategory, int, int> CountChanged;

        public int GetCount(PlaceableCategory category) => counts[(int)category];

        public int GetLimit(PlaceableCategory category) => rules.GetLimit(category);

        public bool CanPlace(PlaceableCategory category) => counts[(int)category] < rules.GetLimit(category);

        /// <summary>Reserva un hueco. Llamar solo cuando la colocación se confirma.</summary>
        public bool TryRegister(PlaceableCategory category)
        {
            if (!CanPlace(category))
            {
                return false;
            }

            counts[(int)category]++;
            Notify(category);
            return true;
        }

        /// <summary>Libera un hueco (cosechado, destruido o quitado con el martillo).</summary>
        public void Unregister(PlaceableCategory category)
        {
            int index = (int)category;
            if (counts[index] == 0)
            {
                return;
            }

            counts[index]--;
            Notify(category);
        }

        private void Notify(PlaceableCategory category) =>
            CountChanged?.Invoke(category, counts[(int)category], rules.GetLimit(category));
    }
}
