using DeadAcres.Core.Session;
using UnityEngine;

namespace DeadAcres.GameMode
{
    /// <summary>
    /// Adaptador de escena del <see cref="PlacementLimiter"/>: un único contador de plantas y
    /// objetos para toda la partida, creado con el modo de la sesión. Lo comparten la granja
    /// y, más adelante, la colocación de defensas.
    /// </summary>
    public sealed class PlacementLimits : MonoBehaviour
    {
        [SerializeField] private GameSession session;

        private PlacementLimiter limiter;

        /// <summary>Null si no hay sesión o modo asignado (sin límites).</summary>
        public PlacementLimiter Limiter
        {
            get
            {
                if (limiter == null && session != null && session.Mode != null)
                {
                    limiter = new PlacementLimiter(session.Mode);
                }

                return limiter;
            }
        }

        internal void Configure(GameSession newSession)
        {
            session = newSession;
            limiter = null;
        }
    }
}
