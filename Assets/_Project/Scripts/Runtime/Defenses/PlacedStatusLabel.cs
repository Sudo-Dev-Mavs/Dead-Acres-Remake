using DeadAcres.Building;
using DeadAcres.Combat;
using UnityEngine;

namespace DeadAcres.Defenses
{
    /// <summary>
    /// Cartel flotante sobre un objeto colocado (las torretas): nombre, vida y consumo de
    /// electricidad. Solo se ve con la cámara cerca y siempre mira hacia ella.
    /// </summary>
    public sealed class PlacedStatusLabel : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float height = 1.7f;
        [SerializeField, Min(1f)] private float showDistance = 5f;

        private Health health;
        private PowerConsumer power;
        private PlacedObject placed;
        private TextMesh text;
        private Transform cameraTransform;
        private float nextCameraSearch;
        private bool dirty = true;

        private void Awake()
        {
            health = GetComponent<Health>();
            power = GetComponent<PowerConsumer>();
            placed = GetComponent<PlacedObject>();

            var labelObject = new GameObject("StatusLabel");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = Vector3.up * height;

            text = labelObject.AddComponent<TextMesh>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                text.font = font;
                labelObject.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }

            // Tamaño grande de fuente con caracteres pequeños: letra nítida de unos 12 cm.
            text.fontSize = 64;
            text.characterSize = 0.025f;
            text.anchor = TextAnchor.LowerCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            labelObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (health != null) health.HealthChanged += OnHealthChanged;
            if (power != null) power.PoweredChanged += OnPoweredChanged;
        }

        private void OnDisable()
        {
            if (health != null) health.HealthChanged -= OnHealthChanged;
            if (power != null) power.PoweredChanged -= OnPoweredChanged;
        }

        private void OnHealthChanged(float current, float max) => dirty = true;

        private void OnPoweredChanged(bool powered) => dirty = true;

        // La distancia a la cámara cambia de forma continua: se mira cada frame.
        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                if (Time.unscaledTime < nextCameraSearch)
                {
                    return;
                }

                nextCameraSearch = Time.unscaledTime + 1f;
                Camera main = Camera.main;
                if (main == null)
                {
                    return;
                }

                cameraTransform = main.transform;
            }

            Transform label = text.transform;
            bool near = (cameraTransform.position - transform.position).sqrMagnitude <= showDistance * showDistance;
            if (near != label.gameObject.activeSelf)
            {
                label.gameObject.SetActive(near);
                dirty = true;
            }

            if (!near)
            {
                return;
            }

            // De cara a la cámara, sin inclinarse.
            Vector3 away = label.position - cameraTransform.position;
            away.y = 0f;
            if (away.sqrMagnitude > 0.0001f) label.rotation = Quaternion.LookRotation(away);

            if (dirty)
            {
                dirty = false;
                text.text = BuildText();
            }
        }

        private string BuildText()
        {
            if (placed == null) placed = GetComponent<PlacedObject>();
            string title = placed != null && placed.Item != null ? placed.Item.DisplayName : name;
            string life = health != null ? $"\nVida {Mathf.CeilToInt(health.Current)}/{Mathf.CeilToInt(health.Max)}" : string.Empty;
            string energy = power != null
                ? $"\nConsumo {power.Demand:0.#}  ·  {(power.IsPowered ? "con energía" : "SIN ENERGÍA")}"
                : string.Empty;
            return title + life + energy;
        }
    }
}
