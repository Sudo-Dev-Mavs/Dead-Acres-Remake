using DeadAcres.Combat;
using UnityEngine;

namespace DeadAcres.Enemies
{
    /// <summary>
    /// Habilidades especiales de un zombi, además de andar y morder: lanzar algo a distancia
    /// (el ácido del escupidor, la roca del demoledor mutante) y embestir a un jugador.
    /// El <see cref="ZombieBrain"/> le cede el control cuando está usando una.
    /// </summary>
    public sealed class ZombieAbilities : MonoBehaviour
    {
        [Header("Ataque a distancia (0 de daño = no tiene)")]
        [SerializeField, Min(0f)] private float rangedDamage;
        [SerializeField, Min(1f)] private float rangedRange = 14f;
        [SerializeField, Min(0.2f)] private float rangedInterval = 2.5f;

        [Tooltip("Segundos preparando el lanzamiento, quieto (la roca sale del suelo).")]
        [SerializeField, Min(0f)] private float rangedWindup = 0.4f;

        [Tooltip("True: solo dispara a jugadores. False: a lo que tenga más cerca (jugador, torreta, caravana...).")]
        [SerializeField] private bool rangedPlayersOnly;

        [Tooltip("True: se queda a distancia disparando en vez de acercarse.")]
        [SerializeField] private bool keepsDistance = true;

        [SerializeField, Min(0.5f)] private float projectileSpeed = 12f;
        [SerializeField, Min(0.1f)] private float projectileRadius = 1.2f;
        [SerializeField, Min(0.05f)] private float projectileSize = 0.3f;
        [SerializeField] private Color projectileColor = new Color(0.55f, 0.95f, 0.2f);
        [SerializeField] private bool projectileIsRock;

        [Header("Embestida (0 de velocidad = no tiene)")]
        [SerializeField, Min(0f)] private float chargeSpeed;
        [SerializeField, Min(0.2f)] private float chargeSeconds = 1.6f;
        [SerializeField, Min(1f)] private float chargeCooldown = 7f;
        [SerializeField, Min(0f)] private float chargeMinDistance = 6f;
        [SerializeField, Min(1f)] private float chargeMaxDistance = 18f;
        [SerializeField, Min(0f)] private float chargeDamage = 60f;
        [SerializeField, Min(0.5f)] private float chargeHitDistance = 2.2f;

        private ZombieProjectile heldProjectile;
        private Vector3 launchPoint;
        private float windupLeft;
        private float windupTotal;
        private float nextRanged;
        private float nextCharge;
        private Vector3 chargeDirection;
        private float chargeLeft;
        private bool charging;
        private bool chargeHit;

        private void OnEnable()
        {
            // Que no lo usen todo nada más aparecer.
            nextRanged = Time.time + Random.Range(1f, rangedInterval);
            nextCharge = Time.time + Random.Range(2f, chargeCooldown);
        }

        private void OnDisable()
        {
            if (heldProjectile != null) Destroy(heldProjectile.gameObject);
        }

        /// <returns>True si este fotograma manda la habilidad: el cerebro no debe moverse por su cuenta.</returns>
        public bool TakeControl(ZombieBrain brain)
        {
            if (charging)
            {
                TickCharge(brain);
                return true;
            }

            if (heldProjectile != null)
            {
                TickWindup(brain);
                return true;
            }

            if (chargeSpeed > 0f && Time.time >= nextCharge
                && brain.TryGetNearestTarget(chargeMaxDistance, true, out Health runTarget, out Vector3 runPoint))
            {
                Vector3 toPlayer = runPoint - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.magnitude >= chargeMinDistance)
                {
                    charging = true;
                    chargeHit = false;
                    chargeLeft = chargeSeconds;
                    chargeDirection = toPlayer.normalized;
                    nextCharge = Time.time + chargeCooldown;
                    return true;
                }
            }

            if (rangedDamage <= 0f
                || !brain.TryGetNearestTarget(rangedRange, rangedPlayersOnly, out Health target, out Vector3 aimPoint))
            {
                return false;
            }

            Vector3 toTarget = aimPoint - transform.position;
            toTarget.y = 0f;
            if (Time.time >= nextRanged && toTarget.magnitude > brain.AttackRange * 1.5f)
            {
                nextRanged = Time.time + rangedInterval;
                BeginWindup(brain, aimPoint, toTarget);
                return true;
            }

