namespace DeadAcres.Saving
{
    /// <summary>Texto con el que se enseña una ranura en el menú. C# puro.</summary>
    public static class SaveSummary
    {
        public const int MaxNameLength = 24;

        public static string Describe(int slot, SaveData data)
        {
            if (data == null)
            {
                return $"Ranura {slot + 1}  ·  vacía";
            }

            string name = string.IsNullOrWhiteSpace(data.name) ? $"Partida {slot + 1}" : data.name.Trim();
            return $"Ranura {slot + 1}  ·  {name}  ·  Día {data.dayNumber}  ·  {data.mode}, {data.difficulty}";
        }

        /// <summary>Nombre limpio para guardar: sin espacios sobrantes, con un tope de longitud y nunca vacío.</summary>
        public static string CleanName(string typed, int slot)
        {
            string name = typed == null ? string.Empty : typed.Trim();
            if (name.Length == 0)
            {
                return $"Partida {slot + 1}";
            }

            return name.Length > MaxNameLength ? name.Substring(0, MaxNameLength).TrimEnd() : name;
        }
    }
}
