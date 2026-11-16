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

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
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

            int targetIndex = ChooseTarget();
            if (targetIndex < 0)
            {
                controller.SimpleMove(Vector3.zero);
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
            target.ApplyDamage(attackDamage);
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
