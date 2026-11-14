using UnityEditor;
using UnityEngine;

namespace DeadAcres.EditorTools
{
    internal static class EditorPaths
    {
        /// <summary>Crea la carpeta y todas las intermedias que falten (rutas tipo "Assets/a/b/c").</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int slash = path.LastIndexOf('/');
            if (slash <= 0)
            {
                Debug.LogError($"Ruta de carpeta no válida: {path}");
                return;
            }

            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
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
    }
}
