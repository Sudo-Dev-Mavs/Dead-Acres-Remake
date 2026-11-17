using System.Collections.Generic;
using System.IO;
using DeadAcres.Audio;
using DeadAcres.Building;
using DeadAcres.Combat;
using DeadAcres.DayNight;
using DeadAcres.Economy;
using DeadAcres.Enemies;
using DeadAcres.Farming;
using DeadAcres.Items;
using DeadAcres.Loot;
using DeadAcres.Match;
using DeadAcres.Mods;
using DeadAcres.Player;
using DeadAcres.Saving;
using DeadAcres.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DeadAcres.EditorTools
{
    /// <summary>
    /// Genera una escena jugable de pruebas con formas básicas: suelo, sol y luna, rejilla de granja,
    /// jugador en primera persona (con colocación de objetos) y HUD, todo ya cableado.
    /// Menú: Dead Acres → Crear escena greybox.
    /// La escena se regenera entera cada vez: los cambios hechos a mano en ella se pierden.
    /// </summary>
    public static class GreyboxSceneBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string ScenePath = SceneFolder + "/Greybox.unity";
        private const string MenuScenePath = SceneFolder + "/MainMenu.unity";

        // FarmWorld marca la esquina (0,0) de la rejilla: 32×32 celdas de 1 m delante del jugador.
        // La rejilla cubre todo el suelo (300×300 m): se puede arar y colocar en cualquier parte,
        // salvo alrededor de la caravana.
        private const int GridSize = 300;
        private const float GridCellSize = 1f;
        private static readonly Vector3 GridOrigin = new Vector3(-GridSize * 0.5f, 0f, -GridSize * 0.5f);

        private static readonly Vector3 RvCenter = new Vector3(-7f, 1.4f, -2f);
        private static readonly Vector3 RvSize = new Vector3(2.6f, 2.8f, 7f);

        /// <summary>Celdas alrededor de la caravana en las que no se puede poner nada.</summary>
        private const int RvNoBuildMargin = 3;

        /// <summary>Alcance de la energía de la caravana, en metros desde su borde.</summary>
        private const float RvPowerRange = 6f;

        /// <summary>Consumo que aguanta la caravana: unas pocas torretas sencillas. Para más, generadores.</summary>
        private const float RvPowerCapacity = 10f;

        [MenuItem("Dead Acres/Crear escena greybox")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Dead Acres: sal del modo Play antes de generar la escena.");
                return;
            }

            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Dead Acres",
                    $"Ya existe {ScenePath}. Se va a regenerar entera y se perderán los cambios hechos a mano.",
                    "Regenerar", "Cancelar"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            DefaultDataGenerator.GeneratedData data = DefaultDataGenerator.Generate();

            Material groundMaterial = EditorPaths.LoadOrCreateMaterial("Greybox_Ground", new Color(0.33f, 0.55f, 0.27f));
            Material propMaterial = EditorPaths.LoadOrCreateMaterial("Greybox_Prop", new Color(0.62f, 0.62f, 0.66f));
            Material tintMaterial = EditorPaths.LoadOrCreateMaterial("Greybox_Tint", Color.white);
            Material ghostValid = EditorPaths.LoadOrCreateGhostMaterial("Ghost_Valid", new Color(0.2f, 0.9f, 0.3f, 0.45f));
            Material ghostInvalid = EditorPaths.LoadOrCreateGhostMaterial("Ghost_Invalid", new Color(0.95f, 0.2f, 0.2f, 0.45f));

            // Primero el menú: así la escena que queda abierta al terminar es la de juego.
            BuildMainMenuScene(data);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Light sun = CreateDirectionalLight("Sun", new Color(1f, 0.96f, 0.86f), 1.3f, LightShadows.Soft,
                Quaternion.Euler(50f, -30f, 0f));
            Light moon = CreateDirectionalLight("Moon", new Color(0.55f, 0.65f, 1f), 0f, LightShadows.None,
                Quaternion.Euler(60f, 140f, 0f));

            // Luz ambiente plana: DayNightLighting la oscurece de noche.
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.72f);

            CreateGround(groundMaterial);
            Health rvHealth = CreateProps(propMaterial, data);

            var systems = new GameObject("GameSystems");
            var waves = systems.AddComponent<WaveDirector>();
            var cycle = systems.AddComponent<DayNightCycle>();
            cycle.Configure(data.DayNight, data.PhaseChanged, data.NightCompleted, sun.transform, waves);
            systems.AddComponent<DayNightLighting>().Configure(sun, moon);

            var worldObject = new GameObject("FarmWorld");
            worldObject.transform.position = GridOrigin;
            var world = worldObject.AddComponent<FarmWorld>();
            world.Configure(data.Session, GridSize, GridSize, GridCellSize);

            rvHealth.gameObject.AddComponent<PowerSource>()
                .Configure(world, RvPowerRange, new Vector2(RvSize.x * 0.5f, RvSize.z * 0.5f), RvPowerCapacity);
            rvHealth.gameObject.AddComponent<NoBuildZone>().Configure(world, RvNoBuildMargin);
            CreateNoBuildMarker();
            var farm = worldObject.AddComponent<FarmArea>();
            farm.Configure(world, data.Crops, cycle);

            var farmView = new GameObject("FarmView").AddComponent<FarmAreaView>();
            farmView.Configure(farm, tintMaterial);

            PlayerInteractor interactor = CreatePlayer(data, world, farm, cycle, ghostValid, ghostInvalid,
                out PlayerInventory inventory);

            var highlight = new GameObject("TargetCellHighlight").AddComponent<TargetCellHighlight>();
            highlight.Configure(interactor, tintMaterial);

            Shopkeeper jack = CreateShopkeeper(data);

            var playerHealth = interactor.GetComponent<Health>();

            // Por mucho que crezca el daño de los zombis, un solo golpe nunca quita más del 99 %
            // de la vida máxima del jugador ni de la caravana.
            playerHealth.ConfigureHitCap(0.99f);
            rvHealth.ConfigureHitCap(0.99f);
            var playerInput = interactor.GetComponent<PlayerInputReader>();

            // Los zombis aparecen en un círculo alrededor de la caravana y van a por ella.
            waves.Configure(data.Waves, data.Session, data.PhaseChanged, data.NightCompleted, data.ZombiePrefab,
                rvHealth, new List<Health> { playerHealth }, rvHealth.transform, 45f, cycle);
            waves.ConfigureCrypt(data.StrongZombiePrefab, CreateCrypt(world));

            // Tipos de zombi de la oleada y un mini jefe al azar cada 10 noches.
            waves.ConfigureRoster(data.ZombieRoster, 10);
            CreateAnimals(data);
            CreateCars(data, world);

            var match = systems.AddComponent<MatchDirector>();
            match.Configure(data.Session, cycle, data.NightCompleted, rvHealth, playerHealth, playerInput,
                data.RvDestroyed, data.Victory);

            HudBuilder.Build(cycle, data.PhaseChanged, inventory, interactor, world,
                interactor.GetComponent<PlayerWallet>(), jack, playerInput, playerHealth, rvHealth, match, waves,
                interactor.GetComponent<PlayerBuildTools>());

            // Guardado automático al amanecer y carga al elegir "Continuar" en el menú.
            systems.AddComponent<SaveGameController>().Configure(data.Session, cycle, data.PhaseChanged, data.Items,
                data.Crops, world, farm, inventory, interactor.GetComponent<PlayerWallet>(), playerHealth, rvHealth,
                match);

            // Mods: armas extra leídas de las carpetas Mods al arrancar la partida.
            systems.AddComponent<ModContentLoader>().Configure(data.Items, data.Shop);

            // Reparte el cupo de armas entre los autos: como mucho una en toda la partida.
            systems.AddComponent<LootDirector>();

            new GameObject("PauseMenu").AddComponent<PauseMenuView>().Configure(playerInput, cycle, match);

            new GameObject("Audio").AddComponent<GameAudio>().Configure(data.Audio, data.PhaseChanged,
                interactor.GetComponent<PlayerWeapon>(), interactor, inventory,
                interactor.GetComponent<PlayerBuildTools>(), interactor.GetComponent<PlayerWallet>(), playerHealth,
                rvHealth, waves, world);

            // La tienda tiene botones: hace falta un EventSystem con el módulo del Input System.
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            EditorPaths.EnsureFolder(SceneFolder);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildScenes(MenuScenePath, ScenePath);
            PlayFromMainMenu.Apply();
            AssetDatabase.SaveAssets();

            Debug.Log($"Dead Acres: escenas generadas en {SceneFolder}. Play arranca en el menú principal " +
                      "(se puede cambiar en Dead Acres > Play empieza en el menú principal).");
        }

        /// <summary>
        /// El menú principal: detrás del panel se ve una partida de demostración (caravana,
        /// muros, torretas, cultivos y zombis que llegan y caen) con la cámara dando vueltas.
        /// </summary>
        private static void BuildMainMenuScene(DefaultDataGenerator.GeneratedData data)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Luz de atardecer: se ve todo, pero ya con ambiente de noche cerca.
            Light sun = CreateDirectionalLight("Sun", new Color(1f, 0.72f, 0.50f), 1f, LightShadows.Soft,
                Quaternion.Euler(20f, -40f, 0f));
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.38f, 0.48f);

            CreateGround(EditorPaths.LoadOrCreateMaterial("Greybox_Ground", new Color(0.33f, 0.55f, 0.27f)));
            Material propMaterial = EditorPaths.LoadOrCreateMaterial("Greybox_Prop", new Color(0.62f, 0.62f, 0.66f));

            var demo = new GameObject("Demo").transform;
            GameObject rv = CreateBox("RV (placeholder)", demo, propMaterial, new Vector3(0f, 1.4f, 0f), RvSize);
            var rvHealth = rv.AddComponent<Health>();
            rvHealth.Configure(Faction.PlayerSide, 100000f, data.Session);

            // Solo para dar energía a las torretas de la demostración: sobra capacidad.
            var worldObject = new GameObject("FarmWorld");
            worldObject.transform.position = new Vector3(-40f, 0f, -40f);
            var world = worldObject.AddComponent<FarmWorld>();
            world.Configure(data.Session, 80, 80, 1f);
            rv.AddComponent<PowerSource>().Configure(world, 40f, default, 1000f);

            var repaired = new List<Health>();
            CreateDemoDefenses(data, demo, world, repaired);
            CreateDemoFarm(demo);

            var director = new GameObject("DemoDirector").AddComponent<MenuDemoDirector>();
            director.Configure(new List<ZombieBrain> { data.ZombiePrefab, data.StrongZombiePrefab }, rvHealth, repaired, 30f);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.20f, 0.16f, 0.22f);
            camera.farClipPlane = 200f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<MenuCameraOrbit>().Configure(new Vector3(0f, 1.5f, 0f), 19f, 8f);

            new GameObject("MainMenu").AddComponent<MainMenuView>().Configure(data.Session, data.Difficulties, data.Modes);

            if (data.Audio != null && data.Audio.MusicDay != null)
            {
                var music = new GameObject("Music").AddComponent<AudioSource>();
                music.clip = data.Audio.MusicDay;
                music.loop = true;
                music.playOnAwake = true;
                music.volume = 0.35f;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            EditorPaths.EnsureFolder(SceneFolder);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        /// <summary>Torretas alrededor de la caravana, cuatro tramos de muro con huecos en las esquinas y luces.</summary>
        private static void CreateDemoDefenses(DefaultDataGenerator.GeneratedData data, Transform parent, FarmWorld world,
            List<Health> repaired)
        {
            PlaceDemoObject(data, "placeable_turret_auto", parent, new Vector3(6f, 0f, 6f), 0f, world, repaired);
            PlaceDemoObject(data, "placeable_turret", parent, new Vector3(-6f, 0f, 6f), 0f, world, repaired);
            PlaceDemoObject(data, "placeable_turret_electric", parent, new Vector3(6f, 0f, -6f), 0f, world, repaired);
            PlaceDemoObject(data, "placeable_turret_shotgun", parent, new Vector3(-6f, 0f, -6f), 0f, world, repaired);
            PlaceDemoObject(data, "placeable_turret_sniper", parent, new Vector3(0f, 0f, 8f), 0f, world, repaired);
            PlaceDemoObject(data, "placeable_turret_double", parent, new Vector3(0f, 0f, -8f), 0f, world, repaired);

            const float ring = 11f;
            for (int i = -5; i <= 5; i++)
            {
                PlaceDemoObject(data, "placeable_wood_wall", parent, new Vector3(i, 0f, ring), 0f, world, repaired);
                PlaceDemoObject(data, "placeable_wood_wall", parent, new Vector3(i, 0f, -ring), 0f, world, repaired);
                PlaceDemoObject(data, "placeable_wood_wall", parent, new Vector3(ring, 0f, i), 90f, world, repaired);
                PlaceDemoObject(data, "placeable_wood_wall", parent, new Vector3(-ring, 0f, i), 90f, world, repaired);
            }

            PlaceDemoObject(data, "placeable_torch", parent, new Vector3(9f, 0f, 9f), 0f, world, repaired);
            PlaceDemoObject(data, "placeable_torch", parent, new Vector3(-9f, 0f, 9f), 0f, world, repaired);
            PlaceDemoObject(data, "placeable_torch", parent, new Vector3(9f, 0f, -9f), 0f, world, repaired);
            PlaceDemoObject(data, "placeable_torch", parent, new Vector3(-9f, 0f, -9f), 0f, world, repaired);
            PlaceDemoObject(data, "placeable_campfire", parent, new Vector3(-3.5f, 0f, 3f), 0f, world, repaired);
            PlaceDemoObject(data, "placeable_lamp", parent, new Vector3(3f, 0f, 4.5f), 0f, world, repaired);
            PlaceDemoObject(data, "placeable_well", parent, new Vector3(-4f, 0f, -4f), 0f, world, repaired);
            PlaceDemoObject(data, "placeable_windmill", parent, new Vector3(-7.5f, 0f, 0f), 90f, world, repaired);
        }

        /// <summary>
        /// Pone en la demostración el prefab de un ítem colocable, sin pasar por la rejilla: con
        /// energía si la necesita, en silencio y apuntado para que se repare solo.
        /// </summary>
        private static void PlaceDemoObject(DefaultDataGenerator.GeneratedData data, string itemId, Transform parent,
            Vector3 position, float yaw, FarmWorld world, List<Health> repaired)
        {
            if (!data.Items.TryGet(itemId, out ItemDefinition item) || item.PlacedPrefab == null)
            {
                Debug.LogWarning($"Dead Acres: la demostración del menú no encontró el ítem {itemId}.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(item.PlacedPrefab, parent);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            if (instance.TryGetComponent(out PowerConsumer consumer)) consumer.ConfigureWorld(world);
            foreach (AudioSource source in instance.GetComponentsInChildren<AudioSource>())
            {
                // En el menú solo suena la música.
                source.mute = true;
            }

            if (instance.TryGetComponent(out Health health)) repaired.Add(health);
        }

        /// <summary>Un huerto y un par de árboles, solo de adorno (cubos sin lógica de cultivo).</summary>
        private static void CreateDemoFarm(Transform parent)
        {
            Material soil = EditorPaths.LoadOrCreateMaterial("Greybox_DemoSoil", new Color(0.36f, 0.25f, 0.16f));
            Material red = EditorPaths.LoadOrCreateMaterial("Greybox_DemoCropRed", new Color(0.86f, 0.12f, 0.20f));
            Material yellow = EditorPaths.LoadOrCreateMaterial("Greybox_DemoCropYellow", new Color(0.98f, 0.82f, 0.10f));
            Material leaves = EditorPaths.LoadOrCreateMaterial("Greybox_DemoLeaves", new Color(0.20f, 0.50f, 0.22f));
            Material trunk = EditorPaths.LoadOrCreateMaterial("Greybox_DemoTrunk", new Color(0.40f, 0.27f, 0.16f));

            for (int x = 0; x < 4; x++)
            {
                for (int z = 0; z < 6; z++)
                {
                    var cell = new Vector3(3.5f + x, 0f, -3f + z);
                    AddDemoBox("Soil", parent, soil, cell + Vector3.up * 0.03f, new Vector3(0.95f, 0.06f, 0.95f));

                    // Alturas distintas: como si unas plantas fueran más adelantadas que otras.
                    float height = 0.25f + 0.15f * ((x * 3 + z * 2) % 4);
                    AddDemoBox("Crop", parent, (x + z) % 3 == 0 ? yellow : red, cell + Vector3.up * (0.06f + height * 0.5f),
                        new Vector3(0.45f, height, 0.45f));
                }
            }

            foreach (Vector3 spot in new[] { new Vector3(-4f, 0f, 7.5f), new Vector3(4.5f, 0f, -7.5f), new Vector3(-8.5f, 0f, -6f) })
            {
                AddDemoBox("Trunk", parent, trunk, spot + Vector3.up * 0.9f, new Vector3(0.35f, 1.8f, 0.35f));
                AddDemoBox("Leaves", parent, leaves, spot + Vector3.up * 2.4f, new Vector3(1.6f, 1.4f, 1.6f));
            }
        }

        private static void AddDemoBox(string name, Transform parent, Material material, Vector3 position, Vector3 scale)
        {
            GameObject box = CreateDecoration(name, scale, material);
            box.transform.SetParent(parent, false);
            box.transform.position = position;
        }

        private static PlayerInteractor CreatePlayer(DefaultDataGenerator.GeneratedData data, FarmWorld world,
            FarmArea farm, DayNightCycle cycle, Material ghostValid, Material ghostInvalid,
            out PlayerInventory inventory)
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.1f, 0f);

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            // El pivote rota en vertical; el cuerpo, en horizontal.
            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = new Vector3(0f, 1.62f, 0f);

            var cameraObject = new GameObject("PlayerCamera") { tag = "MainCamera" };
            cameraObject.transform.SetParent(pivot, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 500f;
            camera.fieldOfView = 70f;
            cameraObject.AddComponent<AudioListener>();

            var input = player.AddComponent<PlayerInputReader>();
            player.AddComponent<FirstPersonController>().Configure(input, pivot);

            inventory = player.AddComponent<PlayerInventory>();
            inventory.Configure(input, data.StartingItems);
            player.AddComponent<PlayerWallet>().Configure(data.StartingGold);
            player.AddComponent<Health>().Configure(Faction.PlayerSide, 100f, data.Session);

            // Arma provisional: una caja en la esquina de la vista y una marca en el impacto.
            GameObject viewModel = CreateDecoration("WeaponViewModel", new Vector3(0.08f, 0.12f, 0.4f),
                EditorPaths.LoadOrCreateMaterial("Greybox_Weapon", new Color(0.22f, 0.22f, 0.25f)));
            viewModel.transform.SetParent(cameraObject.transform, false);
            viewModel.transform.localPosition = new Vector3(0.28f, -0.24f, 0.55f);

            GameObject impact = CreateDecoration("ImpactMarker", new Vector3(0.12f, 0.12f, 0.12f),
                EditorPaths.LoadOrCreateMaterial("Greybox_Impact", new Color(1f, 0.85f, 0.2f)));

            player.AddComponent<PlayerWeapon>().Configure(input, inventory, cameraObject.transform, viewModel, impact);
            player.AddComponent<PlayerBuildTools>().Configure(input, inventory, cameraObject.transform);

            player.AddComponent<PlacementController>()
                .Configure(input, inventory, world, camera, ghostValid, ghostInvalid);

            var interactor = player.AddComponent<PlayerInteractor>();
            interactor.Configure(input, inventory, cameraObject.transform, farm, cycle);
            return interactor;
        }

        /// <summary>
        /// Animales de regalo: una oveja y dos gallinas, cada uno por su lado a unos 25-30 m de la
        /// caravana. Se les puede hacer un corral ahí o traerlos con el martillo. El resto se compra.
        /// </summary>
        private static void CreateAnimals(DefaultDataGenerator.GeneratedData data)
        {
            var parent = new GameObject("Animals").transform;
            SpawnAnimal(data.SheepItem, parent, new Vector3(12.5f, 0f, 18.5f));
            SpawnAnimal(data.ChickenItem, parent, new Vector3(-26.5f, 0f, 12.5f));
            SpawnAnimal(data.ChickenItem, parent, new Vector3(16.5f, 0f, -24.5f));
        }

        private static void SpawnAnimal(ItemDefinition item, Transform parent, Vector3 position)
        {
            var animal = (GameObject)PrefabUtility.InstantiatePrefab(item.PlacedPrefab, parent);
            animal.transform.position = position;

            // No ocupa celda, pero el martillo lo recoge como ítem para poder moverlo.
            animal.AddComponent<PlacedObject>().ConfigureItem(item);
        }

        /// <summary>
        /// La cripta: a unos 50 m de la caravana, con la puerta mirando hacia ella. De noche salen
        /// de ahí los zombis fuertes.
        /// </summary>
        /// <returns>La puerta: de dónde salen y hacia dónde miran.</returns>
        private static Transform CreateCrypt(FarmWorld world)
        {
            var position = new Vector3(-45f, 0f, 35f);
            var toRv = new Vector3(RvCenter.x - position.x, 0f, RvCenter.z - position.z);

            var crypt = new GameObject("Crypt");
            crypt.transform.SetPositionAndRotation(position, Quaternion.LookRotation(toRv));

            Material stone = EditorPaths.LoadOrCreateMaterial("Greybox_Crypt", new Color(0.36f, 0.36f, 0.40f));
            GameObject building = AddPart(crypt.transform, "Building", new Vector3(0f, 1.5f, 0f), new Vector3(5f, 3f, 6f),
                stone, true);
            AddPart(crypt.transform, "Roof", new Vector3(0f, 3.2f, 0f), new Vector3(5.6f, 0.4f, 6.6f), stone, false);
            AddPart(crypt.transform, "Doorway", new Vector3(0f, 1.1f, 3.01f), new Vector3(1.6f, 2.2f, 0.05f),
                EditorPaths.LoadOrCreateMaterial("Greybox_CryptDoor", new Color(0.05f, 0.03f, 0.08f)), false);

            // Delante de la puerta no se construye: es por donde salen.
            building.AddComponent<NoBuildZone>().Configure(world, 4);

            var glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(crypt.transform, false);
            glow.transform.localPosition = new Vector3(0f, 2.2f, 4f);
            glow.type = LightType.Point;
            glow.color = new Color(0.65f, 0.30f, 0.95f);
            glow.range = 12f;
            glow.intensity = 3f;

            var door = new GameObject("Door").transform;
            door.SetParent(crypt.transform, false);
            door.localPosition = new Vector3(0f, 0f, 3f);
            return door;
        }

        /// <summary>Autos abandonados repartidos por el mapa. El botín de todos es al azar.</summary>
        private static void CreateCars(DefaultDataGenerator.GeneratedData data, FarmWorld world)
        {
            var parent = new GameObject("Cars").transform;
            CreateCar(parent, world, "car_1", new Vector3(22f, 0f, -14f), 20f, data.CarLoot, new Color(0.55f, 0.20f, 0.18f));
            CreateCar(parent, world, "car_2", new Vector3(-30f, 0f, -24f), 75f, data.CarLoot, new Color(0.20f, 0.35f, 0.55f));
            CreateCar(parent, world, "car_3", new Vector3(38f, 0f, 30f), -30f, data.CarLoot, new Color(0.60f, 0.58f, 0.50f));
            CreateCar(parent, world, "car_4", new Vector3(-20f, 0f, 48f), 10f, data.CarLoot, new Color(0.25f, 0.45f, 0.30f));
            CreateCar(parent, world, "car_5", new Vector3(55f, 0f, -40f), 60f, data.CarLoot, new Color(0.15f, 0.15f, 0.17f));
        }

        private static void CreateCar(Transform parent, FarmWorld world, string id, Vector3 position, float yaw,
            LootTable loot, Color color)
        {
            var car = new GameObject($"Car ({id})");
            car.transform.SetParent(parent, false);
            car.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            // Un solo collider en la raíz: es lo que mira el rayo de interacción y lo que reserva celdas.
            var body = car.AddComponent<BoxCollider>();
            body.center = new Vector3(0f, 0.75f, 0f);
            body.size = new Vector3(1.9f, 1.5f, 4.2f);

            Material paint = EditorPaths.LoadOrCreateMaterial($"Greybox_Car_{id}", color);
            Material dark = EditorPaths.LoadOrCreateMaterial("Greybox_CarDark", new Color(0.10f, 0.10f, 0.12f));
            AddPart(car.transform, "Body", new Vector3(0f, 0.6f, 0f), new Vector3(1.9f, 0.8f, 4.2f), paint, false);
            AddPart(car.transform, "Cabin", new Vector3(0f, 1.3f, -0.2f), new Vector3(1.7f, 0.6f, 2f), dark, false);
            GameObject closed = AddPart(car.transform, "TrunkClosed", new Vector3(0f, 1.03f, -1.6f),
                new Vector3(1.7f, 0.06f, 0.9f), paint, false);
            GameObject open = AddPart(car.transform, "TrunkOpen", new Vector3(0f, 1.4f, -1.2f),
                new Vector3(1.7f, 0.06f, 0.9f), paint, false);
            open.transform.localRotation = Quaternion.Euler(-65f, 0f, 0f);

            car.AddComponent<LootContainer>().Configure(id, "auto", loot, closed, open);
            car.AddComponent<NoBuildZone>().Configure(world, 0);
        }

        /// <summary>Caja hija en coordenadas locales del padre (gira y se mueve con él).</summary>
        private static GameObject AddPart(Transform parent, string name, Vector3 localPosition, Vector3 size,
            Material material, bool withCollider)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            if (!withCollider) Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        /// <summary>Jack provisional: una cápsula junto a la caravana. De noche se oculta.</summary>
        private static Shopkeeper CreateShopkeeper(DefaultDataGenerator.GeneratedData data)
        {
            var jack = new GameObject("Jack");
            jack.transform.position = new Vector3(-4.6f, 0f, -1f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Visual";
            body.transform.SetParent(jack.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.GetComponent<Renderer>().sharedMaterial =
                EditorPaths.LoadOrCreateMaterial("Greybox_Jack", new Color(0.20f, 0.45f, 0.85f));

            var keeper = jack.AddComponent<Shopkeeper>();
            keeper.Configure(data.Shop, data.PhaseChanged, body);
            return keeper;
        }

        private static Light CreateDirectionalLight(string name, Color color, float intensity, LightShadows shadows,
            Quaternion rotation)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.rotation = rotation;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadows;
            return light;
        }

        private static void CreateGround(Material material)
        {
            // El plano de Unity mide 10×10 m: a escala 30 son 300×300 m.
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(30f, 1f, 30f);
            ground.GetComponent<Renderer>().sharedMaterial = material;
        }

        /// <returns>La vida de la caravana.</returns>
        private static Health CreateProps(Material material, DefaultDataGenerator.GeneratedData data)
        {
            var props = new GameObject("Props").transform;

            // La caravana: lo que se defiende de noche. Si llega a cero, se pierde.
            GameObject rv = CreateBox("RV (placeholder)", props, material, RvCenter, RvSize);
            var rvHealth = rv.AddComponent<Health>();
            rvHealth.Configure(Faction.PlayerSide, 600f, data.Session);

            return rvHealth;
        }

        /// <summary>
        /// Losa a ras de suelo que marca dónde no se puede construir: las mismas celdas que
        /// reserva <see cref="NoBuildZone"/> en la caravana.
        /// </summary>
        private static void CreateNoBuildMarker()
        {
            var grid = new PlacementGrid(GridSize, GridSize, GridOrigin, GridCellSize);
            RectInt cells = grid.CellsCovering(RvCenter - RvSize * 0.5f, RvCenter + RvSize * 0.5f, RvNoBuildMargin);

            Material material = EditorPaths.LoadOrCreateMaterial("Greybox_NoBuild", new Color(0.42f, 0.38f, 0.30f));
            GameObject marker = CreateDecoration("No-build zone",
                new Vector3(cells.width * GridCellSize, 0.04f, cells.height * GridCellSize), material);
            marker.transform.position = grid.RectCenterToWorld(cells) + Vector3.up * 0.02f;
        }

        /// <summary>Caja visual sin collider (no bloquea disparos ni el rayo de interacción).</summary>
        private static GameObject CreateDecoration(string name, Vector3 scale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.transform.localScale = scale;
            var renderer = box.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return box;
        }

        private static GameObject CreateBox(string name, Transform parent, Material material, Vector3 position, Vector3 scale)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        /// <summary>
        /// Deja estas escenas al principio de Build Settings, en este orden (la primera es la que
        /// arranca el juego compilado). Las demás que hubiera se conservan detrás.
        /// </summary>
        private static void SetBuildScenes(params string[] orderedPaths)
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in orderedPaths)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (System.Array.IndexOf(orderedPaths, existing.path) < 0)
                {
                    scenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
