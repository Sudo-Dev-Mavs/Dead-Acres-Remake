using System.Collections.Generic;
using UnityEngine;

namespace DeadAcres.Farming
{
    /// <summary>
    /// Vista provisional (greybox) de la granja: una losa por parcela arada y un cubo por planta,
    /// que crece por pasos y cambia de color al madurar. Solo reacciona a
    /// <see cref="FarmField.PlotChanged"/>; no decide nada. Sustituible por arte voxel sin tocar la lógica.
    /// </summary>
    public sealed class FarmAreaView : MonoBehaviour
    {
        private const float SoilHeight = 0.06f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private FarmArea farm;

        [Tooltip("Material compartido; el color se aplica por MaterialPropertyBlock.")]
        [SerializeField] private Material material;

        [SerializeField] private Color soilColor = new Color(0.36f, 0.24f, 0.14f);
        [SerializeField] private Color sproutColor = new Color(0.30f, 0.62f, 0.22f);

        private sealed class PlotVisual
        {
            public GameObject Soil;
            public GameObject Crop;
            public Renderer CropRenderer;
        }

        private readonly Dictionary<Vector2Int, PlotVisual> visuals = new Dictionary<Vector2Int, PlotVisual>();
        private readonly List<Vector2Int> cellBuffer = new List<Vector2Int>();
        private MaterialPropertyBlock block;

        private void OnEnable()
        {
            if (farm == null)
            {
                Debug.LogError("FarmAreaView necesita una FarmArea.", this);
                enabled = false;
                return;
            }

            farm.Field.PlotChanged += Refresh;

            cellBuffer.Clear();
            farm.Field.GetCells(cellBuffer);
            for (int i = 0; i < cellBuffer.Count; i++)
            {
                Refresh(cellBuffer[i]);
            }
        }

        private void OnDisable()
        {
            if (farm != null)
            {
                farm.Field.PlotChanged -= Refresh;
            }
        }

        private void Refresh(Vector2Int cell)
        {
            if (!farm.Field.TryGetPlot(cell, out PlotState state))
            {
                if (visuals.TryGetValue(cell, out PlotVisual stale))
                {
                    stale.Soil.SetActive(false);
                    stale.Crop.SetActive(false);
                }

                return;
            }

            PlotVisual visual = GetOrCreate(cell);
            visual.Soil.SetActive(true);

            if (state.Crop == null)
            {
                visual.Crop.SetActive(false);
                return;
            }

            float growth = (state.GrowthStep + 1) / (float)FarmField.GrowthSteps;
            float height = state.Crop.RipeHeight * growth;
            float width = farm.CellSize * Mathf.Lerp(0.2f, 0.6f, growth);
            Transform crop = visual.Crop.transform;
            crop.localScale = new Vector3(width, height, width);
            crop.position = farm.CellToWorld(cell) + Vector3.up * (SoilHeight + height * 0.5f);

            SetColor(visual.CropRenderer, state.Stage == PlotStage.Ripe ? state.Crop.RipeColor : sproutColor);
            visual.Crop.SetActive(true);
        }

        private PlotVisual GetOrCreate(Vector2Int cell)
        {
            if (visuals.TryGetValue(cell, out PlotVisual visual))
            {
                return visual;
            }

            Vector3 center = farm.CellToWorld(cell);
            Quaternion rotation = Quaternion.identity;
            float tile = farm.CellSize * 0.94f;

            // La losa no lleva collider: el rayo de interacción debe dar en el suelo.
            GameObject soil = CreateCube($"Soil {cell.x},{cell.y}", out Renderer soilRenderer, out Collider soilCollider);
            soilCollider.enabled = false;
            Destroy(soilCollider);
            soil.transform.SetPositionAndRotation(center + Vector3.up * (SoilHeight * 0.5f), rotation);
            soil.transform.localScale = new Vector3(tile, SoilHeight, tile);
            SetColor(soilRenderer, soilColor);

            // La planta sí: como trigger, para poder apuntarle sin que bloquee al jugador.
            GameObject crop = CreateCube($"Crop {cell.x},{cell.y}", out Renderer cropRenderer, out Collider cropCollider);
            cropCollider.isTrigger = true;
            crop.transform.rotation = rotation;
            crop.SetActive(false);

            visual = new PlotVisual { Soil = soil, Crop = crop, CropRenderer = cropRenderer };
            visuals.Add(cell, visual);
            return visual;
        }

        private GameObject CreateCube(string objectName, out Renderer cubeRenderer, out Collider cubeCollider)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(transform, true);
            cubeRenderer = cube.GetComponent<Renderer>();
            cubeCollider = cube.GetComponent<Collider>();

            if (material != null)
            {
                cubeRenderer.sharedMaterial = material;
            }

            return cube;
        }

        private void SetColor(Renderer target, Color color)
        {
            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            // _BaseColor es el de URP; _Color cubre el shader Standard por si falta el material.
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            target.SetPropertyBlock(block);
        }

        internal void Configure(FarmArea newFarm, Material sharedMaterial)
        {
            farm = newFarm;
            material = sharedMaterial;
        }
    }
}
