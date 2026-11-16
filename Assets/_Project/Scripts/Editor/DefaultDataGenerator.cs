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
            // Día de 2 minutos: poco tiempo para cultivar, hay que elegir bien qué se hace.
            data.DayNight.Configure(dawn: 15f, firstDay: 120f, day: 120f, dusk: 30f, minNight: 120f, maxNight: 300f);
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

            // Cada noche los zombis tienen un 20 % más de vida y pegan un 15 % más que la primera
            // (noche 5: 108 de vida y 12,8 de daño el normal; 432 y 32 el fuerte).
            data.Waves.ConfigureGrowth(healthPerNight: 0.2f, damagePerNight: 0.15f);
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

            // La tienda puede traer cualquier cosa que tenga precio de compra; qué sale cada día
            // lo decide el azar según la rareza (ShopStock).
            var forSale = new List<KeyValuePair<ItemDefinition, int>>();
            foreach (ItemDefinition item in createdItems)
            {
                if (item.BuyPrice > 0) forSale.Add(new KeyValuePair<ItemDefinition, int>(item, 1));
            }

            data.Shop = EditorPaths.LoadOrCreate<ShopCatalog>($"{Root}/Shop/ShopCatalog.asset");
            data.Shop.Configure(forSale);
            EditorUtility.SetDirty(data.Shop);

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
            tape.ConfigurePrices(buy: 25, sell: 0);

            ItemDefinition torch = CreatePlaceableItem("placeable_torch", "Antorcha", new Color(1f, 0.55f, 0.15f),
                CreateTorchPrefab(data.Session), buy: 10, sell: 4);
            ItemDefinition lamp = CreatePlaceableItem("placeable_lamp", "Luz portátil", new Color(0.95f, 0.95f, 0.80f),
                CreateLampPrefab(data.Session), buy: 40, sell: 15);
            // Generador: 9 m de alcance y 20 de capacidad. El pequeño: 5 m y 8, más barato y más común.
            ItemDefinition generator = CreatePlaceableItem("placeable_generator", "Generador", new Color(0.80f, 0.65f, 0.15f),
                CreateGeneratorPrefab(data.Session, "Generator", 200f, 9f, 20f, 0.9f, new Color(0.80f, 0.65f, 0.15f)),
                buy: 150, sell: 60);
            ItemDefinition smallGenerator = CreatePlaceableItem("placeable_generator_small", "Generador pequeño",
                new Color(0.70f, 0.60f, 0.30f),
                CreateGeneratorPrefab(data.Session, "GeneratorSmall", 120f, 5f, 8f, 0.6f, new Color(0.70f, 0.60f, 0.30f)),
                buy: 70, sell: 28);
            ItemDefinition tnt = CreatePlaceableItem("placeable_tnt_scarecrow", "Espantapájaros TNT", new Color(0.85f, 0.15f, 0.15f),
                CreateTntScarecrowPrefab(data.Session), buy: 60, sell: 20);

            // 200 de oro: uno de los pocos precios que se oyen en el original.
            // Torretas. Todas necesitan energía: la caravana da 10 y cada generador 20. Columnas:
            // rareza, precio, oro por reparación, vida, consumo; luego modo, alcance, daño y cadencia.
            var orange = new Color(1f, 0.55f, 0.15f);
            CreateTurret(data.Session, "placeable_turret", "Turret", "Torreta",
                ItemRarity.UltraRare, 200, 5, 150f, 2f, new Color(0.30f, 0.34f, 0.42f), orange, 0.6f, 0.12f,
                t => t.ConfigureStats(TurretMode.Single, 12f, 10f, 0.5f));                               // 20 de daño/s

            // Doble cañón: dos disparos por ráfaga. Aguanta mucho.
            CreateTurret(data.Session, "placeable_turret_double", "TurretDouble", "Torreta de doble cañón",
                ItemRarity.UltraRare, 380, 9, 220f, 4f, new Color(0.25f, 0.30f, 0.30f), orange, 0.7f, 0.22f,
                t => t.ConfigureStats(TurretMode.Single, 14f, 14f, 0.5f, shots: 2));                     // 56/s

            // Escopeta: corto alcance, pega a todo lo que tenga delante (hasta 4).
            CreateTurret(data.Session, "placeable_turret_shotgun", "TurretShotgun", "Torreta escopeta",
                ItemRarity.UltraRare, 320, 8, 200f, 3f, new Color(0.45f, 0.30f, 0.20f), orange, 0.4f, 0.24f,
                t => t.ConfigureStats(TurretMode.Cone, 8f, 22f, 1f, coneAngle: 25f, coneTargets: 4));    // 22/s a cada uno

            // Lanzallamas: muy corto, daño continuo a todo el cono (hasta 8).
            CreateTurret(data.Session, "placeable_turret_flame", "TurretFlame", "Torreta lanzallamas",
                ItemRarity.Epic, 450, 10, 180f, 4f, new Color(0.55f, 0.20f, 0.12f), new Color(1f, 0.40f, 0.05f), 0.5f, 0.2f,
                t => t.ConfigureStats(TurretMode.Cone, 6f, 5f, 0.15f, coneAngle: 35f, coneTargets: 8,
                    newBeamWidth: 0.35f));                                                               // 33/s a cada uno

            // Francotiradora: llega lejos y pega fuerte, pero tarda en recargar y es frágil.
            CreateTurret(data.Session, "placeable_turret_sniper", "TurretSniper", "Torreta francotiradora",
                ItemRarity.Epic, 420, 10, 120f, 3f, new Color(0.20f, 0.28f, 0.22f), new Color(1f, 0.95f, 0.6f), 1.1f, 0.08f,
                t => t.ConfigureStats(TurretMode.Single, 30f, 120f, 2.5f));                              // 48/s; un zombi normal por tiro

            // Automática: cadencia de rifle de asalto. La que más gasta de las de bala.
            CreateTurret(data.Session, "placeable_turret_auto", "TurretAuto", "Torreta automática",
                ItemRarity.Epic, 600, 12, 160f, 5f, new Color(0.22f, 0.24f, 0.28f), orange, 0.65f, 0.1f,
                t => t.ConfigureStats(TurretMode.Single, 13f, 9f, 0.1f));                                // 90/s

            // Eléctrica: el rayo salta hasta 4 zombis más, a 5 m como mucho cada salto, y pierde
            // un 30 % de daño en cada uno (45, 31, 22, 15, 11).
            CreateTurret(data.Session, "placeable_turret_electric", "TurretElectric", "Torreta eléctrica",
                ItemRarity.Legendary, 850, 15, 140f, 6f, new Color(0.25f, 0.35f, 0.60f), new Color(0.55f, 0.80f, 1f), 0.35f, 0.16f,
                t => t.ConfigureStats(TurretMode.Chain, 11f, 45f, 1.2f, jumps: 4, jumpRange: 5f, falloff: 0.7f,
                    newBeamWidth: 0.08f));

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
            pistol.ConfigureMagazine(12, 1.2f);

            // El arma mejor: aparece en el auto más lejano y, con suerte, en la tienda.
            ItemDefinition rifle = CreateWeapon("weapon_rifle", "Rifle de caza", ItemRarity.UltraRare, 300,
                damage: 60f, range: 90f, interval: 0.7f, magazine: 5, reload: 2.5f);

            // Resto del arsenal. La munición total es infinita, pero cada arma tiene su cargador y
            // su tiempo de recarga. Los precios que se escriben aquí son la base: CreateWeapon les
            // aplica el recargo de armas. El comentario de cada línea es el daño por segundo
            // aproximado sin contar recargas (la pistola inicial ronda 80).
            //
            // Pistolas y subfusiles: poco daño por bala, mucha cadencia, corto alcance.
            CreateWeapon("weapon_auto_pistol", "Pistola automática", ItemRarity.Rare, 180,
                damage: 14f, range: 50f, interval: 0.11f, automatic: true, spread: 2.5f, magazine: 18, reload: 1.4f);          // ~127
            CreateWeapon("weapon_magnum", "Revólver Magnum .44", ItemRarity.UltraRare, 320,
                damage: 75f, range: 70f, interval: 0.6f, magazine: 6, reload: 2.2f);                                           // ~125, un zombi normal por bala
            CreateWeapon("weapon_uzi", "Uzi", ItemRarity.Rare, 260,
                damage: 11f, range: 40f, interval: 0.07f, automatic: true, spread: 3.5f, magazine: 32, reload: 1.8f);          // ~157

            // Escopetas: varios perdigones por disparo; de cerca rinden mucho, de lejos casi nada.
            CreateWeapon("weapon_shotgun_single", "Escopeta monotiro", ItemRarity.Common, 150,
                damage: 14f, range: 22f, interval: 0.3f, pellets: 8, spread: 6f, magazine: 1, reload: 1.2f);                  // ~86
            CreateWeapon("weapon_shotgun_pump", "Escopeta de corredera", ItemRarity.Rare, 280,
                damage: 14f, range: 24f, interval: 0.85f, pellets: 8, spread: 5.5f, magazine: 6, reload: 3.0f);               // ~132
            CreateWeapon("weapon_shotgun_double", "Escopeta de doble cañón", ItemRarity.UltraRare, 380,
                damage: 13f, range: 20f, interval: 0.25f, pellets: 8, spread: 7f, magazine: 2, reload: 2.0f);                 // dos cartuchos seguidos y a recargar
            CreateWeapon("weapon_shotgun_semi", "Escopeta semiautomática", ItemRarity.Epic, 520,
                damage: 13f, range: 24f, interval: 0.45f, pellets: 8, spread: 5.5f, magazine: 8, reload: 3.2f);               // ~231
            CreateWeapon("weapon_shotgun_auto", "Escopeta automática", ItemRarity.Legendary, 800,
                damage: 12f, range: 22f, interval: 0.28f, automatic: true, pellets: 7, spread: 6f, magazine: 12, reload: 3.5f); // ~300

            // Rifles de asalto: automáticos, precisos y de largo alcance.
            CreateWeapon("weapon_ak47", "AK-47", ItemRarity.Epic, 650,
                damage: 28f, range: 70f, interval: 0.11f, automatic: true, spread: 2f, magazine: 30, reload: 2.4f);            // ~255
            CreateWeapon("weapon_m4", "M4", ItemRarity.Epic, 680,
                damage: 24f, range: 75f, interval: 0.085f, automatic: true, spread: 1.2f, magazine: 30, reload: 2.2f);         // ~282
            CreateWeapon("weapon_xm7", "SIG XM7", ItemRarity.Legendary, 950,
                damage: 38f, range: 85f, interval: 0.1f, automatic: true, spread: 1f, magazine: 20, reload: 2.6f);             // ~380

            // Francotirador pesado: tumba a un zombi fuerte (240) de un tiro, pero dispara despacio.
            CreateWeapon("weapon_heavy_sniper", "Francotirador pesado", ItemRarity.Legendary, 900,
                damage: 260f, range: 150f, interval: 1.6f, magazine: 5, reload: 3.5f);                                         // ~162

            // id, nombre, color maduro, s. hasta 1.ª cosecha, s. de rebrote, rendimiento mín-máx, altura,
            // precio de la semilla, precio de venta del fruto.
            var crops = new List<CropDefinition>
            {
                CreateCrop("strawberry", "Fresa", new Color(0.86f, 0.12f, 0.20f), 60f, 45f, 2, 3, 0.40f, 5, 3, ItemRarity.Common),
                CreateCrop("potato", "Papa", new Color(0.76f, 0.62f, 0.38f), 120f, 90f, 2, 4, 0.35f, 8, 4, ItemRarity.Common),
                CreateCrop("tomato", "Tomate", new Color(0.92f, 0.28f, 0.12f), 90f, 60f, 2, 3, 0.80f, 10, 5, ItemRarity.Rare),
                CreateCrop("wheat", "Trigo", new Color(0.90f, 0.76f, 0.30f), 75f, 60f, 1, 2, 0.90f, 6, 6, ItemRarity.Common),
                CreateCrop("sunflower", "Girasol", new Color(0.98f, 0.82f, 0.10f), 100f, 80f, 1, 1, 1.40f, 12, 14, ItemRarity.Rare),

                // Árboles frutales: tardan más, pero dan más fruta cada vez.
                CreateCrop("apple", "Manzana", new Color(0.80f, 0.15f, 0.15f), 180f, 90f, 3, 5, 2.20f, 25, 6, ItemRarity.Epic),
                CreateCrop("orange", "Naranja", new Color(0.98f, 0.55f, 0.10f), 180f, 90f, 3, 5, 2.20f, 25, 6, ItemRarity.Epic),
                CreateCrop("lemon", "Limón", new Color(0.95f, 0.90f, 0.25f), 160f, 80f, 3, 5, 2.00f, 22, 5, ItemRarity.UltraRare),
            };

            // Rareza según cuánto oro por minuto deja el cultivo: fresa, papa y trigo son comunes;
            // tomate y girasol, raros; el limonero, ultrararo; manzano y naranjo, épicos.

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

            // Rareza del resto: lo que no se nombra aquí es común (muro, valla, antorcha, cinta,
            // martillo, pozo y las herramientas iniciales).
            SetRarity(ItemRarity.Rare, gate, campfire, lamp, windmill, chicken, egg, smallGenerator);
            SetRarity(ItemRarity.UltraRare, tnt, sheep, wool);
            SetRarity(ItemRarity.Epic, generator);

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
            float regrowSeconds, int yieldMin, int yieldMax, float height, int seedPrice, int produceSellPrice,
            ItemRarity rarity)
        {
            Color seedColor = Color.Lerp(color, new Color(0.45f, 0.36f, 0.24f), 0.6f);
            ItemDefinition seed = CreateItem($"seed_{id}", $"Semillas de {name.ToLowerInvariant()}",
                ItemKind.Seed, 99, seedColor);
            ItemDefinition produce = CreateItem($"produce_{id}", name, ItemKind.Produce, 99, color);
            seed.ConfigureRarity(rarity);
            produce.ConfigureRarity(rarity);
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

        /// <summary>Recargo sobre el precio base de armas y torretas (1,5 = un 50 % más caras).</summary>
        private const float CombatPriceFactor = 1.5f;

        /// <summary>Precio con el recargo, redondeado a múltiplos de 5.</summary>
        private static int WithCombatMarkup(int basePrice)
        {
            return Mathf.RoundToInt(basePrice * CombatPriceFactor / 5f) * 5;
        }

        private static ItemDefinition CreateWeapon(string id, string name, ItemRarity rarity, int price, float damage,
            float range, float interval, bool automatic = false, int pellets = 1, float spread = 0f, int magazine = 0,
            float reload = 0f)
        {
            ItemDefinition weapon = CreateItem(id, name, ItemKind.Weapon, 1, new Color(0.25f, 0.25f, 0.28f));
            weapon.ConfigureWeapon(damage, range, interval, automatic, pellets, spread);
            weapon.ConfigureMagazine(magazine, reload);
            weapon.ConfigurePrices(buy: WithCombatMarkup(price), sell: 0);
            weapon.ConfigureRarity(rarity);
            return weapon;
        }

        private static void SetRarity(ItemRarity rarity, params ItemDefinition[] items)
        {
            foreach (ItemDefinition item in items)
            {
                item.ConfigureRarity(rarity);
                EditorUtility.SetDirty(item);
            }
        }

        private static readonly List<ItemDefinition> createdItems = new List<ItemDefinition>();

        private static ItemDefinition CreateItem(string id, string name, ItemKind kind, int maxStack,
            Color color, ToolKind tool = ToolKind.None, GameObject placedPrefab = null, Vector2Int footprint = default)
        {
            var item = EditorPaths.LoadOrCreate<ItemDefinition>($"{Root}/Items/Item_{id}.asset");
            item.Configure(name, kind, maxStack, footprint);
            item.ConfigureExtras(id, color, tool, placedPrefab);

            // Los assets se reutilizan entre generaciones: lo que no se vuelva a fijar más abajo
            // tiene que volver a su valor por defecto.
            item.ConfigureRarity(ItemRarity.Common);
            item.ConfigureRepairCost(0);
            item.ConfigureMagazine(0, 0f);
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
            consumer.ConfigureDemand(1f);
            root.AddComponent<PoweredLight>().Configure(consumer, light);
            return SavePlaceable(root);
        }

        /// <summary>Generador: fuente de energía que se puede colocar lejos de la caravana.</summary>
        private static GameObject CreateGeneratorPrefab(GameSession session, string prefabName, float maxHealth,
            float radius, float capacity, float size, Color color)
        {
            GameObject root = NewPlaceable(prefabName, maxHealth, session);
            AddBox(root, "Body", new Vector3(0f, size * 0.5f, 0f), new Vector3(size, size, size), prefabName, color);
            root.AddComponent<PowerSource>().Configure(null, radius, default, capacity);
            return SavePlaceable(root);
        }

        /// <summary>Torreta: base fija, cabeza giratoria con cañón, luz de estado y rayo de disparo.</summary>
        private static GameObject CreateTurretPrefab(GameSession session, string prefabName, float maxHealth,
            float powerDemand, Color headColor, Color beamColor, float barrelLength, float barrelWidth,
            System.Action<Turret> configureStats)
        {
            GameObject root = NewPlaceable(prefabName, maxHealth, session);
            AddBox(root, "Base", new Vector3(0f, 0.35f, 0f), new Vector3(0.8f, 0.7f, 0.8f), "Metal", new Color(0.45f, 0.47f, 0.50f));

            var head = new GameObject("Head");
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            AddBox(head, "Body", Vector3.zero, new Vector3(0.45f, 0.35f, 0.45f), prefabName + "Head", headColor, false);
            AddBox(head, "Barrel", new Vector3(0f, 0f, 0.2f + barrelLength * 0.5f),
                new Vector3(barrelWidth, barrelWidth, barrelLength), prefabName + "Head", headColor, false);

            GameObject status = AddBox(root, "StatusLight", new Vector3(0f, 0.75f, -0.3f), new Vector3(0.14f, 0.14f, 0.14f), "Tint", Color.white, false);
            GameObject beam = AddBox(root, "Beam", Vector3.zero, new Vector3(0.05f, 0.05f, 1f), prefabName + "Beam", beamColor, false);
            beam.SetActive(false);

            var consumer = root.AddComponent<PowerConsumer>();
            consumer.ConfigureDemand(powerDemand);
            var turret = root.AddComponent<Turret>();
            turret.Configure(consumer, head.transform, beam.transform, status.GetComponent<Renderer>());
            configureStats(turret);
            return SavePlaceable(root);
        }

        /// <summary>Crea el prefab y el ítem de una torreta con sus números.</summary>
        private static ItemDefinition CreateTurret(GameSession session, string id, string prefabName, string name,
            ItemRarity rarity, int price, int repairCost, float maxHealth, float powerDemand, Color headColor,
            Color beamColor, float barrelLength, float barrelWidth, System.Action<Turret> configureStats)
        {
            GameObject prefab = CreateTurretPrefab(session, prefabName, maxHealth, powerDemand, headColor, beamColor,
                barrelLength, barrelWidth, configureStats);

            int finalPrice = WithCombatMarkup(price);
            ItemDefinition item = CreatePlaceableItem(id, name, headColor, prefab, buy: finalPrice, sell: finalPrice * 2 / 5);
            item.ConfigureRarity(rarity);
            item.ConfigureRepairCost(repairCost);
            return item;
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
