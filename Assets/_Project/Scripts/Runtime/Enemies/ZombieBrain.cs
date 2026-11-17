using System.Collections.Generic;
using DeadAcres.Combat;
using UnityEngine;

namespace DeadAcres.Enemies
{
    /// <summary>
    /// Zombi provisional. Va en línea recta hacia la caravana, o hacia un jugador o un animal si
    /// lo tiene cerca, y golpea lo que sea del bando del jugador y le corte el paso (muros incluidos).
    /// No usa NavMesh todavía: no rodea obstáculos largos, los rompe.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]
    public sealed class ZombieBrain : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 2.2f;
        [SerializeField, Min(0f)] private float turnDegreesPerSecond = 360f;
        [SerializeField, Min(0f)] private float attackDamage = 8f;
        [SerializeField, Min(0.1f)] private float attackInterval = 1f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.1f;

        [Tooltip("A esta distancia deja la caravana y va a por el jugador.")]
        [SerializeField, Min(0f)] private float aggroRadius = 9f;

        [SerializeField] private LayerMask obstacleMask = Physics.DefaultRaycastLayers;

        private readonly List<Health> targets = new List<Health>(4);
        private readonly List<Collider> targetColliders = new List<Collider>(4);
        private CharacterController controller;
        private Health health;
        private float nextAttackTime;
        private float sidestepSign;
        private float damageMultiplier = 1f;
        private ZombieAbilities abilities;
        private IReadOnlyList<Health> playerList;

        public float DamageMultiplier => damageMultiplier;
        public float AttackRange => attackRange;
        public float BodyHeight => controller != null ? controller.height : 1.8f;
        public float BodyRadius => controller != null ? controller.radius : 0.35f;

        /// <summary>Quiénes de sus objetivos son jugadores (para las habilidades que solo van a por ellos).</summary>
        public void SetPlayers(IReadOnlyList<Health> players) => playerList = players;

