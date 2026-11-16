using System.Collections.Generic;
using DeadAcres.Combat;
using DeadAcres.Enemies;
using UnityEngine;

namespace DeadAcres.UI
{
    /// <summary>
    /// La partida de mentira que se ve detrás del menú principal: van llegando zombis hacia la
    /// caravana y las torretas de la escena los abaten. Nada se rompe del todo: lo que protege
    /// se repara solo, para que la escena pueda estar horas en pantalla.
    /// </summary>
    public sealed class MenuDemoDirector : MonoBehaviour
    {
        [SerializeField] private List<ZombieBrain> zombiePrefabs = new List<ZombieBrain>();
        [SerializeField] private Health caravan;

        [Tooltip("Muros, torretas y demás: se reparan solos.")]
        [SerializeField] private List<Health> repaired = new List<Health>();

        [SerializeField, Min(5f)] private float spawnRadius = 30f;
        [SerializeField, Min(0.2f)] private float spawnInterval = 1.6f;
        [SerializeField, Min(1)] private int maxAlive = 14;

        private readonly List<Health> alive = new List<Health>(16);
        private readonly List<Health> noExtraTargets = new List<Health>(0);
        private float nextSpawn;
        private float nextRepair;

        private void Update()
        {
            if (Time.time >= nextSpawn && alive.Count < maxAlive && zombiePrefabs.Count > 0 && caravan != null)
            {
                nextSpawn = Time.time + spawnInterval;
                Spawn();
            }

            if (Time.time >= nextRepair)
            {
                nextRepair = Time.time + 1f;
                if (caravan != null) caravan.Heal(caravan.Max);
                for (int i = 0; i < repaired.Count; i++)
                {
                    if (repaired[i] != null) repaired[i].Heal(repaired[i].Max);
                }
            }
        }

        private void Spawn()
        {
            Vector3 center = caravan.transform.position;
            float angle = Random.value * Mathf.PI * 2f;
            var position = new Vector3(center.x + Mathf.Cos(angle) * spawnRadius, 0.1f, center.z + Mathf.Sin(angle) * spawnRadius);
            Quaternion facing = Quaternion.LookRotation(new Vector3(center.x - position.x, 0f, center.z - position.z));

            // De vez en cuando, uno fuerte (el último de la lista).
            int index = zombiePrefabs.Count > 1 && Random.value < 0.15f ? zombiePrefabs.Count - 1 : 0;
            ZombieBrain zombie = Instantiate(zombiePrefabs[index], position, facing, transform);
            zombie.Initialize(caravan, noExtraTargets);

            var health = zombie.GetComponent<Health>();
            health.Died += OnZombieDied;
            alive.Add(health);
        }

        private void OnZombieDied(Health zombie)
        {
            zombie.Died -= OnZombieDied;
            alive.Remove(zombie);
            Destroy(zombie.gameObject, 0.15f);
        }

        internal void Configure(List<ZombieBrain> prefabs, Health caravanHealth, List<Health> repairedObjects, float radius)
        {
            zombiePrefabs = prefabs;
            caravan = caravanHealth;
            repaired = repairedObjects;
            spawnRadius = Mathf.Max(5f, radius);
        }
    }

    /// <summary>Cámara del menú: da vueltas despacio alrededor de un punto, mirándolo.</summary>
    public sealed class MenuCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Vector3 center = new Vector3(0f, 1.5f, 0f);
        [SerializeField, Min(1f)] private float radius = 17f;
        [SerializeField] private float height = 7.5f;
        [SerializeField] private float degreesPerSecond = 5f;

        [Tooltip("Desplaza la vista hacia un lado para dejar sitio al menú (fracción del radio).")]
        [SerializeField] private float sideOffset = 0.28f;

        private float angle;

        private void LateUpdate()
        {
            angle += degreesPerSecond * Time.unscaledDeltaTime;
            float radians = angle * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Cos(radians) * radius, height, Mathf.Sin(radians) * radius);
            transform.position = center + offset;

            // Mira un poco a la izquierda del centro: la escena queda a la derecha del panel del menú.
            Vector3 toCenter = center - transform.position;
            Vector3 side = Vector3.Cross(Vector3.up, toCenter).normalized;
            transform.rotation = Quaternion.LookRotation(toCenter - side * (radius * sideOffset));
        }

        internal void Configure(Vector3 orbitCenter, float orbitRadius, float orbitHeight)
        {
            center = orbitCenter;
            radius = Mathf.Max(1f, orbitRadius);
            height = orbitHeight;
        }
    }
}
