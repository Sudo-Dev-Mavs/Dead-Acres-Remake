using UnityEngine;

namespace DeadAcres.Items
{
    /// <summary>
    /// Rareza de un ítem: cuánto beneficia al jugador. Decide con qué frecuencia lo trae la
    /// tienda. El orden importa: de menos a más raro.
    /// </summary>
    public enum ItemRarity : byte
    {
        Common = 0,
        Rare = 1,
        UltraRare = 2,
        Epic = 3,
        Legendary = 4,
    }

    public static class ItemRarityInfo
    {
        public const int Count = 5;

        public static string GetName(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare: return "Raro";
                case ItemRarity.UltraRare: return "Ultrararo";
                case ItemRarity.Epic: return "Épico";
                case ItemRarity.Legendary: return "Legendario";
                default: return "Común";
            }
        }

        public static Color GetColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare: return new Color(0.35f, 0.65f, 1f);
                case ItemRarity.UltraRare: return new Color(0.45f, 0.90f, 0.55f);
                case ItemRarity.Epic: return new Color(0.78f, 0.45f, 1f);
                case ItemRarity.Legendary: return new Color(1f, 0.72f, 0.20f);
                default: return new Color(0.78f, 0.78f, 0.78f);
            }
        }
    }
}
