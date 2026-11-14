using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>
    /// Marco provisional sobre la celda que mira el jugador local. Amarillo si ahí se puede
    /// hacer algo con lo que lleva en la mano, gris si no.
    /// </summary>
    public sealed class TargetCellHighlight : MonoBehaviour
    {
        private const float BarThickness = 0.05f;
        private const float BarHeight = 0.04f;
        private const float HeightAboveGround = 0.09f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private Material material;
        [SerializeField] private Color actionColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color idleColor = new Color(0.75f, 0.75f, 0.75f);

        private Transform frame;
        private Renderer[] bars;
        private MaterialPropertyBlock block;

        private void OnEnable()
        {
            if (interactor == null)
            {
                Debug.LogError("TargetCellHighlight necesita un PlayerInteractor.", this);
                enabled = false;
                return;
            }

            interactor.ContextChanged += Refresh;
            Refresh(interactor.Context);
        }

        private void OnDisable()
        {
            if (interactor != null)
            {
                interactor.ContextChanged -= Refresh;
            }

            if (frame != null)
            {
                frame.gameObject.SetActive(false);
            }
        }

        private void Refresh(InteractionContext context)
        {
            if (!context.HasTarget || interactor.Farm == null)
            {
                if (frame != null)
                {
                    frame.gameObject.SetActive(false);
                }

                return;
            }

            EnsureFrame(interactor.Farm.CellSize);

            frame.SetPositionAndRotation(
                interactor.Farm.CellToWorld(context.Cell) + Vector3.up * HeightAboveGround,
                Quaternion.identity);

            bool actionable = context.Primary != PrimaryActionKind.None || context.CanHarvest;
            Color color = actionable ? actionColor : idleColor;
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            for (int i = 0; i < bars.Length; i++)
            {
                bars[i].SetPropertyBlock(block);
            }

            frame.gameObject.SetActive(true);
        }

        private void EnsureFrame(float cellSize)
        {
            if (frame != null)
            {
                return;
            }

            block = new MaterialPropertyBlock();
            frame = new GameObject("Frame").transform;
            frame.SetParent(transform, false);

            float half = cellSize * 0.5f - BarThickness * 0.5f;
            bars = new[]
            {
                CreateBar(new Vector3(0f, 0f, half), new Vector3(cellSize, BarHeight, BarThickness)),
                CreateBar(new Vector3(0f, 0f, -half), new Vector3(cellSize, BarHeight, BarThickness)),
                CreateBar(new Vector3(half, 0f, 0f), new Vector3(BarThickness, BarHeight, cellSize)),
                CreateBar(new Vector3(-half, 0f, 0f), new Vector3(BarThickness, BarHeight, cellSize)),
            };
        }

        private Renderer CreateBar(Vector3 localPosition, Vector3 localScale)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Bar";
            Collider barCollider = bar.GetComponent<Collider>();
            barCollider.enabled = false;
            Destroy(barCollider);
            bar.transform.SetParent(frame, false);
            bar.transform.localPosition = localPosition;
            bar.transform.localScale = localScale;

            Renderer barRenderer = bar.GetComponent<Renderer>();
            if (material != null)
            {
                barRenderer.sharedMaterial = material;
            }

            return barRenderer;
        }

        internal void Configure(PlayerInteractor playerInteractor, Material sharedMaterial)
        {
            interactor = playerInteractor;
            material = sharedMaterial;
        }
    }
}
