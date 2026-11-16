using DeadAcres.Items;
using DeadAcres.Player;
using UnityEngine;

namespace DeadAcres.Animals
{
    /// <summary>
    /// Animal de granja provisional (oveja, gallina): pasea cerca de donde se colocó, sin
    /// atravesar vallas ni muros, y cada cierto tiempo tiene un producto listo que el jugador
    /// recoge con Interactuar (F). Tiene vida: los zombis lo atacan si lo tienen cerca.
    /// </summary>
    public sealed class Animal : MonoBehaviour, IInteractable
    {
        [SerializeField] private string displayName = "Animal";

        [Tooltip("Verbo de la ayuda del HUD: \"Recoger\", \"Esquilar\"...")]
        [SerializeField] private string collectVerb = "Recoger";

        [SerializeField] private ItemDefinition product;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField, Min(1f)] private float secondsPerProduct = 60f;

        [Tooltip("Opcional. Se muestra mientras hay producto listo.")]
        [SerializeField] private GameObject readyIndicator;

        [Header("Paseo")]
        [SerializeField, Min(0f)] private float wanderRadius = 2.5f;
        [SerializeField, Min(0f)] private float speed = 0.8f;
        [SerializeField] private LayerMask obstacleMask = Physics.DefaultRaycastLayers;

        private readonly RaycastHit[] hitBuffer = new RaycastHit[8];
        private ProductionTimer timer;
        private Vector3 home;
        private Vector3 destination;
        private float waitUntil;

        public string Prompt => product != null ? $"{collectVerb} {product.DisplayName} ({displayName})" : displayName;

        public bool CanInteract => timer != null && timer.IsReady && product != null;

        private void Awake()
        {
            timer = new ProductionTimer(secondsPerProduct);
            timer.BecameReady += ShowIndicator;
            home = transform.position;
            destination = home;
            if (readyIndicator != null) readyIndicator.SetActive(false);
        }

        // El paseo y la cuenta atrás son continuos: van por frame.
        private void Update()
        {
            timer.Tick(Time.deltaTime);

            if (Time.time < waitUntil || speed <= 0f)
            {
                return;
            }

            Vector3 toDestination = destination - transform.position;
            toDestination.y = 0f;
            if (toDestination.sqrMagnitude < 0.04f)
            {
                // Llegó: descansa un rato y elige otro punto alrededor de casa.
                ChooseDestination();
                waitUntil = Time.time + Random.Range(1f, 4f);
                return;
            }

            Vector3 direction = toDestination.normalized;
            if (IsBlocked(direction, 0.5f))
            {
                // Le pusieron algo delante mientras caminaba: se queda donde está y elige otro punto.
                destination = transform.position;
                return;
            }

            transform.position += direction * (speed * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction),
                240f * Time.deltaTime);
        }

        // Solo acepta puntos a los que puede llegar en línea recta: así no sale de un corral.
        private void ChooseDestination()
        {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = home + new Vector3(offset.x, 0f, offset.y);
            Vector3 toCandidate = candidate - transform.position;
            toCandidate.y = 0f;
            float distance = toCandidate.magnitude;

            destination = distance > 0.01f && !IsBlocked(toCandidate / distance, distance + 0.3f)
                ? candidate
                : transform.position;
        }

        private bool IsBlocked(Vector3 direction, float distance)
        {
            Vector3 origin = transform.position + Vector3.up * 0.35f;
            int count = Physics.SphereCastNonAlloc(origin, 0.2f, direction, hitBuffer, distance, obstacleMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                // No cuentan sus propios colliders ni lo que ya tiene encima (distancia 0): de eso
                // tiene que poder apartarse.
                if (hitBuffer[i].distance > 0f && !hitBuffer[i].collider.transform.IsChildOf(transform))
                {
                    return true;
                }
            }

            return false;
        }

        public void Interact(GameObject player)
        {
            if (!CanInteract || !player.TryGetComponent(out PlayerInventory playerInventory))
            {
                return;
            }

            // Si no cabe, el producto se queda en el animal.
            if (!playerInventory.Inventory.CanAdd(product, amount))
            {
                return;
            }

            timer.Collect();
            playerInventory.Inventory.Add(product, amount);
            if (readyIndicator != null) readyIndicator.SetActive(false);
        }

        private void ShowIndicator()
        {
            if (readyIndicator != null) readyIndicator.SetActive(true);
        }

        internal void Configure(string name, string verb, ItemDefinition item, int count, float seconds,
            GameObject indicator)
        {
            displayName = name;
            collectVerb = verb;
            product = item;
            amount = Mathf.Max(1, count);
            secondsPerProduct = Mathf.Max(1f, seconds);
            readyIndicator = indicator;
        }
    }
}
