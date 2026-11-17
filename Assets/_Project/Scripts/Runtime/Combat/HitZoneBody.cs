using UnityEngine;

namespace DeadAcres.Combat
{
    /// <summary>
    /// Cuerpo con zonas de impacto (los zombis): según dónde dé el disparo del jugador, el daño
    /// se multiplica. Las zonas coinciden con las piezas visibles: piernas abajo, cabeza arriba
    /// y brazos a los costados del torso.
    /// </summary>
    public sealed class HitZoneBody : MonoBehaviour
    {
        [Header("Silueta")]
        [SerializeField, Min(0.1f)] private float height = 1.8f;

        [Tooltip("Fracción de la altura hasta donde llegan las piernas.")]
        [SerializeField, Range(0f, 1f)] private float legsTop = 0.45f;

        [Tooltip("Fracción de la altura donde empieza la cabeza.")]
        [SerializeField, Range(0f, 1f)] private float headBottom = 0.83f;

        [Tooltip("Distancia lateral al eje del cuerpo a partir de la cual el impacto es en un brazo.")]
        [SerializeField, Min(0f)] private float armOffset = 0.22f;

        [Header("Daño por zona")]
        [SerializeField, Min(0f)] private float headMultiplier = 2f;
        [SerializeField, Min(0f)] private float torsoMultiplier = 1f;
        [SerializeField, Min(0f)] private float armMultiplier = 0.4f;
        [SerializeField, Min(0f)] private float legMultiplier = 0.4f;

        /// <summary>Zona en la que da un disparo que impactó en <paramref name="worldHitPoint"/>.</summary>
        public HitZone GetZone(Vector3 worldHitPoint, Ray shot)
        {
            return HitZones.Classify(transform.InverseTransformPoint(worldHitPoint),
                transform.InverseTransformPoint(shot.origin), transform.InverseTransformDirection(shot.direction),
                height, legsTop, headBottom, armOffset);
        }

        public float GetMultiplier(HitZone zone)
        {
            switch (zone)
            {
                case HitZone.Head: return headMultiplier;
                case HitZone.Arm: return armMultiplier;
                case HitZone.Leg: return legMultiplier;
                default: return torsoMultiplier;
            }
        }

        internal void Configure(float bodyHeight, float legsTopFraction, float headBottomFraction, float armLateralOffset,
            float head, float torso, float arm, float leg)
        {
            height = Mathf.Max(0.1f, bodyHeight);
            legsTop = Mathf.Clamp01(legsTopFraction);
            headBottom = Mathf.Clamp01(headBottomFraction);
            armOffset = Mathf.Max(0f, armLateralOffset);
            headMultiplier = Mathf.Max(0f, head);
            torsoMultiplier = Mathf.Max(0f, torso);
            armMultiplier = Mathf.Max(0f, arm);
            legMultiplier = Mathf.Max(0f, leg);
        }
    }
}
