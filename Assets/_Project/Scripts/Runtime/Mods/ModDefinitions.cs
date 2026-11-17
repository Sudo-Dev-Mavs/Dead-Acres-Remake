using System;
using DeadAcres.Items;

namespace DeadAcres.Mods
{
    /// <summary>mod.json: la ficha de un mod. Va en la raíz de su carpeta.</summary>
    [Serializable]
    public sealed class ModManifest
    {
        /// <summary>Identificador único, sin espacios. Si falta, se usa el nombre de la carpeta.</summary>
        public string id;
        public string name;
        public string version;
        public string author;
        public string description;
    }

    /// <summary>Weapons/*.json: un arma nueva. Los valores que falten quedan como los de una pistola.</summary>
    [Serializable]
    public sealed class ModWeapon
    {
        public string id;
        public string name;

        /// <summary>common, rare, ultrarare, epic o legendary.</summary>
        public string rarity = "common";
        public int price = 200;
        public float damage = 20f;
        public float range = 60f;
        public float fireInterval = 0.25f;
        public bool automatic;
        public int pellets = 1;
        public float spread;
        public int magazine = 12;
        public float reloadSeconds = 1.5f;
    }

    /// <summary>Reglas para dar por bueno lo que trae un mod. C# puro.</summary>
    public static class ModRules
    {
        /// <summary>Id con el que el arma entra al juego: lleva el del mod delante para no chocar con nada.</summary>
        public static string BuildItemId(string modId, string weaponId)
        {
            return $"mod.{Slug(modId)}.{Slug(weaponId)}";
        }

        /// <summary>Solo letras, números y guion bajo, en minúsculas.</summary>
        public static string Slug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "sin_id";
            }

            var builder = new System.Text.StringBuilder(text.Length);
            foreach (char c in text.Trim().ToLowerInvariant())
            {
                builder.Append(char.IsLetterOrDigit(c) ? c : '_');
            }

            return builder.ToString();
        }

        public static ItemRarity ParseRarity(string text)
        {
            switch (Slug(text))
            {
                case "rare":
                case "raro":
                    return ItemRarity.Rare;
                case "ultrarare":
                case "ultra_rare":
                case "ultrararo":
                    return ItemRarity.UltraRare;
                case "epic":
                case "epico":
                case "épico":
                    return ItemRarity.Epic;
                case "legendary":
                case "legendario":
                    return ItemRarity.Legendary;
                default:
                    return ItemRarity.Common;
            }
        }

        /// <returns>Null si el arma vale; si no, por qué se descarta.</returns>
        public static string Validate(ModWeapon weapon)
        {
            if (weapon == null) return "el archivo está vacío o no es JSON válido";
            if (string.IsNullOrWhiteSpace(weapon.id)) return "falta \"id\"";
            if (string.IsNullOrWhiteSpace(weapon.name)) return "falta \"name\"";
            if (weapon.damage <= 0f) return "\"damage\" tiene que ser mayor que 0";
            if (weapon.price <= 0) return "\"price\" tiene que ser mayor que 0";
            return null;
        }
    }
}
