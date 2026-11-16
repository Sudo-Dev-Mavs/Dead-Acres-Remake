using System.Collections.Generic;
using DeadAcres.Combat;
using DeadAcres.Animals;
using DeadAcres.Audio;
using DeadAcres.Building;
using DeadAcres.Core.Events;
using DeadAcres.Core.Session;
using DeadAcres.DayNight;
using DeadAcres.Defenses;
using DeadAcres.Difficulty;
using DeadAcres.Economy;
using DeadAcres.Enemies;
using DeadAcres.Farming;
using DeadAcres.GameMode;
using DeadAcres.Items;
using DeadAcres.Loot;
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
            public ZombieBrain StrongZombiePrefab;
            public ItemCatalog Items;
            public AudioLibrary Audio;
            public List<DifficultyProfile> Difficulties;
            public List<GameModeRules> Modes;
            public LootTable CarLoot;
            public LootTable SpecialCarLoot;
            public ItemDefinition SheepItem;
            public ItemDefinition ChickenItem;
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
            EditorPaths.EnsureFolder($"{Root}/Loot");

            var data = new GeneratedData();
            createdItems.Clear();

            data.DayNight = EditorPaths.LoadOrCreate<DayNightConfig>($"{Root}/DayNightConfig.asset");
            data.DayNight.Configure(dawn: 15f, firstDay: 360f, day: 240f, dusk: 30f, minNight: 120f, maxNight: 300f);
            EditorUtility.SetDirty(data.DayNight);

            // El primer nombre es el del asset (no cambia); el segundo, el que ve el jugador y el
            // que se escribe en la partida guardada.
            DifficultyProfile medium = CreateDifficulty("Medium", "Media", "La experiencia base.", 1f, 1f);
            data.Difficulties = new List<DifficultyProfile>
            {
                CreateDifficulty("Easy", "Fácil", "Para aprender el juego. Los zombis caen antes.", 0.7f, 0.75f),
                medium,
                CreateDifficulty("Hard", "Difícil", "Los zombis aguantan más y pegan más fuerte.", 1.5f, 1.2f),
                CreateDifficulty("Hell", "Infierno", "Solo para veteranos de la granja.", 2.2f, 1.6f),
            };

            var campaign = EditorPaths.LoadOrCreate<GameModeRules>($"{Root}/GameModes/GameMode_Campaign.asset");
            campaign.Configure("Campaña", plants: 100, objects: 100, nights: 5);
            EditorUtility.SetDirty(campaign);

            var endless = EditorPaths.LoadOrCreate<GameModeRules>($"{Root}/GameModes/GameMode_Endless.asset");
            endless.Configure("Endless", plants: 500, objects: 500, nights: 0);
            EditorUtility.SetDirty(endless);

            data.Modes = new List<GameModeRules> { campaign, endless };

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

            // La cripta suelta zombis fuertes desde la segunda noche: uno más cada noche, hasta 8.
            data.Waves.ConfigureStrong(fromNight: 2, perNight: 1, maxCount: 8);
            EditorUtility.SetDirty(data.Waves);

            // Zombi normal: 60 de vida, tres tiros de pistola. El fuerte aguanta cuatro veces más,
            // pega más del doble y es más lento.
            data.ZombiePrefab = CreateZombiePrefab("Zombie", data.Session, 60f, 1.8f, 0.35f,
                new Color(0.35f, 0.55f, 0.30f), speed: 2.2f, damage: 8f, attackInterval: 1f);
            data.StrongZombiePrefab = CreateZombiePrefab("ZombieStrong", data.Session, 240f, 2.3f, 0.45f,
                new Color(0.45f, 0.25f, 0.50f), speed: 1.6f, damage: 20f, attackInterval: 1.2f);

            GenerateItemsAndCrops(data);

            // Sonido provisional: los .wav de Assets/_Project/Audio (sintetizados con
            // Tools/generate_placeholder_audio.py). Para cambiar uno, se sustituye el archivo.
            data.Audio = EditorPaths.LoadOrCreate<AudioLibrary>($"{Root}/AudioLibrary.asset");
            data.Audio.Configure(LoadClip);
            EditorUtility.SetDirty(data.Audio);

            // Todos los ítems, para encontrarlos por Id al cargar una partida.
            data.Items = EditorPaths.LoadOrCreate<ItemCatalog>($"{Root}/ItemCatalog.asset");
            data.Items.Configure(createdItems);
            EditorUtility.SetDirty(data.Items);

            AssetDatabase.SaveAssets();
            return data;
        }

        private static void GenerateItemsAndCrops(GeneratedData data)
        {
            ItemDefinition hoe = CreateItem("tool_hoe", "Azada", ItemKind.Tool, 1,
                new Color(0.60f, 0.60f, 0.66f), ToolKind.Hoe);

            ItemDefinition hammer = CreateItem("tool_hammer", "Martillo", ItemKind.Tool, 1,
                new Color(0.55f, 0.45f, 0.40f), ToolKind.Hammer);
            hammer.ConfigurePrices(buy: 15, sell: 0);

            // 60 de vida por uso: un muro (150) se repara entero con tres.
            ItemDefinition tape = CreateItem("consumable_duct_tape", "Cinta americana", ItemKind.Consumable, 20,
                new Color(0.70f, 0.70f, 0.72f));
            tape.ConfigureRepair(60f);
            tape.ConfigurePrices(buy: 15, sell: 0);

            ItemDefinition torch = CreatePlaceableItem("placeable_torch", "Antorcha", new Color(1f, 0.55f, 0.15f),
                CreateTorchPrefab(data.Session), buy: 10, sell: 4);
            ItemDefinition lamp = CreatePlaceableItem("placeable_lamp", "Luz portátil", new Color(0.95f, 0.95f, 0.80f),
                CreateLampPrefab(data.Session), buy: 40, sell: 15);
            ItemDefinition generator = CreatePlaceableItem("placeable_generator", "Generador", new Color(0.80f, 0.65f, 0.15f),
                CreateGeneratorPrefab(data.Session), buy: 150, sell: 60);
            ItemDefinition tnt = CreatePlaceableItem("placeable_tnt_scarecrow", "Espantapájaros TNT", new Color(0.85f, 0.15f, 0.15f),
                CreateTntScarecrowPrefab(data.Session), buy: 60, sell: 20);

            // 200 de oro: uno de los pocos precios que se oyen en el original.
            ItemDefinition turret = CreatePlaceableItem("placeable_turret", "Torreta", new Color(0.35f, 0.40f, 0.50f),
                CreateTurretPrefab(data.Session), buy: 200, sell: 80);

            // Sin la hoz no se puede cosechar.
            ItemDefinition sickle = CreateItem("tool_sickle", "Hoz dentada", ItemKind.Tool, 1,
                new Color(0.85f, 0.80f, 0.55f), ToolKind.Sickle);

            // Un colocable sencillo para probar la colocación en rejilla (fantasma, Q/E, límites).
            var wallColor = new Color(0.55f, 0.38f, 0.22f);
            GameObject wallRoot = NewPlaceable("WoodWall", 150f, data.Session);
            AddBox(wallRoot, "Box", new Vector3(0f, 1f, 0f), new Vector3(1f, 2f, 0.3f), "WoodWall", wallColor);
            GameObject wallPrefab = SavePlaceable(wallRoot);
            ItemDefinition wall = CreateItem("placeable_wood_wall", "Muro de madera", ItemKind.Placeable, 99,
                wallColor, ToolKind.None, wallPrefab);
            wall.ConfigurePrices(buy: 5, sell: 2); // 5 de oro: uno de los pocos precios que se oyen en el original

            // Pistola inicial, como en el original. 20 de daño: un zombi (60 de vida) cae de tres tiros.
            ItemDefinition pistol = CreateItem("weapon_pistol", "Pistola", ItemKind.Weapon, 1,
                new Color(0.25f, 0.25f, 0.28f));
            pistol.ConfigureWeapon(damage: 20f, range: 60f, interval: 0.25f);

            // El arma mejor: aparece en el auto más lejano y Jack la trae a partir del cuarto día.
            ItemDefinition rifle = CreateItem("weapon_rifle", "Rifle de caza", ItemKind.Weapon, 1,
                new Color(0.40f, 0.28f, 0.18f));
            rifle.ConfigureWeapon(damage: 60f, range: 90f, interval: 0.7f);
            rifle.ConfigurePrices(buy: 300, sell: 0);

            // id, nombre, color maduro, s. hasta 1.ª cosecha, s. de rebrote, rendimiento mín-máx, altura,
            // precio de la semilla, precio de venta del fruto.
            var crops = new List<CropDefinition>
            {
                CreateCrop("strawberry", "Fresa", new Color(0.86f, 0.12f, 0.20f), 60f, 45f, 2, 3, 0.40f, 5, 3),
                CreateCrop("potato", "Papa", new Color(0.76f, 0.62f, 0.38f), 120f, 90f, 2, 4, 0.35f, 8, 4),
                CreateCrop("tomato", "Tomate", new Color(0.92f, 0.28f, 0.12f), 90f, 60f, 2, 3, 0.80f, 10, 5),
                CreateCrop("wheat", "Trigo", new Color(0.90f, 0.76f, 0.30f), 75f, 60f, 1, 2, 0.90f, 6, 6),
                CreateCrop("sunflower", "Girasol", new Color(0.98f, 0.82f, 0.10f), 100f, 80f, 1, 1, 1.40f, 12, 14),

                // Árboles frutales: tardan más, pero dan más fruta cada vez.
                CreateCrop("apple", "Manzana", new Color(0.80f, 0.15f, 0.15f), 180f, 90f, 3, 5, 2.20f, 25, 6),
                CreateCrop("orange", "Naranja", new Color(0.98f, 0.55f, 0.10f), 180f, 90f, 3, 5, 2.20f, 25, 6),
                CreateCrop("lemon", "Limón", new Color(0.95f, 0.90f, 0.25f), 160f, 80f, 3, 5, 2.00f, 22, 5),
            };

            // Vida que recupera comer cada fruto (en el mismo orden que la lista de cultivos).
            float[] healByCrop = { 4f, 3f, 4f, 2f, 2f, 6f, 6f, 4f };
            for (int i = 0; i < crops.Count; i++)
            {
                crops[i].ProduceItem.ConfigureHeal(healByCrop[i]);
            }

            // Productos de los animales.
            ItemDefinition egg = CreateItem("produce_egg", "Huevo", ItemKind.Produce, 20, new Color(0.96f, 0.93f, 0.85f));
            egg.ConfigurePrices(buy: 0, sell: 10);
            egg.ConfigureHeal(12f);
            ItemDefinition wool = CreateItem("produce_wool", "Lana", ItemKind.Produce, 20, new Color(0.92f, 0.92f, 0.92f));
            wool.ConfigurePrices(buy: 0, sell: 15);

            // Los animales son colocables: se compran, se ponen en la granja y el martillo los recoge.
            GameObject chickenPrefab = CreateAnimalPrefab("Chicken", "Gallina", "Recoger", egg, 120f, 20f,
                new Vector3(0.3f, 0.3f, 0.4f), new Color(0.95f, 0.85f, 0.55f), data.Session);
            GameObject sheepPrefab = CreateAnimalPrefab("Sheep", "Oveja", "Esquilar", wool, 180f, 50f,
                new Vector3(0.6f, 0.5f, 0.9f), new Color(0.93f, 0.93f, 0.93f), data.Session);
            ItemDefinition chicken = CreatePlaceableItem("placeable_chicken", "Gallina", new Color(0.95f, 0.85f, 0.55f),
                chickenPrefab, buy: 50, sell: 20);
            ItemDefinition sheep = CreatePlaceableItem("placeable_sheep", "Oveja", new Color(0.93f, 0.93f, 0.93f),
                sheepPrefab, buy: 90, sell: 35);
            data.ChickenItem = chicken;
            data.SheepItem = sheep;

            // Más colocables. Fogata a 100 de oro: uno de los pocos precios que se oyen en el original.
            ItemDefinition fence = CreatePlaceableItem("placeable_fence", "Valla", new Color(0.62f, 0.45f, 0.28f),
                CreateFencePrefab(data.Session), buy: 3, sell: 1);
            ItemDefinition gate = CreatePlaceableItem("placeable_gate", "Puerta", new Color(0.50f, 0.34f, 0.20f),
                CreateGatePrefab(data.Session), buy: 15, sell: 6);
            ItemDefinition campfire = CreatePlaceableItem("placeable_campfire", "Fogata", new Color(1f, 0.45f, 0.10f),
                CreateCampfirePrefab(data.Session), buy: 100, sell: 40);
            ItemDefinition well = CreatePlaceableItem("placeable_well", "Pozo", new Color(0.55f, 0.57f, 0.60f),
                CreateWellPrefab(data.Session), buy: 30, sell: 12);
            ItemDefinition windmill = CreatePlaceableItem("placeable_windmill", "Molino", new Color(0.85f, 0.80f, 0.70f),
                CreateWindmillPrefab(data.Session), buy: 80, sell: 30, footprint: new Vector2Int(2, 2));

            // Qué trae Jack y desde qué día: su catálogo crece, como en el original.
            data.Shop = EditorPaths.LoadOrCreate<ShopCatalog>($"{Root}/Shop/ShopCatalog.asset");
            data.Shop.Configure(new List<KeyValuePair<ItemDefinition, int>>
            {
                new KeyValuePair<ItemDefinition, int>(crops[0].SeedItem, 1),
                new KeyValuePair<ItemDefinition, int>(crops[1].SeedItem, 1),
                new KeyValuePair<ItemDefinition, int>(hammer, 1),
                new KeyValuePair<ItemDefinition, int>(wall, 1),
                new KeyValuePair<ItemDefinition, int>(fence, 1),
                new KeyValuePair<ItemDefinition, int>(gate, 1),
                new KeyValuePair<ItemDefinition, int>(torch, 1),
                new KeyValuePair<ItemDefinition, int>(tape, 1),
                new KeyValuePair<ItemDefinition, int>(lamp, 1),
                new KeyValuePair<ItemDefinition, int>(crops[2].SeedItem, 2),
                new KeyValuePair<ItemDefinition, int>(crops[3].SeedItem, 2),
                new KeyValuePair<ItemDefinition, int>(crops[5].SeedItem, 2),
                new KeyValuePair<ItemDefinition, int>(chicken, 2),
                new KeyValuePair<ItemDefinition, int>(campfire, 2),
                new KeyValuePair<ItemDefinition, int>(well, 2),
                new KeyValuePair<ItemDefinition, int>(tnt, 2),
                new KeyValuePair<ItemDefinition, int>(turret, 2),
                new KeyValuePair<ItemDefinition, int>(crops[4].SeedItem, 3),
                new KeyValuePair<ItemDefinition, int>(crops[6].SeedItem, 3),
                new KeyValuePair<ItemDefinition, int>(crops[7].SeedItem, 3),
                new KeyValuePair<ItemDefinition, int>(sheep, 3),
                new KeyValuePair<ItemDefinition, int>(windmill, 3),
                new KeyValuePair<ItemDefinition, int>(generator, 3),
                new KeyValuePair<ItemDefinition, int>(rifle, 4),
            });
            EditorUtility.SetDirty(data.Shop);
            data.StartingGold = 20;

            data.Crops = EditorPaths.LoadOrCreate<CropCatalog>($"{Root}/CropCatalog.asset");
            data.Crops.Configure(crops);
            EditorUtility.SetDirty(data.Crops);

            // Autos abandonados: algo de oro y dos cosas al azar. Lo que anota la referencia del
            // original: semillas, luces portátiles y un arma mejor.
            var carEntries = new List<LootTable.Entry>
            {
                new LootTable.Entry(crops[0].SeedItem, 3, 6, 3f),
                new LootTable.Entry(crops[1].SeedItem, 2, 4, 3f),
                new LootTable.Entry(crops[2].SeedItem, 2, 3, 2f),
                new LootTable.Entry(tape, 1, 2, 2f),
                new LootTable.Entry(torch, 1, 2, 2f),
                new LootTable.Entry(wall, 3, 6, 2f),
                new LootTable.Entry(lamp, 1, 1, 1f),
            };
            data.CarLoot = EditorPaths.LoadOrCreate<LootTable>($"{Root}/Loot/Loot_Car.asset");
            data.CarLoot.Configure(10, 30, 2, null, carEntries);
            EditorUtility.SetDirty(data.CarLoot);

            data.SpecialCarLoot = EditorPaths.LoadOrCreate<LootTable>($"{Root}/Loot/Loot_CarSpecial.asset");
            data.SpecialCarLoot.Configure(30, 60, 2,
                new List<LootTable.Entry> { new LootTable.Entry(rifle, 1, 1) }, new List<LootTable.Entry>(carEntries));
            EditorUtility.SetDirty(data.SpecialCarLoot);

            data.StartingItems = new List<KeyValuePair<ItemDefinition, int>>
            {
                new KeyValuePair<ItemDefinition, int>(pistol, 1),
                new KeyValuePair<ItemDefinition, int>(hoe, 1),
                new KeyValuePair<ItemDefinition, int>(sickle, 1),

                // Solo las semillas más baratas: todo lo demás se compra a Jack.
                new KeyValuePair<ItemDefinition, int>(crops[0].SeedItem, 8),
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

        private static AudioClip LoadClip(string fileName)
        {
            string path = $"Assets/_Project/Audio/{fileName}.wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"Dead Acres: falta el sonido {path}; esa parte quedará en silencio.");
            }

            return clip;
        }

        private static readonly List<ItemDefinition> createdItems = new List<ItemDefinition>();

        private static ItemDefinition CreateItem(string id, string name, ItemKind kind, int maxStack,
            Color color, ToolKind tool = ToolKind.None, GameObject placedPrefab = null, Vector2Int footprint = default)
        {
            var item = EditorPaths.LoadOrCreate<ItemDefinition>($"{Root}/Items/Item_{id}.asset");
            item.Configure(name, kind, maxStack, footprint);
            item.ConfigureExtras(id, color, tool, placedPrefab);
            EditorUtility.SetDirty(item);
            createdItems.Add(item);
            return item;
        }

        private static ItemDefinition CreatePlaceableItem(string id, string name, Color color, GameObject prefab,
            int buy, int sell, Vector2Int footprint = default)
        {
            ItemDefinition item = CreateItem(id, name, ItemKind.Placeable, 99, color, ToolKind.None, prefab, footprint);
            item.ConfigurePrices(buy, sell);
            return item;
        }

        // ---------- Prefabs provisionales de colocables ----------
        // La raíz queda a ras de suelo (donde instancia la colocación) y lleva la vida; las
        // piezas son cajas hijas.

        private static GameObject NewPlaceable(string name, float maxHealth, GameSession session)
        {
            var root = new GameObject(name);
            root.AddComponent<Health>().Configure(Faction.PlayerSide, maxHealth, session);
            root.AddComponent<DestroyOnDeath>();
            return root;
        }

        private static GameObject AddBox(GameObject parent, string name, Vector3 localPosition, Vector3 size,
            string materialName, Color color, bool withCollider = true)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            if (!withCollider)
            {
                Object.DestroyImmediate(box.GetComponent<Collider>());
            }

            box.transform.SetParent(parent.transform, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = EditorPaths.LoadOrCreateMaterial($"Greybox_{materialName}", color);
            return box;
        }

        private static Light AddPointLight(GameObject parent, Vector3 localPosition, Color color, float range,
            float intensity)
        {
            var lightObject = new GameObject("Light");
            lightObject.transform.SetParent(parent.transform, false);
            lightObject.transform.localPosition = localPosition;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            return light;
        }

        private static GameObject SavePlaceable(GameObject root)
        {
            const string folder = "Assets/_Project/Prefabs/Placeables";
            EditorPaths.EnsureFolder(folder);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{folder}/{root.name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Valla: baja (1 m). Frena a los zombis y se puede disparar por encima.</summary>
        private static GameObject CreateFencePrefab(GameSession session)
        {
            GameObject root = NewPlaceable("Fence", 80f, session);
            AddBox(root, "Box", new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 0.15f), "Wood", new Color(0.45f, 0.30f, 0.18f));
            return SavePlaceable(root);
        }

        /// <summary>Puerta: dos postes y una hoja que se abre y se cierra con F.</summary>
        private static GameObject CreateGatePrefab(GameSession session)
        {
            GameObject root = NewPlaceable("Gate", 150f, session);
            // Los postes no llevan collider: abierta, el hueco es la celda entera.
            AddBox(root, "PostLeft", new Vector3(-0.47f, 1f, 0f), new Vector3(0.06f, 2f, 0.2f), "Wood", new Color(0.45f, 0.30f, 0.18f), false);
            AddBox(root, "PostRight", new Vector3(0.47f, 1f, 0f), new Vector3(0.06f, 2f, 0.2f), "Wood", new Color(0.45f, 0.30f, 0.18f), false);
            GameObject door = AddBox(root, "Door", new Vector3(0f, 0.9f, 0f), new Vector3(0.9f, 1.8f, 0.12f), "WoodWall", new Color(0.55f, 0.38f, 0.22f));

            // Umbral sólido a ras de suelo: con la puerta abierta sigue habiendo algo a lo que
            // apuntar con el martillo o la cinta, y no estorba el paso.
            AddBox(root, "Threshold", new Vector3(0f, 0.025f, 0f), new Vector3(1f, 0.05f, 0.2f), "Wood", new Color(0.45f, 0.30f, 0.18f));
            root.AddComponent<Gate>().Configure(door.transform);
            return SavePlaceable(root);
        }

        /// <summary>Fogata: más luz que la antorcha, sin energía.</summary>
        private static GameObject CreateCampfirePrefab(GameSession session)
        {
            GameObject root = NewPlaceable("Campfire", 80f, session);
            AddBox(root, "LogA", new Vector3(0f, 0.1f, 0f), new Vector3(0.9f, 0.2f, 0.2f), "Wood", new Color(0.45f, 0.30f, 0.18f));
            AddBox(root, "LogB", new Vector3(0f, 0.1f, 0f), new Vector3(0.2f, 0.2f, 0.9f), "Wood", new Color(0.45f, 0.30f, 0.18f), false);
            AddBox(root, "Flame", new Vector3(0f, 0.45f, 0f), new Vector3(0.35f, 0.5f, 0.35f), "Flame", new Color(1f, 0.55f, 0.15f), false);
            AddPointLight(root, new Vector3(0f, 1f, 0f), new Color(1f, 0.6f, 0.25f), 14f, 3.2f);
            return SavePlaceable(root);
        }

        /// <summary>Pozo: decoración.</summary>
        private static GameObject CreateWellPrefab(GameSession session)
        {
            GameObject root = NewPlaceable("Well", 100f, session);
            AddBox(root, "Ring", new Vector3(0f, 0.35f, 0f), new Vector3(0.9f, 0.7f, 0.9f), "Stone", new Color(0.55f, 0.57f, 0.60f));
            AddBox(root, "PostLeft", new Vector3(-0.4f, 1.1f, 0f), new Vector3(0.08f, 0.9f, 0.08f), "Wood", new Color(0.45f, 0.30f, 0.18f), false);
            AddBox(root, "PostRight", new Vector3(0.4f, 1.1f, 0f), new Vector3(0.08f, 0.9f, 0.08f), "Wood", new Color(0.45f, 0.30f, 0.18f), false);
            AddBox(root, "Roof", new Vector3(0f, 1.6f, 0f), new Vector3(1f, 0.12f, 0.6f), "WoodWall", new Color(0.55f, 0.38f, 0.22f), false);
            return SavePlaceable(root);
        }

        /// <summary>Molino: decoración de 2×2 celdas con aspas que giran.</summary>
        private static GameObject CreateWindmillPrefab(GameSession session)
        {
            GameObject root = NewPlaceable("Windmill", 200f, session);
            AddBox(root, "Tower", new Vector3(0f, 2f, 0f), new Vector3(1.3f, 4f, 1.3f), "Windmill", new Color(0.85f, 0.80f, 0.70f));

            var blades = new GameObject("Blades");
            blades.transform.SetParent(root.transform, false);
            blades.transform.localPosition = new Vector3(0f, 3.5f, 0.75f);
            AddBox(blades, "BladeA", Vector3.zero, new Vector3(3.2f, 0.25f, 0.08f), "WoodWall", new Color(0.55f, 0.38f, 0.22f), false);
            AddBox(blades, "BladeB", Vector3.zero, new Vector3(0.25f, 3.2f, 0.08f), "WoodWall", new Color(0.55f, 0.38f, 0.22f), false);
            blades.AddComponent<Spinner>();
            return SavePlaceable(root);
        }

        /// <summary>
        /// Animal provisional: una caja con collider que pasea, con un cubo flotante que aparece
        /// cuando tiene producto listo.
        /// </summary>
        private static GameObject CreateAnimalPrefab(string prefabName, string displayName, string verb,
            ItemDefinition product, float secondsPerProduct, float maxHealth, Vector3 bodySize, Color color,
            GameSession session)
        {
            const string folder = "Assets/_Project/Prefabs/Animals";
            EditorPaths.EnsureFolder(folder);

            var root = new GameObject(prefabName);
            float bodyY = bodySize.y * 0.5f + 0.15f;
            AddBox(root, "Body", new Vector3(0f, bodyY, 0f), bodySize, prefabName, color);
            AddBox(root, "Head", new Vector3(0f, bodyY + bodySize.y * 0.45f, bodySize.z * 0.5f),
                bodySize * 0.5f, prefabName, color, false);
            GameObject indicator = AddBox(root, "ReadyIndicator", new Vector3(0f, bodyY + bodySize.y + 0.35f, 0f),
                new Vector3(0.18f, 0.18f, 0.18f), "ProductReady", new Color(1f, 0.85f, 0.2f), false);

            // Se mueve por transform: con Rigidbody cinemático el collider se desplaza bien.
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            root.AddComponent<Health>().Configure(Faction.PlayerSide, maxHealth, session);
            root.AddComponent<DestroyOnDeath>();
            root.AddComponent<Animal>().Configure(displayName, verb, product, 1, secondsPerProduct, indicator);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{folder}/{prefabName}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Antorcha: luz sin energía, de poco alcance.</summary>
        private static GameObject CreateTorchPrefab(GameSession session)
        {
            GameObject root = NewPlaceable("Torch", 40f, session);
            AddBox(root, "Post", new Vector3(0f, 0.6f, 0f), new Vector3(0.12f, 1.2f, 0.12f), "Wood", new Color(0.45f, 0.30f, 0.18f));
            AddBox(root, "Flame", new Vector3(0f, 1.3f, 0f), new Vector3(0.2f, 0.2f, 0.2f), "Flame", new Color(1f, 0.55f, 0.15f), false);
            AddPointLight(root, new Vector3(0f, 1.5f, 0f), new Color(1f, 0.65f, 0.3f), 9f, 2.2f);
            return SavePlaceable(root);
        }

        /// <summary>Luz portátil: alumbra más, pero solo con energía cerca.</summary>
        private static GameObject CreateLampPrefab(GameSession session)
        {
            GameObject root = NewPlaceable("Lamp", 60f, session);
            AddBox(root, "Post", new Vector3(0f, 0.9f, 0f), new Vector3(0.15f, 1.8f, 0.15f), "Metal", new Color(0.45f, 0.47f, 0.50f));
            AddBox(root, "Head", new Vector3(0f, 1.9f, 0f), new Vector3(0.5f, 0.25f, 0.35f), "LampHead", new Color(0.95f, 0.95f, 0.80f), false);
            Light light = AddPointLight(root, new Vector3(0f, 2.1f, 0f), new Color(1f, 0.97f, 0.88f), 16f, 3f);
            var consumer = root.AddComponent<PowerConsumer>();
            root.AddComponent<PoweredLight>().Configure(consumer, light);
            return SavePlaceable(root);
        }

        /// <summary>Generador: fuente de energía que se puede colocar lejos de la caravana.</summary>
        private static GameObject CreateGeneratorPrefab(GameSession session)
        {
            GameObject root = NewPlaceable("Generator", 200f, session);
            AddBox(root, "Body", new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 0.9f), "Generator", new Color(0.80f, 0.65f, 0.15f));
            root.AddComponent<PowerSource>().Configure(null, 9f);
            return SavePlaceable(root);
        }

        /// <summary>Torreta: base fija, cabeza giratoria con cañón, luz de estado y rayo de disparo.</summary>
        private static GameObject CreateTurretPrefab(GameSession session)
        {
            GameObject root = NewPlaceable("Turret", 150f, session);
            AddBox(root, "Base", new Vector3(0f, 0.35f, 0f), new Vector3(0.8f, 0.7f, 0.8f), "Metal", new Color(0.45f, 0.47f, 0.50f));

            var head = new GameObject("Head");
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            AddBox(head, "Body", Vector3.zero, new Vector3(0.45f, 0.35f, 0.45f), "TurretHead", new Color(0.30f, 0.34f, 0.42f), false);
            AddBox(head, "Barrel", new Vector3(0f, 0f, 0.45f), new Vector3(0.12f, 0.12f, 0.6f), "TurretHead", new Color(0.30f, 0.34f, 0.42f), false);

            GameObject status = AddBox(root, "StatusLight", new Vector3(0f, 0.75f, -0.3f), new Vector3(0.14f, 0.14f, 0.14f), "Tint", Color.white, false);
            GameObject beam = AddBox(root, "Beam", Vector3.zero, new Vector3(0.05f, 0.05f, 1f), "Flame", new Color(1f, 0.55f, 0.15f), false);
            beam.SetActive(false);

            var consumer = root.AddComponent<PowerConsumer>();
            root.AddComponent<Turret>().Configure(consumer, head.transform, beam.transform, status.GetComponent<Renderer>());
            return SavePlaceable(root);
        }

        /// <summary>Espantapájaros con TNT: explota cuando se le acerca un enemigo.</summary>
        private static GameObject CreateTntScarecrowPrefab(GameSession session)
        {
            GameObject root = NewPlaceable("TntScarecrow", 40f, session);
            AddBox(root, "Post", new Vector3(0f, 0.8f, 0f), new Vector3(0.14f, 1.6f, 0.14f), "Wood", new Color(0.45f, 0.30f, 0.18f));
            AddBox(root, "Arms", new Vector3(0f, 1.2f, 0f), new Vector3(1.1f, 0.12f, 0.12f), "Wood", new Color(0.45f, 0.30f, 0.18f), false);
            AddBox(root, "Tnt", new Vector3(0f, 0.85f, 0.15f), new Vector3(0.4f, 0.5f, 0.25f), "Tnt", new Color(0.85f, 0.15f, 0.15f), false);

            // La onda: una esfera que se muestra un instante. Su diámetro es el del radio de daño.
            GameObject blast = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blast.name = "Blast";
            Object.DestroyImmediate(blast.GetComponent<Collider>());
            blast.transform.SetParent(root.transform, false);
            blast.transform.localPosition = new Vector3(0f, 1f, 0f);
            blast.transform.localScale = Vector3.one * 9f;
            blast.GetComponent<Renderer>().sharedMaterial =
                EditorPaths.LoadOrCreateMaterial("Greybox_Flame", new Color(1f, 0.55f, 0.15f));
            blast.SetActive(false);

            root.AddComponent<TntScarecrow>().Configure(blast);
            return SavePlaceable(root);
        }

        /// <summary>Zombi provisional: cápsula verde con CharacterController, vida y cerebro.</summary>
        private static ZombieBrain CreateZombiePrefab(string prefabName, GameSession session, float maxHealth,
            float height, float radius, Color color, float speed, float damage, float attackInterval)
        {
            const string folder = "Assets/_Project/Prefabs/Enemies";
            EditorPaths.EnsureFolder(folder);

            var root = new GameObject(prefabName);
            var controller = root.AddComponent<CharacterController>();
            controller.height = height;
            controller.radius = radius;
            controller.center = new Vector3(0f, height * 0.5f, 0f);

            // El visual no lleva collider: el del CharacterController es el que recibe los disparos.
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Visual";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            // La cápsula de Unity mide 2 de alto y 1 de ancho a escala 1.
            body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            body.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            body.GetComponent<Renderer>().sharedMaterial =
                EditorPaths.LoadOrCreateMaterial($"Greybox_{prefabName}", color);

            root.AddComponent<Health>().Configure(Faction.Enemy, maxHealth, session);
            root.AddComponent<ZombieBrain>().Configure(speed, damage, attackInterval);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{folder}/{prefabName}.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<ZombieBrain>();
        }

        private static DifficultyProfile CreateDifficulty(string assetName, string displayName, string flavor,
            float health, float damage)
        {
            var profile = EditorPaths.LoadOrCreate<DifficultyProfile>($"{Root}/Difficulty/Difficulty_{assetName}.asset");
            profile.Configure(displayName, flavor, health, damage);
            EditorUtility.SetDirty(profile);
            return profile;
        }
    }
}