        /// <summary>Se queda quieto mirando hacia esa dirección.</summary>
        public void Hold(Vector3 lookDirection)
        {
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.001f) Face(lookDirection.normalized);
            controller.SimpleMove(Vector3.zero);
        }

        /// <summary>Se mueve en línea recta a esa velocidad, sin esquivar nada (la embestida).</summary>
        public void MoveRaw(Vector3 direction, float moveSpeed)
        {
            Face(direction);
            controller.SimpleMove(direction * moveSpeed);
        }

        /// <summary>
        /// El objetivo vivo más cercano dentro de esa distancia: solo jugadores, o cualquiera
        /// de los suyos (caravana, jugadores, animales, torretas).
        /// </summary>
        public bool TryGetNearestTarget(float maxDistance, bool playersOnly, out Health target, out Vector3 point)
        {
            target = null;
            point = default;
            float bestSqr = maxDistance * maxDistance;
            for (int i = 0; i < targets.Count; i++)
            {
                Health candidate = targets[i];
                if (candidate == null || candidate.IsDead || (playersOnly && !IsPlayer(candidate)))
                {
                    continue;
                }

                Vector3 closest = GetClosestPoint(i);
                float sqr = (closest - transform.position).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    target = candidate;
                    point = closest;
                }
            }

            return target != null;
        }

        private bool IsPlayer(Health candidate)
        {
            if (playerList == null)
            {
                return false;
            }

            for (int i = 0; i < playerList.Count; i++)
            {
                if (playerList[i] == candidate) return true;
            }

            return false;
        }

        // Amaneció con el zombi vivo: arde, huye de la granja y solo ataca a un jugador cercano.
        private bool burning;
        private Vector3 fleeFrom;
        private float despawnDistance;
        private float burnPerSecond;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            abilities = GetComponent<ZombieAbilities>();
        }

        /// <param name="primaryTarget">La caravana.</param>
        /// <param name="players">Jugadores y animales a los que puede desviarse.</param>
        public void Initialize(Health primaryTarget, IReadOnlyList<Health> players)
        {
            targets.Clear();
            targetColliders.Clear();
            AddTarget(primaryTarget);
            if (players != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    AddTarget(players[i]);
                }
            }

            // Cada zombi esquiva por un lado distinto para que no se apilen en fila.
            sidestepSign = Random.value < 0.5f ? -1f : 1f;
            nextAttackTime = 0f;
        }

        /// <summary>
        /// Amanece: el zombi empieza a quemarse. Se aleja de <paramref name="fleeFromPoint"/> y
        /// desaparece al llegar a <paramref name="despawnAtDistance"/>; si tiene un jugador cerca,
        /// va a por él hasta que muera uno de los dos.
        /// </summary>
        public void StartBurning(IReadOnlyList<Health> players, Vector3 fleeFromPoint, float despawnAtDistance,
            float secondsToDie)
        {
            burning = true;
            fleeFrom = fleeFromPoint;
            despawnDistance = despawnAtDistance;
            burnPerSecond = health.Max / Mathf.Max(0.5f, secondsToDie);

            // Deja en paz la caravana y los animales: el hueco 0 (la caravana) queda vacío.
            targets.Clear();
            targetColliders.Clear();
            targets.Add(null);
            targetColliders.Add(null);
            if (players != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    AddTarget(players[i]);
                }
            }

            ShowFire();
        }

        private void ShowFire()
        {
            var tint = new MaterialPropertyBlock();
            tint.SetColor("_BaseColor", new Color(1f, 0.45f, 0.1f));
            foreach (Renderer part in GetComponentsInChildren<Renderer>())
            {
                part.SetPropertyBlock(tint);
            }

            var glow = new GameObject("Fire").AddComponent<Light>();
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = Vector3.up * 1.2f;
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.55f, 0.15f);
            glow.range = 5f;
            glow.intensity = 3f;
        }

        // Huye en línea recta; lo que le estorbe lo rodea en vez de romperlo.
        private void Flee()
        {
            Vector3 away = transform.position - fleeFrom;
            away.y = 0f;
            if (away.magnitude >= despawnDistance)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 direction = away.sqrMagnitude > 0.001f ? away.normalized : transform.forward;
            if (TryFindObstacle(direction, out _))
            {
                direction = Quaternion.Euler(0f, 70f * sidestepSign, 0f) * direction;
            }

            Face(direction);
            controller.SimpleMove(direction * (speed * 1.6f));
        }

        /// <summary>Cuánto más pega esta noche respecto a su daño base.</summary>
        public void SetDamageMultiplier(float multiplier) => damageMultiplier = Mathf.Max(0f, multiplier);

        internal void Configure(float newSpeed, float newDamage, float newAttackInterval, float newAttackRange = 1.1f)
        {
            attackRange = Mathf.Max(0.1f, newAttackRange);
            speed = Mathf.Max(0f, newSpeed);
            attackDamage = Mathf.Max(0f, newDamage);
            attackInterval = Mathf.Max(0.1f, newAttackInterval);
        }

        private void AddTarget(Health target)
        {
            if (target == null)
            {
                return;
            }

            targets.Add(target);
            targetColliders.Add(target.GetComponentInChildren<Collider>());
        }

        private void Update()
        {
            if (health.IsDead)
            {
                return;
            }

            if (burning)
            {
                // El fuego no entiende de armaduras.
                health.ApplyDamage(burnPerSecond * Time.deltaTime, true);
                if (health.IsDead)
                {
                    return;
                }
            }
            else if (abilities != null && abilities.TakeControl(this))
            {
                // Está escupiendo, arrancando una roca o embistiendo.
                return;
            }

            int targetIndex = ChooseTarget();
            if (targetIndex < 0)
            {
                if (burning)
                {
                    Flee();
                }
                else
                {
                    controller.SimpleMove(Vector3.zero);
                }

                return;
            }

            Vector3 toTarget = GetClosestPoint(targetIndex) - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            Vector3 direction = distance > 0.001f ? toTarget / distance : transform.forward;

            if (distance <= attackRange)
            {
                Face(direction);
                Attack(targets[targetIndex]);
                controller.SimpleMove(Vector3.zero);
                return;
            }

            if (TryFindObstacle(direction, out Health blocker))
            {
                if (blocker != null)
                {
                    Face(direction);
                    Attack(blocker);
                    controller.SimpleMove(Vector3.zero);
                    return;
                }

                // Algo sólido sin vida (un poste, una roca): se rodea.
                direction = Quaternion.Euler(0f, 70f * sidestepSign, 0f) * direction;
            }

            Face(direction);
            controller.SimpleMove(direction * speed);
        }

        /// <returns>Índice del jugador vivo más cercano dentro del radio de agresión; si no, la caravana.</returns>
        private int ChooseTarget()
        {
            int best = -1;
            float bestSqr = aggroRadius * aggroRadius;

            // El índice 0 es la caravana; a partir del 1, jugadores y animales.
            for (int i = 1; i < targets.Count; i++)
            {
                if (targets[i] == null || targets[i].IsDead)
                {
                    continue;
                }

                float sqr = (targets[i].transform.position - transform.position).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = i;
                }
            }

            if (best >= 0)
            {
                return best;
            }

            return targets.Count > 0 && targets[0] != null && !targets[0].IsDead ? 0 : -1;
        }

        private Vector3 GetClosestPoint(int index)
        {
            Collider targetCollider = targetColliders[index];
            return targetCollider != null
                ? targetCollider.bounds.ClosestPoint(transform.position)
                : targets[index].transform.position;
        }

        /// <returns>True si hay algo delante. <paramref name="blocker"/> es null si no se puede romper.</returns>
        private bool TryFindObstacle(Vector3 direction, out Health blocker)
        {
            blocker = null;
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            if (!Physics.SphereCast(origin, 0.3f, direction, out RaycastHit hit, 0.9f, obstacleMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            var other = hit.collider.GetComponentInParent<Health>();
            if (other == null)
            {
                return true;
            }

            // Otros zombis no cuentan: se empujan entre ellos.
            if (other.Faction == Faction.Enemy || other.IsDead)
            {
                return false;
            }

            blocker = other;
            return true;
        }

        private void Attack(Health target)
        {
            if (Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackInterval;
            target.ApplyDamage(attackDamage * damageMultiplier);
        }

        private void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion look = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnDegreesPerSecond * Time.deltaTime);
        }
    }
}
