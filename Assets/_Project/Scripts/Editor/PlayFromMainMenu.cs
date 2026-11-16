using UnityEditor;
using UnityEditor.SceneManagement;

namespace DeadAcres.EditorTools
{
    /// <summary>
    /// Hace que el botón Play del editor arranque siempre en el menú principal, esté abierta la
    /// escena que esté. Se puede apagar desde el menú Dead Acres para probar una escena suelta.
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayFromMainMenu
    {
        private const string MenuPath = "Dead Acres/Play empieza en el menú principal";
        private const string PrefKey = "DeadAcres.PlayFromMainMenu";
        private const string MenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";

        static PlayFromMainMenu()
        {
            // Con el editor recién abierto la base de assets puede no estar lista todavía.
            EditorApplication.delayCall += Apply;
        }

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        internal static void Apply()
        {
            SceneAsset menu = Enabled ? AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) : null;
            EditorSceneManager.playModeStartScene = menu;
        }

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }
    }
}
