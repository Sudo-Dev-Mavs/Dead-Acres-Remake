using System.Collections.Generic;
using DeadAcres.Building;
using DeadAcres.Combat;
using UnityEngine;

namespace DeadAcres.Defenses
{
    /// <summary>Cómo reparte el daño una torreta.</summary>
    public enum TurretMode : byte
    {
        /// <summary>Un objetivo: el más cercano.</summary>
        Single = 0,

        /// <summary>Todo lo que haya en un cono delante (escopeta, lanzallamas).</summary>
        Cone = 1,

        /// <summary>Un objetivo y saltos en cadena a los que tenga cerca (eléctrica).</summary>
        Chain = 2,
    }

    /// <summary>
    /// Torreta automática provisional: con energía, busca al enemigo más cercano en su alcance,
    /// gira hacia él y le dispara a intervalos según su modo. Sin energía no hace nada (luz de
    /// estado en rojo).
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

        [SerializeField] private TurretMode mode = TurretMode.Single;
        [SerializeField, Min(1f)] private float range = 12f;
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0.05f)] private float fireInterval = 0.5f;

        [Tooltip("Single: disparos por ráfaga (el doble cañón dispara 2).")]
        [SerializeField, Min(1)] private int shotsPerVolley = 1;

        [Tooltip("Cone: mitad de la apertura del cono, en grados.")]
        [SerializeField, Range(1f, 90f)] private float coneHalfAngle = 25f;

        [Tooltip("Cone: enemigos como mucho por disparo.")]
        [SerializeField, Min(1)] private int coneMaxTargets = 4;

        [Tooltip("Chain: saltos del rayo después del primer objetivo.")]
        [SerializeField, Min(0)] private int chainJumps = 4;

        [Tooltip("Chain: distancia máxima de cada salto.")]
        [SerializeField, Min(0.5f)] private float chainJumpRange = 5f;

        [Tooltip("Chain: fracción del daño que queda tras cada salto (0,7 = pierde un 30 %).")]
        [SerializeField, Range(0f, 1f)] private float chainFalloff = 0.7f;

        [Tooltip("Grosor del rayo que se ve al disparar.")]
        [SerializeField, Min(0.01f)] private float beamWidth = 0.05f;
        [SerializeField, Min(0.05f)] private float scanInterval = 0.25f;
        [SerializeField, Min(0f)] private float turnDegreesPerSecond = 540f;
        [SerializeField] private LayerMask enemyMask = Physics.DefaultRaycastLayers;

        private readonly Collider[] hits = new Collider[64];
        private readonly List<Health> candidates = new List<Health>(32);
        private readonly List<Vector3> candidatePositions = new List<Vector3>(32);
        private readonly List<int> selected = new List<int>(8);
        private readonly List<Transform> beams = new List<Transform>(5);
        private MaterialPropertyBlock block;
        private Health target;
        private float nextScanTime;
        private float nextFireTime;
        private float beamHideTime;

        /// <summary>La torreta disparó (para el sonido).</summary>
        public event System.Action Fired;

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
            if (beams.Count > 0 && Time.time >= beamHideTime)
            {
                HideBeams();
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
                Fire(origin, aimPoint, flat);
                Fired?.Invoke();
            }
        }

        private void Fire(Vector3 origin, Vector3 aimPoint, Vector3 flatAim)
        {
            HideBeams();
            switch (mode)
            {
                case TurretMode.Cone:
                    // Escopeta y lanzallamas: todo lo que pille delante, hasta un máximo.
                    CollectLivingCandidates();
                    TurretTargeting.SelectCone(transform.position, flatAim, coneHalfAngle, range, candidatePositions,
                        coneMaxTargets, selected);
                    for (int i = 0; i < selected.Count; i++)
                    {
                        Health hit = candidates[selected[i]];
                        ShowBeam(i, origin, hit.transform.position + Vector3.up);
                        hit.ApplyDamage(damage);
                    }

                    break;

                case TurretMode.Chain:
                    // Eléctrica: el rayo salta de zombi en zombi perdiendo fuerza.
                    CollectLivingCandidates();
                    TurretTargeting.SelectChain(candidatePositions, candidates.IndexOf(target), chainJumps,
                        chainJumpRange, selected);
                    Vector3 from = origin;
                    for (int i = 0; i < selected.Count; i++)
                    {
                        Health hit = candidates[selected[i]];
                        Vector3 to = hit.transform.position + Vector3.up;
                        ShowBeam(i, from, to);
                        from = to;
                        hit.ApplyDamage(TurretTargeting.GetChainDamage(damage, chainFalloff, i));
                    }

                    break;

                default:
                    ShowBeam(0, origin, aimPoint);
                    for (int i = 0; i < shotsPerVolley; i++)
                    {
                        target.ApplyDamage(damage);
                    }

                    break;
            }
        }

        // Deja en las listas solo los candidatos de la última búsqueda que sigan vivos.
        private void CollectLivingCandidates()
        {
            candidatePositions.Clear();
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (candidates[i] == null || candidates[i].IsDead)
                {
                    candidates.RemoveAt(i);
                }
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                candidatePositions.Add(candidates[i].transform.position);
            }
        }

        private Health FindTarget()
        {
            // La cadena puede saltar a zombis que están fuera del alcance de la torreta.
            float searchRadius = mode == TurretMode.Chain ? range + chainJumps * chainJumpRange : range;
            int count = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, hits, enemyMask,
                QueryTriggerInteraction.Ignore);
            Health best = null;
            float bestSqr = range * range;
            candidates.Clear();

            for (int i = 0; i < count; i++)
            {
                var candidate = hits[i].GetComponentInParent<Health>();
                if (candidate == null || candidate.Faction != Faction.Enemy || candidate.IsDead)
                {
                    continue;
                }

                if (!candidates.Contains(candidate)) candidates.Add(candidate);

                float sqr = (candidate.transform.position - transform.position).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }

            return best;
        }

        // Un rayo por impacto: el primero es el del prefab y los demás, copias suyas.
        private void ShowBeam(int index, Vector3 from, Vector3 to)
        {
            if (beam == null)
            {
                return;
            }

            if (beams.Count == 0)
            {
                beams.Add(beam);
            }

            while (beams.Count <= index)
            {
                beams.Add(Instantiate(beam, beam.parent));
            }

            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f)
            {
                return;
            }

            Transform segment = beams[index];
            segment.SetPositionAndRotation(from + delta * 0.5f, Quaternion.LookRotation(delta));
            segment.localScale = new Vector3(beamWidth, beamWidth, length);
            segment.gameObject.SetActive(true);
            beamHideTime = Time.time + BeamSeconds;
        }

        private void HideBeams()
        {
            for (int i = 0; i < beams.Count; i++)
            {
                if (beams[i] != null) beams[i].gameObject.SetActive(false);
            }
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

        internal void ConfigureStats(TurretMode newMode, float newRange, float newDamage, float newInterval,
            int shots = 1, float coneAngle = 25f, int coneTargets = 4, int jumps = 4, float jumpRange = 5f,
            float falloff = 0.7f, float newBeamWidth = 0.05f)
        {
            mode = newMode;
            range = Mathf.Max(1f, newRange);
            damage = Mathf.Max(0f, newDamage);
            fireInterval = Mathf.Max(0.05f, newInterval);
            shotsPerVolley = Mathf.Max(1, shots);
            coneHalfAngle = Mathf.Clamp(coneAngle, 1f, 90f);
            coneMaxTargets = Mathf.Max(1, coneTargets);
            chainJumps = Mathf.Max(0, jumps);
            chainJumpRange = Mathf.Max(0.5f, jumpRange);
            chainFalloff = Mathf.Clamp01(falloff);
            beamWidth = Mathf.Max(0.01f, newBeamWidth);
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
