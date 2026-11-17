using System.Collections.Generic;
using DeadAcres.Combat;
using UnityEngine;

namespace DeadAcres.Enemies
{
    /// <summary>
    /// Algo que un zombi lanza (ácido, una roca): vuela en arco hasta donde estaba su objetivo
    /// al lanzarlo y, al caer, daña a lo que sea del bando del jugador alrededor. Se puede
    /// esquivar moviéndose.
    /// </summary>
    public sealed class ZombieProjectile : MonoBehaviour
    {
        private static readonly Collider[] Hits = new Collider[32];
        private static readonly List<Health> Damaged = new List<Health>(8);

        private Vector3 from;
        private Vector3 to;
        private float duration;
        private float elapsed;
        private float arcHeight;
        private float damage;
        private float radius;
        private bool launched;

        /// <summary>Crea el proyectil (una esfera o un cubo de ese color y tamaño), todavía sin lanzar.</summary>
        public static ZombieProjectile Create(Vector3 position, float size, Color color, bool cube)
        {
            GameObject visual = GameObject.CreatePrimitive(cube ? PrimitiveType.Cube : PrimitiveType.Sphere);
            visual.name = "ZombieProjectile";
            // Destroy es diferido: se apaga ya para que ningún rayo choque con él este fotograma.
            Collider visualCollider = visual.GetComponent<Collider>();
            visualCollider.enabled = false;
            Destroy(visualCollider);
            visual.transform.position = position;
            visual.transform.localScale = Vector3.one * size;

            var tint = new MaterialPropertyBlock();
            tint.SetColor("_BaseColor", color);
            tint.SetColor("_Color", color);
            visual.GetComponent<Renderer>().SetPropertyBlock(tint);
            return visual.AddComponent<ZombieProjectile>();
        }

        public void Launch(Vector3 target, float speed, float arc, float hitDamage, float hitRadius)
        {
            from = transform.position;
            to = target;
            duration = Mathf.Max(0.1f, Vector3.Distance(from, to) / Mathf.Max(0.1f, speed));
            arcHeight = arc;
            damage = hitDamage;
            radius = hitRadius;
            elapsed = 0f;
            launched = true;
        }

        private void Update()
        {
            if (!launched)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(from, to, t) + Vector3.up * (arcHeight * 4f * t * (1f - t));
            if (t >= 1f)
            {
                Land();
            }
        }

        private void Land()
        {
            int count = Physics.OverlapSphereNonAlloc(to, radius, Hits, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            Damaged.Clear();
            for (int i = 0; i < count; i++)
            {
                var health = Hits[i].GetComponentInParent<Health>();
                if (health != null && health.Faction == Faction.PlayerSide && !health.IsDead && !Damaged.Contains(health))
                {
                    Damaged.Add(health);
                    health.ApplyDamage(damage);
                }
            }

            Destroy(gameObject);
        }
    }
}
