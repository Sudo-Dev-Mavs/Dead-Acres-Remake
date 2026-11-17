using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadAcres.World
{
    /// <summary>
    /// El bosque que rodea el mapa. Se genera al arrancar la escena como unas pocas mallas
    /// grandes (una de troncos y una por tono de copa) en vez de miles de objetos: así la escena
    /// guardada no crece y dibujarlo cuesta poco. Es solo decorado: no tiene colisión; quien no
    /// deja pasar es la barrera invisible del borde de la zona jugable.
    /// </summary>
    public sealed class ForestBorder : MonoBehaviour
    {
        [SerializeField, Min(10f)] private float mapHalfSize = 150f;

        [Tooltip("Anchura del bosque, desde el borde del terreno hacia dentro.")]
        [SerializeField, Min(1f)] private float bandWidth = 50f;
        [SerializeField, Min(0)] private int treeCount = 3500;

        [Tooltip("La misma semilla da siempre el mismo bosque.")]
        [SerializeField] private int seed = 20151;

        [SerializeField] private Material trunkMaterial;
        [SerializeField] private List<Material> canopyMaterials = new List<Material>();

        private readonly List<Mesh> builtMeshes = new List<Mesh>();

        private void Awake()
        {
            var trees = new List<ForestTree>(treeCount);
            ForestLayout.Generate(seed, mapHalfSize, bandWidth, treeCount, canopyMaterials.Count, trees);

            var trunks = new MeshData();
            var canopies = new List<MeshData>();
            for (int i = 0; i < Mathf.Max(1, canopyMaterials.Count); i++)
            {
                canopies.Add(new MeshData());
            }

            foreach (ForestTree tree in trees)
            {
                Quaternion rotation = Quaternion.Euler(0f, tree.Yaw, 0f);
                trunks.AddBox(tree.Position + Vector3.up * (tree.TrunkHeight * 0.5f),
                    new Vector3(tree.TrunkWidth, tree.TrunkHeight, tree.TrunkWidth), rotation);

                // La copa baja un poco sobre el tronco para que no se vea la unión.
                Vector3 canopyCenter = tree.Position + Vector3.up * (tree.TrunkHeight + tree.CanopySize * 0.4f);
                canopies[Mathf.Clamp(tree.Shade, 0, canopies.Count - 1)]
                    .AddBox(canopyCenter, new Vector3(tree.CanopySize, tree.CanopySize, tree.CanopySize), rotation);
            }

            CreatePart("Trunks", trunks, trunkMaterial);
            for (int i = 0; i < canopies.Count; i++)
            {
                CreatePart($"Canopies {i}", canopies[i], i < canopyMaterials.Count ? canopyMaterials[i] : trunkMaterial);
            }
        }

        private void OnDestroy()
        {
            // Las mallas creadas por código no se liberan solas al cambiar de escena.
            foreach (Mesh mesh in builtMeshes)
            {
                if (mesh != null) Destroy(mesh);
            }
        }

        private void CreatePart(string partName, MeshData data, Material material)
        {
            if (data.Vertices.Count == 0)
            {
                return;
            }

            var mesh = new Mesh { name = partName, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(data.Vertices);
            mesh.SetNormals(data.Normals);
            mesh.SetTriangles(data.Triangles, 0);
            mesh.RecalculateBounds();
            builtMeshes.Add(mesh);

            var part = new GameObject(partName);
            part.transform.SetParent(transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        internal void Configure(float halfSize, float width, int count, Material trunk, List<Material> canopies)
        {
            mapHalfSize = Mathf.Max(10f, halfSize);
            bandWidth = Mathf.Max(1f, width);
            treeCount = Mathf.Max(0, count);
            trunkMaterial = trunk;
            canopyMaterials = canopies ?? new List<Material>();
        }

        /// <summary>Vértices, normales y triángulos de muchas cajas juntas.</summary>
        private sealed class MeshData
        {
            // Las seis caras de un cubo de lado 1: normal y los dos ejes de la cara.
            private static readonly Vector3[] FaceNormals =
            {
                Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left,
            };

            private static readonly Vector3[] FaceRight =
            {
                Vector3.right, Vector3.left, Vector3.left, Vector3.right, Vector3.forward, Vector3.back,
            };

            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<int> Triangles = new List<int>();

            public void AddBox(Vector3 center, Vector3 size, Quaternion rotation)
            {
                for (int face = 0; face < 6; face++)
                {
                    Vector3 normal = FaceNormals[face];
                    Vector3 right = FaceRight[face];
                    Vector3 up = Vector3.Cross(normal, right);
                    int start = Vertices.Count;

                    AddVertex(center, size, rotation, normal - right - up, normal);
                    AddVertex(center, size, rotation, normal - right + up, normal);
                    AddVertex(center, size, rotation, normal + right + up, normal);
                    AddVertex(center, size, rotation, normal + right - up, normal);

                    // Orden de los vértices tal que la cara mira hacia fuera (Unity descarta las
                    // que quedan de espaldas).
                    Triangles.Add(start);
                    Triangles.Add(start + 2);
                    Triangles.Add(start + 1);
                    Triangles.Add(start);
                    Triangles.Add(start + 3);
                    Triangles.Add(start + 2);
                }
            }

            private void AddVertex(Vector3 center, Vector3 size, Quaternion rotation, Vector3 corner, Vector3 normal)
            {
                Vertices.Add(center + rotation * Vector3.Scale(corner * 0.5f, size));
                Normals.Add(rotation * normal);
            }
        }
    }
}
