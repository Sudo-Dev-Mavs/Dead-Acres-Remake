using UnityEngine;

namespace DeadAcres.Combat
{
    /// <summary>Parte del cuerpo en la que impacta un disparo.</summary>
    public enum HitZone : byte
    {
        Torso = 0,
        Head = 1,
        Arm = 2,
        Leg = 3,
    }

    /// <summary>
    /// A qué parte del cuerpo da un disparo, calculado con geometría sobre la silueta del
    /// enemigo: no hacen falta colliders por extremidad. Funciones puras.
    /// </summary>
    public static class HitZones
    {
        /// <param name="localHitPoint">Punto de impacto en el espacio local del enemigo (pies en Y = 0).</param>
        /// <param name="localRayOrigin">Desde dónde se disparó, en ese mismo espacio.</param>
        /// <param name="localRayDirection">Dirección del disparo, en ese mismo espacio.</param>
        /// <param name="height">Altura total del enemigo.</param>
        /// <param name="legsTop">Fracción de la altura hasta donde llegan las piernas.</param>
        /// <param name="headBottom">Fracción de la altura donde empieza la cabeza.</param>
        /// <param name="armOffset">Distancia lateral al eje del cuerpo a partir de la cual es un brazo.</param>
        public static HitZone Classify(Vector3 localHitPoint, Vector3 localRayOrigin, Vector3 localRayDirection,
            float height, float legsTop, float headBottom, float armOffset)
        {
            float relativeHeight = height > 0f ? localHitPoint.y / height : 0.5f;
            if (relativeHeight >= headBottom)
            {
                return HitZone.Head;
            }

            if (relativeHeight < legsTop)
            {
                return HitZone.Leg;
            }

            // A la altura del torso: brazo si la bala pasa lejos del eje hacia un costado del
            // cuerpo. Se mira por dónde pasa la trayectoria, no dónde toca la cápsula: así un
            // tiro de lado al centro del cuerpo sigue siendo torso.
            var origin = new Vector2(localRayOrigin.x, localRayOrigin.z);
            var direction = new Vector2(localRayDirection.x, localRayDirection.z);
            float lateral = localHitPoint.x;
            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
                Vector2 closest = origin - direction * Vector2.Dot(origin, direction);
                lateral = closest.x;
            }

            return Mathf.Abs(lateral) >= armOffset ? HitZone.Arm : HitZone.Torso;
        }
    }
}
