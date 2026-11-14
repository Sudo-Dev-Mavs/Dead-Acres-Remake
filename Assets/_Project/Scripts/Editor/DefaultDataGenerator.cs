using System.Collections.Generic;
using DeadAcres.Core.Events;
using DeadAcres.Core.Session;
using DeadAcres.DayNight;
using DeadAcres.Difficulty;
using DeadAcres.Farming;
using DeadAcres.GameMode;
using DeadAcres.Items;
using UnityEditor;
using UnityEngine;

namespace DeadAcres.EditorTools
{
    /// <summary>
    /// Crea (o actualiza) los assets de configuración, para no teclearlos a mano en el Inspector.
    /// Menú: Dead Acres → Generar datos por defecto.
    /// Los valores de ítems y cultivos son provisionales: el original no publica sus números.
    /// </summary>
    public static class DefaultDataGenerator
    {
        private const string Root = "Assets/_Project/Data";

        /// <summary>Lo que necesita el generador de escena para cablear los componentes.</summary>
        internal sealed class GeneratedData
        {
            public DayNightConfig DayNight;
            public GameSession Session;
            public PhaseEventChannel PhaseChanged;
            public NightResultEventChannel NightCompleted;
            public CropCatalog Crops;
            public List<KeyValuePair<ItemDefinition, int>> StartingItems;
        }

        [MenuItem("Dead Acres/Generar datos por defecto")]
        public static void GenerateFromMenu()
        {
            Generate();
            Debug.Log($"Dead Acres: datos por defecto generados en {Root}.");
        }

        internal static GeneratedData Generate()
        {
            EditorPaths.EnsureFolder($"{Root}/Difficulty");
            EditorPaths.EnsureFolder($"{Root}/GameModes");
            EditorPaths.EnsureFolder($"{Root}/Events");
            EditorPaths.EnsureFolder($"{Root}/Items");
            EditorPaths.EnsureFolder($"{Root}/Crops");

            var data = new GeneratedData();

            data.DayNight = EditorPaths.LoadOrCreate<DayNightConfig>($"{Root}/DayNightConfig.asset");
            data.DayNight.Configure(dawn: 15f, firstDay: 360f, day: 240f, dusk: 30f, minNight: 180f, maxNight: 300f);
            EditorUtility.SetDirty(data.DayNight);

            CreateDifficulty("Easy", "Para aprender el juego. Los zombis caen antes.", 0.7f, 0.75f);
            DifficultyProfile medium = CreateDifficulty("Medium", "La experiencia base.", 1f, 1f);
            CreateDifficulty("Hard", "Los zombis aguantan más y pegan más fuerte.", 1.5f, 1.2f);
            CreateDifficulty("Hell", "Solo para veteranos de la granja.", 2.2f, 1.6f);

            var campaign = EditorPaths.LoadOrCreate<GameModeRules>($"{Root}/GameModes/GameMode_Campaign.asset");
            campaign.Configure("Campaña", plants: 100, objects: 100, nights: 5);
            EditorUtility.SetDirty(campaign);

            var endless = EditorPaths.LoadOrCreate<GameModeRules>($"{Root}/GameModes/GameMode_Endless.asset");
            endless.Configure("Endless", plants: 500, objects: 500, nights: 0);
            EditorUtility.SetDirty(endless);

            data.Session = EditorPaths.LoadOrCreate<GameSession>($"{Root}/GameSession.asset");
            data.Session.ConfigureDefaults(medium, campaign);
            EditorUtility.SetDirty(data.Session);

            data.PhaseChanged = EditorPaths.LoadOrCreate<PhaseEventChannel>($"{Root}/Events/PhaseChangedChannel.asset");
            data.NightCompleted = EditorPaths.LoadOrCreate<NightResultEventChannel>($"{Root}/Events/NightCompletedChannel.asset");
            EditorPaths.LoadOrCreate<VoidEventChannel>($"{Root}/Events/RvDestroyedChannel.asset");
            EditorPaths.LoadOrCreate<VoidEventChannel>($"{Root}/Events/VictoryChannel.asset");

            GenerateItemsAndCrops(data);

            AssetDatabase.SaveAssets();
            return data;
        }

        private static void GenerateItemsAndCrops(GeneratedData data)
        {
            ItemDefinition hoe = CreateItem("tool_hoe", "Azada", ItemCategory.Tool, 1,
                new Color(0.60f, 0.60f, 0.66f), ToolKind.Hoe);

            // id, nombre, color maduro, s. hasta 1.ª cosecha, s. de rebrote, rendimiento mín-máx, altura.
            var crops = new List<CropDefinition>
            {
                CreateCrop("strawberry", "Fresa", new Color(0.86f, 0.12f, 0.20f), 60f, 45f, 2, 3, 0.40f),
                CreateCrop("potato", "Papa", new Color(0.76f, 0.62f, 0.38f), 120f, 90f, 2, 4, 0.35f),
                CreateCrop("tomato", "Tomate", new Color(0.92f, 0.28f, 0.12f), 90f, 60f, 2, 3, 0.80f),
                CreateCrop("wheat", "Trigo", new Color(0.90f, 0.76f, 0.30f), 75f, 60f, 1, 2, 0.90f),
                CreateCrop("sunflower", "Girasol", new Color(0.98f, 0.82f, 0.10f), 100f, 80f, 1, 1, 1.40f),
            };

            data.Crops = EditorPaths.LoadOrCreate<CropCatalog>($"{Root}/CropCatalog.asset");
            data.Crops.Configure(crops);
            EditorUtility.SetDirty(data.Crops);

            data.StartingItems = new List<KeyValuePair<ItemDefinition, int>>
            {
                new KeyValuePair<ItemDefinition, int>(hoe, 1),
                new KeyValuePair<ItemDefinition, int>(crops[0].SeedItem, 8),
                new KeyValuePair<ItemDefinition, int>(crops[1].SeedItem, 4),
            };
        }

        private static CropDefinition CreateCrop(string id, string name, Color color, float growSeconds,
            float regrowSeconds, int yieldMin, int yieldMax, float height)
        {
            Color seedColor = Color.Lerp(color, new Color(0.45f, 0.36f, 0.24f), 0.6f);
            ItemDefinition seed = CreateItem($"seed_{id}", $"Semillas de {name.ToLowerInvariant()}",
                ItemCategory.Seed, 99, seedColor);
            ItemDefinition produce = CreateItem($"produce_{id}", name, ItemCategory.Produce, 99, color);

            var crop = EditorPaths.LoadOrCreate<CropDefinition>($"{Root}/Crops/Crop_{id}.asset");
            crop.Configure(id, name, seed, produce, growSeconds, regrowSeconds, yieldMin, yieldMax, color, height);
            EditorUtility.SetDirty(crop);
            return crop;
        }

        private static ItemDefinition CreateItem(string id, string name, ItemCategory category, int maxStack,
            Color color, ToolKind tool = ToolKind.None)
        {
            var item = EditorPaths.LoadOrCreate<ItemDefinition>($"{Root}/Items/Item_{id}.asset");
            item.Configure(id, name, category, maxStack, color, tool);
            EditorUtility.SetDirty(item);
            return item;
        }

        private static DifficultyProfile CreateDifficulty(string name, string flavor, float health, float damage)
        {
            var profile = EditorPaths.LoadOrCreate<DifficultyProfile>($"{Root}/Difficulty/Difficulty_{name}.asset");
            profile.Configure(name, flavor, health, damage);
            EditorUtility.SetDirty(profile);
            return profile;
        }
    }
}
