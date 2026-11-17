using UnityEngine;

namespace DeadAcres.UI
{
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
