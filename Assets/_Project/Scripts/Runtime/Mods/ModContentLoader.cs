using System;
using System.Collections.Generic;
using System.IO;
using DeadAcres.Economy;
using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Mods
{
    /// <summary>
    /// Carga los mods al arrancar la partida. Busca carpetas de mod en dos sitios: la carpeta
    /// Mods que viaja con el juego (StreamingAssets/Mods) y la del usuario
    /// (persistentDataPath/Mods). Por ahora carga armas; el resto de carpetas de un mod están
    /// reservadas (ver docs/mods.md).
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class ModContentLoader : MonoBehaviour
    {
        public const string ModsFolderName = "Mods";

        [SerializeField] private ItemCatalog items;
        [SerializeField] private ShopCatalog shop;

        /// <summary>Mods encontrados en esta partida.</summary>
        public IReadOnlyList<ModManifest> LoadedMods => loadedMods;

        private readonly List<ModManifest> loadedMods = new List<ModManifest>();

        public static string GameModsPath => Path.Combine(Application.streamingAssetsPath, ModsFolderName);
        public static string UserModsPath => Path.Combine(Application.persistentDataPath, ModsFolderName);

        private void Awake()
        {
            if (items != null) items.ClearRuntime();
            if (shop != null) shop.ClearRuntime();

            LoadFrom(GameModsPath);
            LoadFrom(UserModsPath);
        }

        private void LoadFrom(string modsPath)
        {
            try
            {
                if (!Directory.Exists(modsPath))
                {
                    return;
                }

                foreach (string folder in Directory.GetDirectories(modsPath))
                {
                    // Las carpetas que empiezan por guion bajo son plantillas: no se cargan.
                    string folderName = Path.GetFileName(folder);
                    if (folderName.StartsWith("_"))
                    {
                        continue;
                    }

                    LoadMod(folder, folderName);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Mods: no se pudo leer {modsPath}: {exception.Message}");
            }
        }

        private void LoadMod(string folder, string folderName)
        {
            ModManifest manifest = ReadJson<ModManifest>(Path.Combine(folder, "mod.json")) ?? new ModManifest();
            if (string.IsNullOrWhiteSpace(manifest.id)) manifest.id = folderName;
            if (string.IsNullOrWhiteSpace(manifest.name)) manifest.name = folderName;
            loadedMods.Add(manifest);

            int weapons = LoadWeapons(Path.Combine(folder, "Weapons"), manifest);
            Debug.Log($"Mods: cargado \"{manifest.name}\" ({weapons} arma(s)).");
        }

        private int LoadWeapons(string folder, ModManifest manifest)
        {
            if (!Directory.Exists(folder) || items == null)
            {
                return 0;
            }

            int count = 0;
            foreach (string file in Directory.GetFiles(folder, "*.json"))
            {
                ModWeapon weapon = ReadJson<ModWeapon>(file);
                string problem = ModRules.Validate(weapon);
                if (problem != null)
                {
                    Debug.LogWarning($"Mods: se ignora {file}: {problem}.");
                    continue;
                }

                var item = ScriptableObject.CreateInstance<ItemDefinition>();
                item.name = weapon.name;
                item.Configure(weapon.name, ItemKind.Weapon, 1);
                item.ConfigureExtras(ModRules.BuildItemId(manifest.id, weapon.id), new Color(0.25f, 0.25f, 0.28f));
                item.ConfigureWeapon(weapon.damage, weapon.range, weapon.fireInterval, weapon.automatic, weapon.pellets,
                    weapon.spread);
                item.ConfigureMagazine(weapon.magazine, weapon.reloadSeconds);
                item.ConfigurePrices(weapon.price, 0);
                item.ConfigureRarity(ModRules.ParseRarity(weapon.rarity));

                items.RegisterRuntime(item);
                if (shop != null) shop.RegisterRuntime(item);
                count++;
            }

            return count;
        }

        private static T ReadJson<T>(string path) where T : class
        {
            try
            {
                return File.Exists(path) ? JsonUtility.FromJson<T>(File.ReadAllText(path)) : null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Mods: {path} no se pudo leer: {exception.Message}");
                return null;
            }
        }

        internal void Configure(ItemCatalog itemCatalog, ShopCatalog shopCatalog)
        {
            items = itemCatalog;
            shop = shopCatalog;
        }
    }
}
