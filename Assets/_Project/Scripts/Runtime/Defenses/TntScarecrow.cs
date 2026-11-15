using DeadAcres.Combat;
using UnityEngine;

namespace DeadAcres.Defenses
{
    /// <summary>
    /// Espantapájaros con TNT: cuando un enemigo se acerca, explota, daña a todos los enemigos
    /// del radio y desaparece. Un solo uso.
    /// </summary>
    public sealed class TntScarecrow : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float triggerRadius = 2.5f;
        [SerializeField, Min(0.5f)] private float blastRadius = 4.5f;
        [SerializeField, Min(0f)] private float damage = 120f;
        [SerializeField, Min(0.05f)] private float scanInterval = 0.2f;
        [SerializeField] private LayerMask enemyMask = Physics.DefaultRaycastLayers;

        [Tooltip("Opcional. Hijo desactivado que se muestra un instante al explotar.")]
        [SerializeField] private GameObject blastVisual;

        private readonly Collider[] hits = new Collider[64];
        private float nextScanTime;
        private bool exploded;

        private void Update()
        {
            if (exploded || Time.time < nextScanTime)
            {
                return;
            }

            nextScanTime = Time.time + scanInterval;
            int count = Physics.OverlapSphereNonAlloc(transform.position, triggerRadius, hits, enemyMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (IsLiveEnemy(hits[i], out _))
                {
                    Explode();
                    return;
                }
            }
        }

        private void Explode()
        {
            exploded = true;

            int count = Physics.OverlapSphereNonAlloc(transform.position, blastRadius, hits, enemyMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (IsLiveEnemy(hits[i], out Health enemy))
                {
                    enemy.ApplyDamage(damage);
                }
            }

            if (blastVisual != null)
            {
                blastVisual.transform.SetParent(null, true);
                blastVisual.SetActive(true);
                Destroy(blastVisual, 0.25f);
            }

            Destroy(gameObject);
        }

        private static bool IsLiveEnemy(Collider hit, out Health enemy)
        {
            enemy = hit.GetComponentInParent<Health>();
            return enemy != null && enemy.Faction == Faction.Enemy && !enemy.IsDead;
        }

        internal void Configure(GameObject blast) => blastVisual = blast;
    }
}
