using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>
    /// Punto del suelo al que apunta el jugador, calculado con geometría y no con colisiones:
    /// no depende de lo que se cruce por delante ni del propio cuerpo del jugador. Si mira más
    /// lejos de lo que alcanza (o al horizonte), devuelve el punto a su alcance máximo en esa
    /// dirección, para que siempre haya un objetivo donde está mirando.
    /// </summary>
    public static class GroundAim
    {
        /// <param name="groundHeight">Altura (Y) del suelo plano.</param>
        /// <param name="maxReach">Distancia horizontal máxima desde el jugador.</param>
        /// <returns>False solo si mira recto hacia arriba o hacia abajo sin dirección horizontal y sin tocar suelo.</returns>
        public static bool TryGetPoint(Ray ray, float groundHeight, float maxReach, out Vector3 point)
        {
            Vector3 origin = ray.origin;
            Vector3 direction = ray.direction;
            var flatDirection = new Vector3(direction.x, 0f, direction.z);
            float flatLength = flatDirection.magnitude;

            // Mirando hacia abajo: donde el rayo toca el suelo, si queda dentro del alcance.
            if (direction.y < -0.0001f && origin.y > groundHeight)
            {
                float distance = (groundHeight - origin.y) / direction.y;
                Vector3 hit = origin + direction * distance;
                float flatDistance = new Vector2(hit.x - origin.x, hit.z - origin.z).magnitude;
                if (flatDistance <= maxReach)
                {
                    point = new Vector3(hit.x, groundHeight, hit.z);
                    return true;
                }
            }

            if (flatLength < 0.0001f)
            {
                point = default;
                return false;
            }

            // Más lejos que el alcance, o mirando al frente o hacia arriba: el borde del alcance.
            Vector3 edge = flatDirection / flatLength * maxReach;
            point = new Vector3(origin.x + edge.x, groundHeight, origin.z + edge.z);
            return true;
        }
    }
}
