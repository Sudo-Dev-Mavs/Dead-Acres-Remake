using DeadAcres.Combat;
using DeadAcres.Settings;
using UnityEngine;

namespace DeadAcres.Enemies
{
    /// <summary>
    /// Zombi explosivo: al morir revienta y daña todo lo que tenga cerca, sea del bando que sea
    /// (también a otros zombis: matarlo en medio de un grupo tiene premio).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class ExplodeOnDeath : MonoBehaviour
    {
        // Propio de cada zombi: una explosión puede matar a otro explosivo y encadenar la suya
        // mientras esta todavía recorre su lista.
        private readonly Collider[] hits = new Collider[48];

        [SerializeField, Min(0.5f)] private float radius = 3.5f;
        [SerializeField, Min(0f)] private float damage = 35f;
        [SerializeField] private AudioClip explosionClip;

        private Health health;
        private bool exploded;

        private void Awake() => health = GetComponent<Health>();

        private void OnEnable() => health.Died += OnDied;

        private void OnDisable() => health.Died -= OnDied;

        private void OnDied(Health _)
        {
            if (exploded)
            {
                return;
            }

            exploded = true;
            Vector3 center = transform.position + Vector3.up;
            int count = Physics.OverlapSphereNonAlloc(center, radius, hits, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var other = hits[i] != null ? hits[i].GetComponentInParent<Health>() : null;
                if (other != null && other != health && !other.IsDead)
                {
                    other.ApplyDamage(damage);
                }
            }

            if (explosionClip != null)
            {
                AudioSource.PlayClipAtPoint(explosionClip, center, GameSettings.EffectsVolume);
            }

            // La onda: una esfera naranja que se ve un instante.
            GameObject blast = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Collider blastCollider = blast.GetComponent<Collider>();
            blastCollider.enabled = false;
            Destroy(blastCollider);
            blast.transform.position = center;
            blast.transform.localScale = Vector3.one * (radius * 2f);
            var tint = new MaterialPropertyBlock();
            tint.SetColor("_BaseColor", new Color(1f, 0.5f, 0.1f));
            blast.GetComponent<Renderer>().SetPropertyBlock(tint);
            Destroy(blast, 0.2f);
        }

        internal void Configure(float blastRadius, float blastDamage, AudioClip clip)
        {
            radius = Mathf.Max(0.5f, blastRadius);
            damage = Mathf.Max(0f, blastDamage);
            explosionClip = clip;
        }
    }
}
