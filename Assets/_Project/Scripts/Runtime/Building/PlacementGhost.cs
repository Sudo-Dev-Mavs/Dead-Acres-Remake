using System.Collections.Generic;
using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>
    /// Vista previa translúcida verde/roja de lo que se va a colocar.
    /// Se construye copiando solo las mallas del prefab, para que ningún script del prefab
    /// (torretas, generadores) se ejecute en el fantasma.
    /// </summary>
    public sealed class PlacementGhost
    {
        private readonly Transform root;
        private readonly Material validMaterial;
        private readonly Material invalidMaterial;
        private readonly List<MeshRenderer> renderers = new List<MeshRenderer>(8);
        private readonly List<MeshFilter> filterBuffer = new List<MeshFilter>(8);
        private bool? lastValid;

        public PlacementGhost(Material valid, Material invalid)
        {
            validMaterial = valid;
            invalidMaterial = invalid;
            root = new GameObject("PlacementGhost").transform;
            root.gameObject.SetActive(false);
        }

        public bool IsVisible => root.gameObject.activeSelf;

        /// <summary>Reconstruye el fantasma. Solo al cambiar de ítem, nunca por frame.</summary>
        public void Build(GameObject prefab)
        {
            Clear();
            if (prefab == null) return;

            prefab.GetComponentsInChildren(true, filterBuffer);
            Transform prefabRoot = prefab.transform;

            foreach (MeshFilter source in filterBuffer)
            {
                if (source.sharedMesh == null) continue;

                var part = new GameObject(source.name);
                part.transform.SetParent(root, false);

                // Pose relativa a la raíz del prefab.
                Matrix4x4 local = prefabRoot.worldToLocalMatrix * source.transform.localToWorldMatrix;
                part.transform.localPosition = local.GetColumn(3);
                part.transform.localRotation = local.rotation;
                part.transform.localScale = local.lossyScale;

                part.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                var meshRenderer = part.AddComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                renderers.Add(meshRenderer);
            }

            lastValid = null;
        }

        public void Show(Vector3 position, Quaternion rotation, bool valid)
        {
            root.SetPositionAndRotation(position, rotation);
            if (!root.gameObject.activeSelf) root.gameObject.SetActive(true);

            if (lastValid == valid) return;
            lastValid = valid;

            Material material = valid ? validMaterial : invalidMaterial;
            foreach (MeshRenderer r in renderers)
            {
                r.sharedMaterial = material;
            }
        }

        public void Hide()
        {
            if (root.gameObject.activeSelf) root.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            if (root != null) Object.Destroy(root.gameObject);
        }

        private void Clear()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                // Destroy es diferido: se desactiva ya para que no se vea junto al nuevo fantasma este frame.
                GameObject child = root.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }

            renderers.Clear();
            Hide();
        }
    }
}
