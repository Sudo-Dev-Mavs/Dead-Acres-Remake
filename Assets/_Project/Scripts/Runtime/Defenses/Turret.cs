using DeadAcres.Building;
using DeadAcres.Combat;
using UnityEngine;

namespace DeadAcres.Defenses
{
    /// <summary>
    /// Torreta automática provisional: con energía, busca al enemigo más cercano en su alcance,
    /// gira hacia él y le dispara a intervalos. Sin energía no hace nada (luz de estado en rojo).
    /// </summary>
    public sealed class Turret : MonoBehaviour
    {
        private const float BeamSeconds = 0.06f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private PowerConsumer power;

        [Tooltip("Parte que gira en horizontal hacia el objetivo.")]
        [SerializeField] private Transform head;

        [Tooltip("Opcional. Caja fina que se estira un instante hasta el objetivo al disparar.")]
        [SerializeField] private Transform beam;

        [Tooltip("Opcional. Verde con energía, rojo sin ella.")]
        [SerializeField] private Renderer statusLight;

        [SerializeField, Min(1f)] private float range = 12f;
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0.05f)] private float fireInterval = 0.5f;
        [SerializeField, Min(0.05f)] private float scanInterval = 0.25f;
        [SerializeField, Min(0f)] private float turnDegreesPerSecond = 540f;
        [SerializeField] private LayerMask enemyMask = Physics.DefaultRaycastLayers;

        private readonly Collider[] hits = new Collider[64];
        private MaterialPropertyBlock block;
        private Health target;
        private float nextScanTime;
        private float nextFireTime;
        private float beamHideTime;

        private void OnEnable()
        {
            if (power != null) power.PoweredChanged += ShowStatus;
            ShowStatus(power != null && power.IsPowered);
            if (beam != null) beam.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (power != null) power.PoweredChanged -= ShowStatus;
        }

        private void Update()
        {
            if (beam != null && beam.gameObject.activeSelf && Time.time >= beamHideTime)
            {
                beam.gameObject.SetActive(false);
            }

            if (power == null || !power.IsPowered)
            {
                return;
            }

            // Buscar es lo caro: se hace unas pocas veces por segundo, no cada frame.
            if (Time.time >= nextScanTime)
            {
                nextScanTime = Time.time + scanInterval;
                target = FindTarget();
            }

            if (target == null || target.IsDead)
            {
                target = null;
                return;
            }

            Vector3 origin = head != null ? head.position : transform.position;
            Vector3 aimPoint = target.transform.position + Vector3.up;
            Vector3 flat = aimPoint - origin;
            flat.y = 0f;

            if (head != null && flat.sqrMagnitude > 0.0001f)
            {
                head.rotation = Quaternion.RotateTowards(head.rotation, Quaternion.LookRotation(flat),
                    turnDegreesPerSecond * Time.deltaTime);
            }

            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireInterval;
                target.ApplyDamage(damage);
                ShowBeam(origin, aimPoint);
            }
        }

        private Health FindTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, range, hits, enemyMask,
                QueryTriggerInteraction.Ignore);
            Health best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var candidate = hits[i].GetComponentInParent<Health>();
                if (candidate == null || candidate.Faction != Faction.Enemy || candidate.IsDead)
                {
                    continue;
                }

                float sqr = (candidate.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }

            return best;
        }

        private void ShowBeam(Vector3 from, Vector3 to)
        {
            if (beam == null)
            {
                return;
            }

            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f)
            {
                return;
            }

            beam.SetPositionAndRotation(from + delta * 0.5f, Quaternion.LookRotation(delta));
            beam.localScale = new Vector3(0.05f, 0.05f, length);
            beam.gameObject.SetActive(true);
            beamHideTime = Time.time + BeamSeconds;
        }

        private void ShowStatus(bool powered)
        {
            if (statusLight == null)
            {
                return;
            }

            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            Color color = powered ? new Color(0.2f, 0.95f, 0.3f) : new Color(0.95f, 0.2f, 0.2f);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            statusLight.SetPropertyBlock(block);
        }

        internal void Configure(PowerConsumer consumer, Transform headTransform, Transform beamTransform,
            Renderer status)
        {
            power = consumer;
            head = headTransform;
            beam = beamTransform;
            statusLight = status;
        }
    }
}
