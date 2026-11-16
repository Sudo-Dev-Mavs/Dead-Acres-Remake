using System.Collections.Generic;
using System.IO;
using DeadAcres.Building;
using DeadAcres.Combat;
using DeadAcres.DayNight;
using DeadAcres.Economy;
using DeadAcres.Enemies;
using DeadAcres.Farming;
using DeadAcres.Items;
using DeadAcres.Match;
using DeadAcres.Player;
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
                .Configure(world, RvPowerRange, new Vector2(RvSize.x * 0.5f, RvSize.z * 0.5f));
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
            var playerInput = interactor.GetComponent<PlayerInputReader>();

            // Los zombis aparecen en un círculo alrededor de la caravana y van a por ella.
            waves.Configure(data.Waves, data.Session, data.PhaseChanged, data.NightCompleted, data.ZombiePrefab,
                rvHealth, new List<Health> { playerHealth }, rvHealth.transform, 45f, cycle);
            CreateAnimals(data);

            var match = systems.AddComponent<MatchDirector>();
            match.Configure(data.Session, cycle, data.NightCompleted, rvHealth, playerHealth, playerInput,
                data.RvDestroyed, data.Victory);

            HudBuilder.Build(cycle, data.PhaseChanged, inventory, interactor, world,
                interactor.GetComponent<PlayerWallet>(), jack, playerInput, playerHealth, rvHealth, match, waves,
                interactor.GetComponent<PlayerBuildTools>());

            // La tienda tiene botones: hace falta un EventSystem con el módulo del Input System.
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            EditorPaths.EnsureFolder(SceneFolder);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Dead Acres: escena generada en {ScenePath}. Pulsa Play para probarla.");
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
        /// Animales de regalo: una oveja y dos gallinas a unos 25 m de la caravana. Se les puede
        /// hacer un corral ahí o traerlos con el martillo. El resto se compra.
        /// </summary>
        private static void CreateAnimals(DefaultDataGenerator.GeneratedData data)
        {
            var parent = new GameObject("Animals").transform;
            SpawnAnimal(data.SheepItem, parent, new Vector3(11.5f, 0f, 16.5f));
            SpawnAnimal(data.ChickenItem, parent, new Vector3(9.5f, 0f, 14.5f));
            SpawnAnimal(data.ChickenItem, parent, new Vector3(13.5f, 0f, 14.5f));
        }

        private static void SpawnAnimal(ItemDefinition item, Transform parent, Vector3 position)
        {
            var animal = (GameObject)PrefabUtility.InstantiatePrefab(item.PlacedPrefab, parent);
            animal.transform.position = position;

            // No ocupa celda, pero el martillo lo recoge como ítem para poder moverlo.
            animal.AddComponent<PlacedObject>().ConfigureItem(item);
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

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
