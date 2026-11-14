using System.Collections.Generic;
using System.IO;
using DeadAcres.Building;
using DeadAcres.DayNight;
using DeadAcres.Farming;
using DeadAcres.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
        private static readonly Vector3 GridOrigin = new Vector3(-16f, 0f, 4f);
        private const int GridSize = 32;
        private const float GridCellSize = 1f;

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
            CreateProps(propMaterial);

            var systems = new GameObject("GameSystems");
            var cycle = systems.AddComponent<DayNightCycle>();
            cycle.Configure(data.DayNight, data.PhaseChanged, data.NightCompleted, sun.transform);
            systems.AddComponent<DayNightLighting>().Configure(sun, moon);

            var worldObject = new GameObject("FarmWorld");
            worldObject.transform.position = GridOrigin;
            var world = worldObject.AddComponent<FarmWorld>();
            world.Configure(data.Session, GridSize, GridSize, GridCellSize);
            var farm = worldObject.AddComponent<FarmArea>();
            farm.Configure(world, data.Crops, cycle);

            var farmView = new GameObject("FarmView").AddComponent<FarmAreaView>();
            farmView.Configure(farm, tintMaterial);

            PlayerInteractor interactor = CreatePlayer(data, world, farm, cycle, ghostValid, ghostInvalid,
                out PlayerInventory inventory);

            var highlight = new GameObject("TargetCellHighlight").AddComponent<TargetCellHighlight>();
            highlight.Configure(interactor, tintMaterial);

            HudBuilder.Build(cycle, data.PhaseChanged, inventory, interactor, world);

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

            player.AddComponent<PlacementController>()
                .Configure(input, inventory, world, camera, ghostValid, ghostInvalid);

            var interactor = player.AddComponent<PlayerInteractor>();
            interactor.Configure(input, inventory, cameraObject.transform, farm, cycle);
            return interactor;
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

        private static void CreateProps(Material material)
        {
            var props = new GameObject("Props").transform;

            // Marcador de dónde irá la caravana (lo que defenderás de noche).
            CreateBox("RV (placeholder)", props, material, new Vector3(-7f, 1.4f, -2f), new Vector3(2.6f, 2.8f, 7f));

            // Postes en las esquinas de la zona cultivable, como referencia visual.
            float side = GridSize * GridCellSize;
            for (int x = 0; x <= 1; x++)
            {
                for (int z = 0; z <= 1; z++)
                {
                    // Justo por fuera de la rejilla, para no tapar ninguna celda.
                    var offset = new Vector3(x == 0 ? -0.25f : side + 0.25f, 0.7f, z == 0 ? -0.25f : side + 0.25f);
                    CreateBox("Grid corner post", props, material, GridOrigin + offset, new Vector3(0.3f, 1.4f, 0.3f));
                }
            }
        }

        private static void CreateBox(string name, Transform parent, Material material, Vector3 position, Vector3 scale)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
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
