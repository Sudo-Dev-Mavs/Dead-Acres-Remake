using System.Collections.Generic;
using DeadAcres.Combat;
using DeadAcres.Core.Events;
using DeadAcres.Core.Session;
using DeadAcres.DayNight;
using DeadAcres.Difficulty;
using DeadAcres.Economy;
using DeadAcres.Enemies;
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
    /// Los valores de ítems, cultivos y precios son provisionales: el original no publica sus números.
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
            public ShopCatalog Shop;
            public WaveConfig Waves;
            public ZombieBrain ZombiePrefab;
            public VoidEventChannel RvDestroyed;
            public VoidEventChannel Victory;
            public int StartingGold;
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
            EditorPaths.EnsureFolder($"{Root}/Shop");

            var data = new GeneratedData();

            data.DayNight = EditorPaths.LoadOrCreate<DayNightConfig>($"{Root}/DayNightConfig.asset");
            data.DayNight.Configure(dawn: 15f, firstDay: 360f, day: 240f, dusk: 30f, minNight: 120f, maxNight: 300f);
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
            data.RvDestroyed = EditorPaths.LoadOrCreate<VoidEventChannel>($"{Root}/Events/RvDestroyedChannel.asset");
            data.Victory = EditorPaths.LoadOrCreate<VoidEventChannel>($"{Root}/Events/VictoryChannel.asset");

            // 12 zombis la primera noche y 5 más cada noche; salen repartidos en 60 s (uno cada ~5 s
            // la primera noche). Con 5 en 100 s la noche se sentía vacía.
            data.Waves = EditorPaths.LoadOrCreate<WaveConfig>($"{Root}/WaveConfig.asset");
            data.Waves.Configure(firstNight: 12, perNight: 5, delay: 3f, window: 60f);

            // Si se despeja la oleada con al menos 15 s de noche por delante: llega un grupo de
            // refuerzo (25 % de la oleada, mínimo 3, hasta 2 grupos) a 0,6 del radio y el resto
            // de la noche pasa al triple de velocidad.
            data.Waves.ConfigureReinforcements(fraction: 0.25f, minCount: 3, maxGroups: 2, window: 6f,
                minRemaining: 15f, fastForward: 3f, radiusFactor: 0.6f);
            EditorUtility.SetDirty(data.Waves);
            data.ZombiePrefab = CreateZombiePrefab(data.Session);

            GenerateItemsAndCrops(data);

            AssetDatabase.SaveAssets();
            return data;
        }

        private static void GenerateItemsAndCrops(GeneratedData data)
        {
            ItemDefinition hoe = CreateItem("tool_hoe", "Azada", ItemKind.Tool, 1,
                new Color(0.60f, 0.60f, 0.66f), ToolKind.Hoe);

            // Sin la hoz no se puede cosechar.
            ItemDefinition sickle = CreateItem("tool_sickle", "Hoz dentada", ItemKind.Tool, 1,
                new Color(0.85f, 0.80f, 0.55f), ToolKind.Sickle);

            // Un colocable sencillo para probar la colocación en rejilla (fantasma, Q/E, límites).
            var wallColor = new Color(0.55f, 0.38f, 0.22f);
            GameObject wallPrefab = CreateBoxPrefab("WoodWall", new Vector3(1f, 2f, 0.3f), wallColor, 150f, data.Session);
            ItemDefinition wall = CreateItem("placeable_wood_wall", "Muro de madera", ItemKind.Placeable, 99,
                wallColor, ToolKind.None, wallPrefab);
            wall.ConfigurePrices(buy: 5, sell: 2); // 5 de oro: uno de los pocos precios que se oyen en el original

            // Pistola inicial, como en el original. 20 de daño: un zombi (60 de vida) cae de tres tiros.
            ItemDefinition pistol = CreateItem("weapon_pistol", "Pistola", ItemKind.Weapon, 1,
                new Color(0.25f, 0.25f, 0.28f));
            pistol.ConfigureWeapon(damage: 20f, range: 60f, interval: 0.25f);

            // id, nombre, color maduro, s. hasta 1.ª cosecha, s. de rebrote, rendimiento mín-máx, altura,
            // precio de la semilla, precio de venta del fruto.
            var crops = new List<CropDefinition>
            {
                CreateCrop("strawberry", "Fresa", new Color(0.86f, 0.12f, 0.20f), 60f, 45f, 2, 3, 0.40f, 5, 3),
                CreateCrop("potato", "Papa", new Color(0.76f, 0.62f, 0.38f), 120f, 90f, 2, 4, 0.35f, 8, 4),
                CreateCrop("tomato", "Tomate", new Color(0.92f, 0.28f, 0.12f), 90f, 60f, 2, 3, 0.80f, 10, 5),
                CreateCrop("wheat", "Trigo", new Color(0.90f, 0.76f, 0.30f), 75f, 60f, 1, 2, 0.90f, 6, 6),
                CreateCrop("sunflower", "Girasol", new Color(0.98f, 0.82f, 0.10f), 100f, 80f, 1, 1, 1.40f, 12, 14),
            };

            // Qué trae Jack y desde qué día: su catálogo crece, como en el original.
            data.Shop = EditorPaths.LoadOrCreate<ShopCatalog>($"{Root}/Shop/ShopCatalog.asset");
            data.Shop.Configure(new List<KeyValuePair<ItemDefinition, int>>
            {
                new KeyValuePair<ItemDefinition, int>(crops[0].SeedItem, 1),
                new KeyValuePair<ItemDefinition, int>(crops[1].SeedItem, 1),
                new KeyValuePair<ItemDefinition, int>(wall, 1),
                new KeyValuePair<ItemDefinition, int>(crops[2].SeedItem, 2),
                new KeyValuePair<ItemDefinition, int>(crops[3].SeedItem, 2),
                new KeyValuePair<ItemDefinition, int>(crops[4].SeedItem, 3),
            });
            EditorUtility.SetDirty(data.Shop);
            data.StartingGold = 20;

            data.Crops = EditorPaths.LoadOrCreate<CropCatalog>($"{Root}/CropCatalog.asset");
            data.Crops.Configure(crops);
            EditorUtility.SetDirty(data.Crops);

            data.StartingItems = new List<KeyValuePair<ItemDefinition, int>>
            {
                new KeyValuePair<ItemDefinition, int>(pistol, 1),
                new KeyValuePair<ItemDefinition, int>(hoe, 1),
                new KeyValuePair<ItemDefinition, int>(sickle, 1),
                new KeyValuePair<ItemDefinition, int>(crops[0].SeedItem, 8),
                new KeyValuePair<ItemDefinition, int>(crops[1].SeedItem, 4),
                new KeyValuePair<ItemDefinition, int>(wall, 12),
            };
        }

        private static CropDefinition CreateCrop(string id, string name, Color color, float growSeconds,
            float regrowSeconds, int yieldMin, int yieldMax, float height, int seedPrice, int produceSellPrice)
        {
            Color seedColor = Color.Lerp(color, new Color(0.45f, 0.36f, 0.24f), 0.6f);
            ItemDefinition seed = CreateItem($"seed_{id}", $"Semillas de {name.ToLowerInvariant()}",
                ItemKind.Seed, 99, seedColor);
            ItemDefinition produce = CreateItem($"produce_{id}", name, ItemKind.Produce, 99, color);
            seed.ConfigurePrices(buy: seedPrice, sell: 0);
            produce.ConfigurePrices(buy: 0, sell: produceSellPrice);

            var crop = EditorPaths.LoadOrCreate<CropDefinition>($"{Root}/Crops/Crop_{id}.asset");
            crop.Configure(id, name, seed, produce, growSeconds, regrowSeconds, yieldMin, yieldMax, color, height);
            EditorUtility.SetDirty(crop);
            return crop;
        }

        private static ItemDefinition CreateItem(string id, string name, ItemKind kind, int maxStack,
            Color color, ToolKind tool = ToolKind.None, GameObject placedPrefab = null)
        {
            var item = EditorPaths.LoadOrCreate<ItemDefinition>($"{Root}/Items/Item_{id}.asset");
            item.Configure(name, kind, maxStack);
            item.ConfigureExtras(id, color, tool, placedPrefab);
            EditorUtility.SetDirty(item);
            return item;
        }

        /// <summary>
        /// Prefab provisional: raíz a ras de suelo (donde lo instancia la colocación) y una caja
        /// hija con collider, levantada media altura. Con vida: los zombis pueden romperlo.
        /// </summary>
        private static GameObject CreateBoxPrefab(string name, Vector3 size, Color color, float maxHealth,
            GameSession session)
        {
            const string folder = "Assets/_Project/Prefabs/Placeables";
            EditorPaths.EnsureFolder(folder);

            var root = new GameObject(name);
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Box";
            box.transform.SetParent(root.transform, false);
            box.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = EditorPaths.LoadOrCreateMaterial($"Greybox_{name}", color);

            root.AddComponent<Health>().Configure(Faction.PlayerSide, maxHealth, session);
            root.AddComponent<DestroyOnDeath>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{folder}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Zombi provisional: cápsula verde con CharacterController, vida y cerebro.</summary>
        private static ZombieBrain CreateZombiePrefab(GameSession session)
        {
            const string folder = "Assets/_Project/Prefabs/Enemies";
            EditorPaths.EnsureFolder(folder);

            var root = new GameObject("Zombie");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            // El visual no lleva collider: el del CharacterController es el que recibe los disparos.
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Visual";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            body.GetComponent<Renderer>().sharedMaterial =
                EditorPaths.LoadOrCreateMaterial("Greybox_Zombie", new Color(0.35f, 0.55f, 0.30f));

            root.AddComponent<Health>().Configure(Faction.Enemy, 60f, session);
            root.AddComponent<ZombieBrain>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{folder}/Zombie.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<ZombieBrain>();
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
