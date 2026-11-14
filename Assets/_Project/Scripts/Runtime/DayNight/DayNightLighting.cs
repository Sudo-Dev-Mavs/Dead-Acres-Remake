using UnityEngine;

namespace DeadAcres.DayNight
{
    /// <summary>
    /// Oscurece la escena según la altura del sol: intensidad del sol, luz de luna y luz ambiente.
    /// Lee la rotación que ya aplica <see cref="DayNightCycle"/>, así que no depende del reloj.
    /// Requiere Lighting → Environment → Source en "Color" (lo deja así el generador de escena).
    /// </summary>
    public sealed class DayNightLighting : MonoBehaviour
    {
        [SerializeField] private Light sun;
        [SerializeField] private Light moon;
        [SerializeField, Min(0f)] private float sunIntensity = 1.3f;
        [SerializeField, Min(0f)] private float moonIntensity = 0.25f;
        [SerializeField] private Color dayAmbient = new Color(0.62f, 0.66f, 0.72f);
        [SerializeField] private Color nightAmbient = new Color(0.05f, 0.07f, 0.13f);

        // Interpolación continua: tiene que ejecutarse cada frame, no hay evento que lo sustituya.
        private void LateUpdate()
        {
            if (sun == null)
            {
                return;
            }

            float elevation = Vector3.Dot(-sun.transform.forward, Vector3.up);
            float daylight = Mathf.Clamp01(Mathf.InverseLerp(-0.1f, 0.25f, elevation));

            sun.intensity = sunIntensity * daylight;
            RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, daylight);

            if (moon != null)
            {
                moon.intensity = moonIntensity * (1f - daylight);
            }
        }

        internal void Configure(Light sunLight, Light moonLight)
        {
            sun = sunLight;
            moon = moonLight;
        }
    }
}
