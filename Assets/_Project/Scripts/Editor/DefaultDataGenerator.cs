using DeadAcres.Core.Events;
using DeadAcres.Core.Session;
using DeadAcres.DayNight;
using DeadAcres.Difficulty;
using DeadAcres.GameMode;
using UnityEditor;
using UnityEngine;

namespace DeadAcres.EditorTools
{
    /// <summary>
    /// Crea (o actualiza) los assets de configuración con los valores del GDD,
    /// para no teclearlos a mano en el Inspector.
    /// Menú: Dead Acres → Generar datos por defecto.
    /// </summary>
    public static class DefaultDataGenerator
    {
        private const string Root = "Assets/_Project/Data";

        [MenuItem("Dead Acres/Generar datos por defecto")]
        public static void Generate()
        {
            EnsureFolder(Root);
            EnsureFolder($"{Root}/Difficulty");
            EnsureFolder($"{Root}/GameModes");
            EnsureFolder($"{Root}/Events");

            var dayNight = LoadOrCreate<DayNightConfig>($"{Root}/DayNightConfig.asset");
            dayNight.Configure(dawn: 15f, firstDay: 360f, day: 240f, dusk: 30f, minNight: 180f, maxNight: 300f);

            var easy = CreateDifficulty("Easy", "Para aprender el juego. Los zombis caen antes.", 0.7f, 0.75f);
            var medium = CreateDifficulty("Medium", "La experiencia base.", 1f, 1f);
            CreateDifficulty("Hard", "Los zombis aguantan más y pegan más fuerte.", 1.5f, 1.2f);
            CreateDifficulty("Hell", "Solo para veteranos de la granja.", 2.2f, 1.6f);

            var campaign = LoadOrCreate<GameModeRules>($"{Root}/GameModes/GameMode_Campaign.asset");
            campaign.Configure("Campaña", plants: 100, objects: 100, nights: 5);

            var endless = LoadOrCreate<GameModeRules>($"{Root}/GameModes/GameMode_Endless.asset");
            endless.Configure("Endless", plants: 500, objects: 500, nights: 0);

            var session = LoadOrCreate<GameSession>($"{Root}/GameSession.asset");
            session.ConfigureDefaults(medium, campaign);

            LoadOrCreate<PhaseEventChannel>($"{Root}/Events/PhaseChangedChannel.asset");
            LoadOrCreate<NightResultEventChannel>($"{Root}/Events/NightCompletedChannel.asset");
            LoadOrCreate<VoidEventChannel>($"{Root}/Events/RvDestroyedChannel.asset");
            LoadOrCreate<VoidEventChannel>($"{Root}/Events/VictoryChannel.asset");

            foreach (var asset in new Object[] { dayNight, campaign, endless, session, easy, medium })
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Dead Acres: datos por defecto generados en {Root}.");
        }

        private static DifficultyProfile CreateDifficulty(string name, string flavor, float health, float damage)
        {
            var profile = LoadOrCreate<DifficultyProfile>($"{Root}/Difficulty/Difficulty_{name}.asset");
            profile.Configure(name, flavor, health, damage);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
