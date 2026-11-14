using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

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

        public const string MaterialFolder = "Assets/_Project/Art/Materials";

        /// <summary>Material opaco del pipeline activo (URP Lit) con el color dado.</summary>
        public static Material LoadOrCreateMaterial(string name, Color color)
        {
            Material material = LoadOrCreateMaterialAsset(name, FindShader("Universal Render Pipeline/Lit"));
            SetColor(material, color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Material translúcido sin iluminación, para el fantasma de colocación.</summary>
        public static Material LoadOrCreateGhostMaterial(string name, Color color)
        {
            Material material = LoadOrCreateMaterialAsset(name, FindShader("Universal Render Pipeline/Unlit"));
            SetColor(material, color);

            // Lo mismo que hace el inspector de URP al elegir Surface Type = Transparent.
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreateMaterialAsset(string name, Shader shader)
        {
            EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            return material;
        }

        // _BaseColor en URP; _Color si se acabó usando el shader Standard.
        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        private static Shader FindShader(string urpShaderName)
        {
            Shader shader = Shader.Find(urpShaderName);
            if (shader != null)
            {
                return shader;
            }

            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && pipeline.defaultShader != null)
            {
                return pipeline.defaultShader;
            }

            return Shader.Find("Standard");
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