            // Entre disparo y disparo: el escupidor se planta a distancia; el de la roca sigue avanzando.
            if (keepsDistance && toTarget.magnitude > brain.AttackRange * 1.5f)
            {
                brain.Hold(toTarget);
                return true;
            }

            return false;
        }

        private void BeginWindup(ZombieBrain brain, Vector3 aimPoint, Vector3 toTarget)
        {
            launchPoint = aimPoint + Vector3.up * 0.8f;
            windupTotal = Mathf.Max(0.01f, rangedWindup);
            windupLeft = rangedWindup;

            // La roca sale del suelo delante de él; el ácido, de la boca.
            Vector3 forward = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;
            Vector3 start = projectileIsRock
                ? transform.position + forward * (brain.BodyRadius + projectileSize) - Vector3.up * projectileSize
                : transform.position + Vector3.up * (brain.BodyHeight * 0.85f) + forward * brain.BodyRadius;
            heldProjectile = ZombieProjectile.Create(start, projectileSize, projectileColor, projectileIsRock);
            brain.Hold(toTarget);
        }

        private void TickWindup(ZombieBrain brain)
        {
            Vector3 toTarget = launchPoint - transform.position;
            toTarget.y = 0f;
            brain.Hold(toTarget);

            windupLeft -= Time.deltaTime;
            if (projectileIsRock)
            {
                // Sube desde el suelo hasta la altura de los brazos mientras la arranca.
                float lifted = 1f - Mathf.Clamp01(windupLeft / windupTotal);
                Vector3 position = heldProjectile.transform.position;
                position.y = Mathf.Lerp(-projectileSize, brain.BodyHeight * 0.75f, lifted);
                heldProjectile.transform.position = position;
            }

            if (windupLeft > 0f)
            {
                return;
            }

            float distance = Vector3.Distance(heldProjectile.transform.position, launchPoint);
            heldProjectile.Launch(launchPoint, projectileSpeed, Mathf.Clamp(distance * 0.18f, 0.5f, 5f),
                rangedDamage * brain.DamageMultiplier, projectileRadius);
            heldProjectile = null;
        }

        private void TickCharge(ZombieBrain brain)
        {
            brain.MoveRaw(chargeDirection, chargeSpeed);
            chargeLeft -= Time.deltaTime;

            // Si pasa pegado a un jugador, lo arrolla (una vez por embestida).
            if (!chargeHit && brain.TryGetNearestTarget(chargeHitDistance + brain.BodyRadius, true, out Health player, out _))
            {
                chargeHit = true;
                player.ApplyDamage(chargeDamage * brain.DamageMultiplier);
                chargeLeft = Mathf.Min(chargeLeft, 0.25f);
            }

            if (chargeLeft <= 0f)
            {
                charging = false;
            }
        }

        internal void ConfigureRanged(float damage, float range, float interval, float windup, bool playersOnly,
            bool holdAtRange, float speed, float radius, float size, Color color, bool rock)
        {
            rangedDamage = Mathf.Max(0f, damage);
            rangedRange = Mathf.Max(1f, range);
            rangedInterval = Mathf.Max(0.2f, interval);
            rangedWindup = Mathf.Max(0f, windup);
            rangedPlayersOnly = playersOnly;
            keepsDistance = holdAtRange;
            projectileSpeed = Mathf.Max(0.5f, speed);
            projectileRadius = Mathf.Max(0.1f, radius);
            projectileSize = Mathf.Max(0.05f, size);
            projectileColor = color;
            projectileIsRock = rock;
        }

        internal void ConfigureCharge(float speed, float seconds, float cooldown, float minDistance, float maxDistance,
            float damage, float hitDistance)
        {
            chargeSpeed = Mathf.Max(0f, speed);
            chargeSeconds = Mathf.Max(0.2f, seconds);
            chargeCooldown = Mathf.Max(1f, cooldown);
            chargeMinDistance = Mathf.Max(0f, minDistance);
            chargeMaxDistance = Mathf.Max(1f, maxDistance);
            chargeDamage = Mathf.Max(0f, damage);
            chargeHitDistance = Mathf.Max(0.5f, hitDistance);
        }
    }
}
